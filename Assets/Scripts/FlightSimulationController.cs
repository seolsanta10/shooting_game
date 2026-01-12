using UnityEngine;
using UnityEngine.InputSystem;

public class FlightSimulationController : MonoBehaviour
{
    [Header("지구 설정")]
    public Transform planetCenter;
    public float planetRadius = 25f; // 비행기 크기의 100배 (비행기 스케일 0.5 기준)
    
    [Header("비행 설정")]
    public float baseSpeed = 5f; // 기본 전진 속도
    public float moveSpeed = 5f; // W 키 가속 시 추가 속도
    public float dashSpeed = 15f; // 부스터 속도
    public float rotationSpeed = 90f; // 회전 속도 (도/초)
    public float altitude = 5f; // 초기 고도
    public float minAltitude = 2f;
    public float maxAltitude = 20f;
    
    [Header("회전 설정")]
    public float smoothRotationSpeed = 5f;
    public float qeRotationSpeed = 30f; // Q/E 키 회전 속도 (도/초)
    public float qeFastTurnSpeed = 120f; // Q/E + Shift 고속 턴 속도 (도/초)
    public float doubleTapWindow = 0.3f; // 더블 탭 인식 시간 (초)
    
    [Header("측면 회피 롤 설정")]
    [Tooltip("롤 회전 시간 (초)")]
    public float rollDuration = 0.3f;
    
    [Tooltip("롤 회전 속도 (도/초)")]
    public float rollRotationSpeed = 1200f; // 빠른 회전

    [Tooltip("롤 중 측면 이동 속도 (미터/초)")]
    public float rollLateralSpeed = 20f;
    
    [Tooltip("회피 대쉬 시간 (초)")]
    public float evadeDashDuration = 0.2f;
    
    [Tooltip("회피 대쉬 거리 (미터)")]
    public float evadeDashDistance = 20f;
    
    [Tooltip("회피 대쉬 속도 (미터/초)")]
    public float evadeDashSpeed = 100f;
    
    private float currentAltitude;
    private Vector3 currentDirection = Vector3.up;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashDuration = 0.5f;
    
    // 마우스 입력
    private Mouse mouse;
    
    private bool isRolling = false; // 회전 중인지 확인
    private BoosterGauge boosterGauge; // 부스터 게이지 참조
    
    // W 키 더블 탭 백턴 관련
    private float lastWKeyPressTime = -1f; // 마지막 W 키 입력 시간
    private BackTurnAbility backTurnAbility; // Back Turn Ability 참조
    
    // 부스터(대쉬) 상태 확인용 public 프로퍼티
    public bool IsDashing { get { return isDashing; } }
    
    // 회전 중인지 확인용 public 프로퍼티
    public bool IsRolling() { return isRolling; }
    
