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
    
    private float currentAltitude;
    private Vector3 currentDirection = Vector3.up;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashDuration = 0.5f;
    
    // 마우스 입력
    private Mouse mouse;
    
    private bool isRolling = false; // 회전 중인지 확인
    private BoosterGauge boosterGauge; // 부스터 게이지 참조
    
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
    }
    
    void Update()
    {
        if (planetCenter == null)
        {
            Debug.LogWarning("FlightSimulationController: planetCenter가 설정되지 않았습니다!");
            return;
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
            // A + Space: Z축으로 360도 회전 (좌측)
            if (keyboard.aKey.isPressed)
            {
                Debug.Log("A + Space 감지: Z축 360도 회전 시작 (좌측)");
                StartCoroutine(ZAxisRotation360(true));
            }
            // D + Space: Z축으로 360도 회전 (우측, 반대 방향)
            else if (keyboard.dKey.isPressed)
            {
                Debug.Log("D + Space 감지: Z축 360도 회전 시작 (우측)");
                StartCoroutine(ZAxisRotation360(false));
            }
        }
        
        // A 키가 방금 눌렸을 때 Space가 눌려있는지 확인 (추가 확인)
        if (keyboard.aKey.wasPressedThisFrame && keyboard.spaceKey.isPressed && !isRolling)
        {
            Debug.Log("A + Space 감지 (A 먼저): Z축 360도 회전 시작 (좌측)");
            StartCoroutine(ZAxisRotation360(true));
        }
        
        // D 키가 방금 눌렸을 때 Space가 눌려있는지 확인 (추가 확인)
        if (keyboard.dKey.wasPressedThisFrame && keyboard.spaceKey.isPressed && !isRolling)
        {
            Debug.Log("D + Space 감지 (D 먼저): Z축 360도 회전 시작 (우측)");
            StartCoroutine(ZAxisRotation360(false));
        }
        
        
        // 디버그: S와 Space 상태 확인 (필요시 주석 해제)
        // if (keyboard.sKey.isPressed && keyboard.spaceKey.isPressed)
        // {
        //     Debug.Log($"S+Space 상태: S={keyboard.sKey.isPressed}, Space={keyboard.spaceKey.isPressed}, SpacePressed={keyboard.spaceKey.wasPressedThisFrame}");
        // }
        
        // Q/E 키 회전 처리 (회전 중이 아닐 때만)
        float qeRotation = 0f;
        if (!isRolling)
        {
            if (keyboard.qKey.isPressed)
            {
                qeRotation = 1f; // 오른쪽 회전
            }
            else if (keyboard.eKey.isPressed)
            {
                qeRotation = -1f; // 왼쪽 회전
            }
        }
        
        // 일반 이동 입력 (회전 중이 아니고, A+Space나 D+Space 조합이 아닐 때만)
        if (!isRolling && !aAndSpace && !dAndSpace)
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
        
        // Q/E 키 회전 처리 (Y축 회전)
        if (qeRotation != 0f && !isRolling)
        {
            // Y축 회전 (비행기의 Y축을 중심으로 회전)
            float rotationAngle = qeRotationSpeed * qeRotation * Time.deltaTime;
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
        
        // 비행기가 지구를 향하도록 회전
        Vector3 up = directionFromPlanet;
        Vector3 flightForward = transform.forward;
        
        // Q/E 회전 중이 아니면 회전 업데이트
        if (qeRotation == 0f)
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
            // Q/E 회전 중일 때는 Ground를 향하도록만 유지 (Y축 회전은 유지)
            Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            if (projectedForward.magnitude > 0.1f)
            {
                // Y축 회전은 유지하고, Ground를 향하도록만 보정
                Quaternion targetRotation = Quaternion.LookRotation(projectedForward, up);
                Vector3 currentEuler = transform.rotation.eulerAngles;
                Vector3 targetEuler = targetRotation.eulerAngles;
                // Y축 회전은 유지하고, X와 Z만 보정
                transform.rotation = Quaternion.Euler(targetEuler.x, currentEuler.y, targetEuler.z);
            }
        }
    }
    
    System.Collections.IEnumerator ZAxisRotation360(bool left)
    {
        isRolling = true;
        
        if (planetCenter == null) 
        {
            Debug.LogError("ZAxisRotation360: planetCenter가 null입니다!");
            isRolling = false;
            yield break;
        }
        
        Debug.Log($"Z축 회전 시작: left={left}");
        
        float totalRotation = 0f;
        float targetRotation = 360f; // 한 바퀴 회전
        float rotationSpeedValue = rotationSpeed * 4f; // 빠른 회전을 위해 4배속
        
        // 현재 위치와 방향 저장
        Vector3 directionFromPlanet = (transform.position - planetCenter.position).normalized;
        Quaternion startRotation = transform.rotation;
        
        // Z축 회전 (비행기의 forward 방향 = 앞뒤 축)
        Vector3 rotationAxis = transform.forward.normalized; // Z축 = 비행기의 앞 방향
        
        // 회전 방향 결정 (left가 true면 좌측, false면 우측)
        float rotationDirection = left ? -1f : 1f;
        
        Debug.Log($"회전 축: {rotationAxis}, 방향: {rotationDirection}");
        
        while (totalRotation < targetRotation)
        {
            float deltaAngle = rotationSpeedValue * Time.deltaTime;
            totalRotation += deltaAngle;
            
            // Z축(비행기의 forward 방향)을 중심으로 360도 회전
            // 로컬 좌표계에서 Z축 회전 (롤 회전)
            transform.Rotate(0, 0, deltaAngle * rotationDirection, Space.Self);
            
            // 회전 축도 함께 업데이트 (비행기가 회전하면서 축도 따라감)
            Quaternion zRotation = Quaternion.AngleAxis(deltaAngle * rotationDirection, rotationAxis);
            rotationAxis = zRotation * rotationAxis;
            
            // 지구를 향하도록 위치 유지
            directionFromPlanet = (transform.position - planetCenter.position).normalized;
            float totalDistance = planetRadius + currentAltitude;
            Vector3 desiredPosition = planetCenter.position + directionFromPlanet * totalDistance;
            transform.position = desiredPosition;
            
            currentDirection = directionFromPlanet;
            
            yield return null;
        }
        
        Debug.Log($"Z축 회전 완료: 총 {totalRotation}도 회전");
        
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