    void Start()
    {
        // 지구를 자동으로 찾기 (지구 또는 Ground)
        if (planetCenter == null)
        {
            GameObject planet = GameObject.Find("Ground");
            if (planet == null)
            {
                planet = GameObject.Find("지구");
            }
            if (planet != null)
            {
                planetCenter = planet.transform;
                planetRadius = planet.transform.localScale.x * 0.5f;
            }
        }
        else
        {
            // Inspector에서 할당된 경우에도 반지름 자동 계산
            planetRadius = planetCenter.localScale.x * 0.5f;
        }
        
        currentAltitude = altitude;
        
        // 초기 위치 설정 (지구 위에 배치)
        if (planetCenter != null)
        {
            // 현재 위치가 이미 설정되어 있으면 그대로 사용
            // 그렇지 않으면 Ground 위에 배치
            if (Vector3.Distance(transform.position, planetCenter.position) < 0.1f)
            {
                // 위치가 Ground 중심과 거의 같으면 초기 위치 설정
                Vector3 directionFromPlanet = Vector3.up;
                float totalDistance = planetRadius + currentAltitude;
                transform.position = planetCenter.position + directionFromPlanet * totalDistance;
                currentDirection = directionFromPlanet;
                
                // 비행기가 지구를 향하도록 초기 회전
                transform.rotation = Quaternion.LookRotation(Vector3.forward, directionFromPlanet);
            }
            else
            {
                // 이미 위치가 설정되어 있으면 현재 위치를 기준으로 currentDirection 업데이트
                Vector3 directionFromPlanet = (transform.position - planetCenter.position).normalized;
                currentDirection = directionFromPlanet;
            }
        }
        
        mouse = Mouse.current;
        
        // 부스터 게이지 찾기
        GameObject gaugeObj = GameObject.Find("BoosterGauge");
        if (gaugeObj != null)
        {
            boosterGauge = gaugeObj.GetComponent<BoosterGauge>();
        }
        
        // Back Turn Ability 찾기
        backTurnAbility = GetComponent<BackTurnAbility>();
        if (backTurnAbility == null)
        {
            Debug.LogWarning("FlightSimulationController: BackTurnAbility를 찾을 수 없습니다. 자동으로 추가합니다...");
            backTurnAbility = gameObject.AddComponent<BackTurnAbility>();
            if (backTurnAbility != null)
            {
                Debug.Log("FlightSimulationController: BackTurnAbility 컴포넌트가 자동으로 추가되었습니다.");
            }
            else
            {
                Debug.LogError("FlightSimulationController: BackTurnAbility 컴포넌트 추가에 실패했습니다. 수동으로 추가해주세요.");
            }
        }
        else
        {
            Debug.Log("FlightSimulationController: BackTurnAbility를 찾았습니다.");
        }
    }
    
    void Update()
    {
        if (planetCenter == null)
        {
            Debug.LogWarning("FlightSimulationController: planetCenter가 설정되지 않았습니다!");
            return;
        }
        
        // 공중 기동 기술 실행 중이면 일반 입력 무시
        AerialManeuverAbility maneuverAbility = GetComponent<AerialManeuverAbility>();
        if (maneuverAbility != null && maneuverAbility.IsManeuvering())
        {
            return; // 기동 중에는 일반 비행 입력 무시
        }
        
        // Back Turn 실행 중이면 일반 입력 무시
        if (backTurnAbility != null && backTurnAbility.IsBackTurning())
        {
            return; // Back Turn 중에는 일반 비행 입력 무시
        }
        
        // WASD 입력 처리
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            Debug.LogWarning("FlightSimulationController: 키보드가 감지되지 않습니다!");
            return;
        }
        
        // 마우스 초기화 (없어도 키보드는 작동)
        if (mouse == null)
        {
            mouse = Mouse.current;
        }
        
        float horizontal = 0f;
        float vertical = 0f;
        
        // A + Space 또는 D + Space 조합 감지
        bool aAndSpace = keyboard.aKey.isPressed && keyboard.spaceKey.isPressed;
        bool dAndSpace = keyboard.dKey.isPressed && keyboard.spaceKey.isPressed;
        
        // Space 키가 방금 눌렸을 때 A나 D가 눌려있는지 확인
        if (keyboard.spaceKey.wasPressedThisFrame && !isRolling)
        {
            // A + Space: 좌측 측면 회피 롤
            if (keyboard.aKey.isPressed)
            {
                Debug.Log("A + Space 감지: 좌측 측면 회피 롤 시작");
                StartCoroutine(SideEvadeRoll360(left: true));
            }
            // D + Space: 우측 측면 회피 롤
            else if (keyboard.dKey.isPressed)
            {
                Debug.Log("D + Space 감지: 우측 측면 회피 롤 시작");
                StartCoroutine(SideEvadeRoll360(left: false));
            }
        }
        
        // A 키가 방금 눌렸을 때 Space가 눌려있는지 확인 (추가 확인)
        if (keyboard.aKey.wasPressedThisFrame && keyboard.spaceKey.isPressed && !isRolling)
        {
            Debug.Log("A + Space 감지 (A 먼저): 좌측 측면 회피 롤 시작");
            StartCoroutine(SideEvadeRoll360(left: true));
        }
        
        // D 키가 방금 눌렸을 때 Space가 눌려있는지 확인 (추가 확인)
        if (keyboard.dKey.wasPressedThisFrame && keyboard.spaceKey.isPressed && !isRolling)
        {
            Debug.Log("D + Space 감지 (D 먼저): 우측 측면 회피 롤 시작");
            StartCoroutine(SideEvadeRoll360(left: false));
        }
        
        
        // 디버그: S와 Space 상태 확인 (필요시 주석 해제)
        // if (keyboard.sKey.isPressed && keyboard.spaceKey.isPressed)
        // {
        //     Debug.Log($"S+Space 상태: S={keyboard.sKey.isPressed}, Space={keyboard.spaceKey.isPressed}, SpacePressed={keyboard.spaceKey.wasPressedThisFrame}");
        // }
        
        // Q/E 키 회전 처리 (회전 중이 아닐 때만)
        float qeRotation = 0f;
        bool isFastTurn = false; // Shift 키와 함께 눌렀는지 확인
        if (!isRolling)
        {
            bool shiftPressed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            
            if (keyboard.qKey.isPressed)
            {
                qeRotation = -1f; // 왼쪽 회전
                isFastTurn = shiftPressed; // Shift와 함께 누르면 고속 턴
            }
            else if (keyboard.eKey.isPressed)
            {
                qeRotation = 1f; // 오른쪽 회전
                isFastTurn = shiftPressed; // Shift와 함께 누르면 고속 턴
            }
        }
        
        // W 키 더블 탭 백턴 감지
        bool isBackTurning = backTurnAbility != null && backTurnAbility.IsBackTurning();
        
        if (!isRolling && !isBackTurning)
        {
            if (keyboard.wKey.wasPressedThisFrame)
            {
                float currentTime = Time.time;
                
                // 디버그: 첫 번째 탭 감지
                if (lastWKeyPressTime < 0f)
                {
                    Debug.Log($"W 키 첫 번째 탭 감지 (시간: {currentTime})");
                }
                
                // 이전 W 키 입력이 있고, 시간 창 내에 있으면 더블 탭으로 인식
                if (lastWKeyPressTime > 0f && (currentTime - lastWKeyPressTime) <= doubleTapWindow)
                {
                    Debug.Log($"W 더블 탭 감지! (시간 차이: {currentTime - lastWKeyPressTime:F3}초) Back Turn 발동 시도...");
                    
                    if (backTurnAbility != null)
                    {
                        bool activated = backTurnAbility.ActivateBackTurn();
                        if (activated)
                        {
                            Debug.Log("Back Turn 발동 성공!");
                            lastWKeyPressTime = -1f; // 리셋
                        }
                        else
                        {
                            Debug.LogWarning($"Back Turn 발동 실패 - 쿨타임: {backTurnAbility.GetCooldownRemaining():F1}초, 실행 중: {backTurnAbility.IsBackTurning()}");
                        }
                    }
                    else
                    {
                        Debug.LogError("BackTurnAbility가 null입니다! Start()에서 자동 추가를 시도합니다...");
                        // 런타임에 다시 시도
                        backTurnAbility = GetComponent<BackTurnAbility>();
                        if (backTurnAbility == null)
                        {
                            backTurnAbility = gameObject.AddComponent<BackTurnAbility>();
                            if (backTurnAbility != null)
                            {
                                Debug.Log("BackTurnAbility가 런타임에 추가되었습니다. 다시 시도해주세요.");
                            }
                        }
                    }
                }
                else
                {
                    // 첫 번째 탭으로 기록
                    lastWKeyPressTime = currentTime;
                    if (lastWKeyPressTime > 0f)
                    {
                        Debug.Log($"W 키 첫 번째 탭 기록 (다음 탭까지 {doubleTapWindow}초 이내)");
                    }
                }
            }
            
            // 더블 탭 시간 창이 지나면 리셋
            if (lastWKeyPressTime > 0f && (Time.time - lastWKeyPressTime) > doubleTapWindow)
            {
                Debug.Log($"더블 탭 시간 창 초과 ({Time.time - lastWKeyPressTime:F3}초), 리셋");
                lastWKeyPressTime = -1f;
            }
        }
        
        // 일반 이동 입력 (회전 중이 아니고, A+Space나 D+Space 조합이 아니고, 백턴 중이 아닐 때만)
        bool isBackTurningNow = backTurnAbility != null && backTurnAbility.IsBackTurning();
        if (!isRolling && !aAndSpace && !dAndSpace && !isBackTurningNow)
        {
            // W 키: 가속 (속도 증가)
            if (keyboard.wKey.isPressed) vertical = 1f;
            if (keyboard.sKey.isPressed) vertical = -1f;
            if (keyboard.aKey.isPressed && !keyboard.spaceKey.isPressed) horizontal = -1f;
            if (keyboard.dKey.isPressed && !keyboard.spaceKey.isPressed) horizontal = 1f;
        }
        
        // B 키: 부스터 (대쉬) - 부스터 게이지 확인
        bool canBoost = boosterGauge == null || boosterGauge.CanBoost();
        
        if (keyboard.bKey.isPressed && canBoost)
        {
            isDashing = true;
            dashTimer = 0f;
        }
        else
        {
            if (isDashing)
            {
                dashTimer += Time.deltaTime;
                if (dashTimer >= dashDuration)
                {
                    isDashing = false;
                }
            }
        }
        
        // 부스터가 없으면 대쉬 비활성화
        if (!canBoost)
        {
            isDashing = false;
        }
        
        // 지구 중심으로부터의 방향 벡터
        Vector3 directionFromPlanet = (transform.position - planetCenter.position).normalized;
        
        // Q/E 키 회전 처리 (Y축 회전) - 백턴 중이 아닐 때만
        bool isBackTurningForQE = backTurnAbility != null && backTurnAbility.IsBackTurning();
        if (qeRotation != 0f && !isRolling && !isBackTurningForQE)
        {
            // Y축 회전 (비행기의 Y축을 중심으로 회전)
            // Shift 키와 함께 누르면 고속 턴, 아니면 일반 회전
            float currentRotationSpeed = isFastTurn ? qeFastTurnSpeed : qeRotationSpeed;
            float rotationAngle = currentRotationSpeed * qeRotation * Time.deltaTime;
            transform.Rotate(0f, rotationAngle, 0f, Space.Self);
        }
        
        // 비행기는 항상 전진 (기본 속도 + W 키 가속 + 부스터)
        float currentSpeed = baseSpeed; // 기본 전진 속도
        
        // W 키를 누르면 가속
        if (vertical > 0f)
        {
            currentSpeed += moveSpeed;
        }
        
        // 부스터 중이면 부스터 속도 사용
        if (isDashing)
        {
            currentSpeed = dashSpeed;
        }
        
        // 지구 표면에 접하는 평면에서의 방향 계산
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, directionFromPlanet).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, directionFromPlanet).normalized;
        
        // 방향 벡터가 너무 작으면 월드 기준으로 계산
        if (forward.magnitude < 0.1f)
        {
            // 지구 중심에서 플레이어로의 방향을 기준으로 forward/right 계산
            Vector3 toPlayer = (transform.position - planetCenter.position).normalized;
            Vector3 worldUp = Vector3.up;
            
            // 지구의 up 방향이 월드 up과 다를 수 있으므로 조정
            if (Vector3.Dot(toPlayer, worldUp) > 0.9f)
            {
                // 위쪽에 있으면
                forward = Vector3.forward;
                right = Vector3.right;
            }
            else
            {
                forward = Vector3.ProjectOnPlane(Vector3.forward, directionFromPlanet).normalized;
                right = Vector3.ProjectOnPlane(Vector3.right, directionFromPlanet).normalized;
            }
        }
        
        // 이동 방향 계산 (항상 전진 + 좌우 이동)
        Vector3 moveDirection = forward; // 기본 전진 방향
        
        // 좌우 이동 추가
        if (horizontal != 0f)
        {
            moveDirection = (forward + right * horizontal).normalized;
        }
        
        // 회피 롤 중이 아니면 일반 이동
        if (!isRolling)
        {
            // 구 표면을 따라 이동
            if (moveDirection.magnitude > 0.1f && currentSpeed > 0f)
            {
                float angle = currentSpeed * Time.deltaTime / (planetRadius + currentAltitude);
                Vector3 rotationAxis = Vector3.Cross(directionFromPlanet, moveDirection).normalized;
                
                if (rotationAxis.magnitude > 0.1f)
                {
                    Quaternion rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, rotationAxis);
                    directionFromPlanet = rotation * directionFromPlanet;
                    directionFromPlanet.Normalize();
                    
                    currentDirection = directionFromPlanet;
                }
            }
            
            // 위치 업데이트
            float totalDistance = planetRadius + currentAltitude;
            transform.position = planetCenter.position + currentDirection * totalDistance;
        }
        
        // 회피 롤 중이 아니면 회전 업데이트
        if (!isRolling)
        {
            // 비행기가 지구를 향하도록 회전
            Vector3 up = directionFromPlanet;
            Vector3 flightForward = transform.forward;
            
            // Q/E 회전 중이 아니고 백턴 중이 아니면 회전 업데이트
            bool isBackTurningForRotation = backTurnAbility != null && backTurnAbility.IsBackTurning();
            if (qeRotation == 0f && !isBackTurningForRotation)
            {
                // 항상 전진하므로 forward 방향 유지
                Vector3 moveDir = Vector3.ProjectOnPlane(transform.forward, up).normalized;
                if (moveDir.magnitude > 0.1f)
                {
                    flightForward = moveDir.normalized;
                }
                
                if (up.magnitude > 0.1f && flightForward.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(flightForward, up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothRotationSpeed * Time.deltaTime);
                }
            }
            else
            {
                // Q/E 회전 중일 때는 Y축 회전만 하고, 회전 보정은 하지 않음
                // (회전 보정을 하면 방향이 반대로 되는 문제 발생)
                // Y축 회전은 이미 224줄에서 처리되었으므로 여기서는 추가 보정 없음
            }
        }
    }
    
    System.Collections.IEnumerator SideEvadeRoll360(bool left)
    {
        isRolling = true;

        if (planetCenter == null)
        {
            Debug.LogError("SideEvadeRoll360: planetCenter가 null입니다!");
            isRolling = false;
            yield break;
        }

        Vector3 center = planetCenter.position;
        
        Debug.Log($"SideEvadeRoll360 시작: {(left ? "좌측" : "우측")} Z축 롤 회전");

        // 롤 도중에도 "좌/우 이동 방향"이 뒤집히지 않도록,
        // 시작 시점의 측면 방향을 고정해둔다 (롤 회전으로 transform.right가 변해도 영향 없음)
        Vector3 startUp = (transform.position - center).normalized;
        Vector3 startForward = Vector3.ProjectOnPlane(transform.forward, startUp).normalized;
        if (startForward.sqrMagnitude < 0.0001f)
        {
            startForward = Vector3.ProjectOnPlane(transform.up, startUp).normalized;
        }
        if (startForward.sqrMagnitude < 0.0001f)
        {
            startForward = Vector3.ProjectOnPlane(Vector3.forward, startUp).normalized;
        }

        Vector3 startRight = Vector3.Cross(startUp, startForward).normalized;
        if (startRight.sqrMagnitude < 0.0001f)
        {
            startRight = Vector3.ProjectOnPlane(transform.right, startUp).normalized;
        }

        // A(좌)면 -right, D(우)면 +right
        Vector3 lateralBase = (left ? -startRight : startRight).normalized;

        // 롤 회전만 수행
        float rollDirection = left ? 1f : -1f;
        float targetRotation = 360f;
        float totalRotation = 0f;

        float rollElapsedTime = 0f;
        while (rollElapsedTime < rollDuration)
        {
            rollElapsedTime += Time.deltaTime;
            
            // 회전 각도 계산
            float rotationAngle = rollRotationSpeed * Time.deltaTime * rollDirection;
            totalRotation += Mathf.Abs(rotationAngle);
            
            // Z축(forward 축) 중심으로 회전
            transform.Rotate(0f, 0f, rotationAngle, Space.Self);
            
            // 롤 도중: 해당 방향(좌/우)으로 측면 이동 + 행성 표면 유지
            Vector3 oldUp = (transform.position - center).normalized;
            float totalDistance = planetRadius + currentAltitude;

            // 현재 위치에서의 측면(접선) 방향: "고정된" lateralBase를 표면에 투영
            Vector3 lateral = Vector3.ProjectOnPlane(lateralBase, oldUp).normalized;
            if (lateral.sqrMagnitude < 0.0001f)
            {
                // fallback: forward 기준으로 측면 생성
                lateral = Vector3.Cross(oldUp, Vector3.ProjectOnPlane(transform.forward, oldUp)).normalized;
            }

            // 구면 이동: moveDirection(측면)으로 일정 거리만큼 이동
            float moveDist = Mathf.Max(0f, rollLateralSpeed) * Time.deltaTime;
            float angleRad = moveDist / Mathf.Max(0.0001f, totalDistance);
            Vector3 rotationAxis = Vector3.Cross(oldUp, lateral).normalized;

            Vector3 newUp = oldUp;
            if (rotationAxis.sqrMagnitude > 0.0001f && angleRad > 0f)
            {
                Quaternion moveRot = Quaternion.AngleAxis(angleRad * Mathf.Rad2Deg, rotationAxis);
                newUp = (moveRot * oldUp).normalized;
            }

            // 위치 갱신 (표면 유지)
            transform.position = center + newUp * totalDistance;
            currentDirection = newUp;

            // 이동으로 변한 up만큼 기체 자세도 같이 보정 (롤은 유지됨)
            Quaternion upAlign = Quaternion.FromToRotation(oldUp, newUp);
            transform.rotation = upAlign * transform.rotation;

            yield return null;
        }

        // 롤 회전 완료: 정확히 360도
        float remainingRotation = targetRotation - totalRotation;
        if (remainingRotation > 0.1f)
        {
            transform.Rotate(0f, 0f, remainingRotation * rollDirection, Space.Self);
        }

        isRolling = false;
        Debug.Log($"SideEvadeRoll360 완료: {(left ? "좌측" : "우측")} 롤 회전 완료");
    }
    
    System.Collections.IEnumerator BarrelRollRight()
    {
        isRolling = true;
        
        if (planetCenter == null) 
        {
            isRolling = false;
            yield break;
        }
        
        float totalRotation = 0f;
        float targetRotation = 360f; // 한 바퀴 덤블링
        float rollSpeed = rotationSpeed * 4f; // 빠른 덤블링을 위해 4배속
        
        // 현재 방향 저장
        Vector3 directionFromPlanet = (transform.position - planetCenter.position).normalized;
        Vector3 startForward = transform.forward.normalized;
        
        // 덤블링 축: 비행기의 forward 방향 (앞으로 구르기)
        Vector3 rollAxis = startForward;
        
        // 시작 회전 저장
        Quaternion startRotation = transform.rotation;
        
        while (totalRotation < targetRotation)
        {
            float deltaAngle = rollSpeed * Time.deltaTime;
            totalRotation += deltaAngle;
            
            // forward 축을 중심으로 우측으로 회전 (덤블링)
            Quaternion rollRotation = Quaternion.AngleAxis(deltaAngle, rollAxis);
            transform.rotation = rollRotation * transform.rotation;
            
            // 덤블링 축도 함께 회전 (비행기가 회전하면서 축도 따라감)
            rollAxis = rollRotation * rollAxis;
            
            // 지구를 향하도록 위치 유지
            directionFromPlanet = (transform.position - planetCenter.position).normalized;
            float totalDistance = planetRadius + currentAltitude;
            Vector3 desiredPosition = planetCenter.position + directionFromPlanet * totalDistance;
            transform.position = desiredPosition;
            
            currentDirection = directionFromPlanet;
            
            yield return null;
        }
        
        // 마지막으로 지구를 향하도록 정렬
        directionFromPlanet = (transform.position - planetCenter.position).normalized;
        Vector3 finalUp = directionFromPlanet;
        Vector3 finalForward = Vector3.ProjectOnPlane(transform.forward, finalUp).normalized;
        if (finalForward.magnitude > 0.1f)
        {
            Quaternion finalRotation = Quaternion.LookRotation(finalForward, finalUp);
            transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, 0.5f);
        }
        
        isRolling = false;
    }
}
