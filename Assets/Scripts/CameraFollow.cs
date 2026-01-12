using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("3인칭 시점 설정")]
    public Transform target; // 플레이어
    public float distance = 8f; // 비행기로부터의 거리
    public float height = 3f; // 비행기 위로 올라가는 높이
    public float lookAheadDistance = 2f; // 비행기 앞쪽을 바라보는 거리
    
    [Header("부드러움 설정")]
    public float positionSmoothSpeed = 5f;
    public float rotationSmoothSpeed = 5f;
    
    [Header("Back Turn 중 카메라 설정")]
    [Tooltip("Back Turn 중 위치 추적 속도 배율")]
    public float backTurnPositionSpeedMultiplier = 3f;
    
    [Tooltip("Back Turn 중 회전 추적 속도 배율")]
    public float backTurnRotationSpeedMultiplier = 4f;
    
    [Header("1인칭 시점 설정")]
    [Tooltip("1인칭 시점 사용 여부")]
    public bool useFirstPerson = true; // 기본값을 true로 변경
    
    [Tooltip("1인칭 시점에서 카메라 오프셋 (플레이어 기준)")]
    public Vector3 firstPersonOffset = new Vector3(0f, 0.5f, 0f);
    
    [Tooltip("1인칭 시점 전환 속도")]
    public float firstPersonTransitionSpeed = 5f;
    
    private bool isFirstPersonMode = false;
    
    [Header("회전 제한")]
    public bool preventFlip = true;
    public float minVerticalAngle = 10f; // 최소 수직 각도 (위에서 내려다보는 각도)
    public float maxVerticalAngle = 60f; // 최대 수직 각도
    
    private Vector3 lastValidPosition;
    private Quaternion lastValidRotation;
    
    void Start()
    {
        // 카메라 배경을 하늘만 보이도록 설정
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.Skybox;
            // 하늘색 배경 (Skybox가 없을 경우를 대비)
            cam.backgroundColor = new Color(0.5f, 0.7f, 1f, 0f); // 하늘색
        }
        
        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
        
        if (target != null)
        {
            // 초기 위치 설정 (비행기 뒤쪽 위에서)
            UpdateCameraPosition();
            lastValidPosition = transform.position;
            lastValidRotation = transform.rotation;
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // 비행기가 회전 중인지 확인
        FlightSimulationController flightController = target.GetComponent<FlightSimulationController>();
        bool isRolling = flightController != null && flightController.IsRolling();
        
        // 공중 기동 기술 실행 중인지 확인
        AerialManeuverAbility maneuverAbility = target.GetComponent<AerialManeuverAbility>();
        bool isManeuvering = maneuverAbility != null && maneuverAbility.IsManeuvering();
        
        // Back Turn 실행 중인지 확인
        BackTurnAbility backTurnAbility = target.GetComponent<BackTurnAbility>();
        bool isBackTurning = backTurnAbility != null && backTurnAbility.IsBackTurning();
        
        // Back Turn 중에는 더 빠르게 위치 업데이트
        UpdateCameraPosition(isBackTurning);
        
        // 회전 중이 아니고 기동 중이 아니면 카메라 회전 업데이트
        // 기동 중이나 Back Turn 중에는 카메라가 뒤집히지 않도록 더 부드럽게 처리
        if (!isRolling)
        {
            if (isManeuvering || isBackTurning)
            {
                // 기동 중이나 Back Turn 중에는 회전을 더 부드럽게 하고 뒤집힘 방지 강화
                UpdateCameraRotationSmooth(isManeuvering || isBackTurning, isBackTurning);
            }
            else
            {
                UpdateCameraRotation();
            }
        }
    }
    
    void UpdateCameraPosition(bool isBackTurning = false)
    {
        Vector3 desiredPosition;
        
        // 1인칭 시점인지 확인
        if (isFirstPersonMode && useFirstPerson)
        {
            // 1인칭 시점: 플레이어 위치 + 오프셋
            desiredPosition = target.position + target.TransformDirection(firstPersonOffset);
        }
        else
        {
            // 3인칭 시점: 비행기 뒤쪽 + 위쪽
            Vector3 backDirection = -target.forward;
            Vector3 upDirection = target.up;
            desiredPosition = target.position 
                + backDirection * distance 
                + upDirection * height;
        }
        
        // Back Turn 중에는 더 빠르게 이동
        float currentSmoothSpeed = isBackTurning 
            ? positionSmoothSpeed * backTurnPositionSpeedMultiplier 
            : positionSmoothSpeed;
        
        // 부드럽게 이동
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position, 
            desiredPosition, 
            currentSmoothSpeed * Time.deltaTime
        );
        
        // 뒤집힘 방지 (1인칭 시점에서는 제한 완화)
        if (preventFlip && !isFirstPersonMode)
        {
            Vector3 directionToTarget = (target.position - smoothedPosition).normalized;
            float dot = Vector3.Dot(transform.up, target.up);
            
            if (dot < 0.1f) // 뒤집힌 경우
            {
                smoothedPosition = lastValidPosition;
            }
            else
            {
                lastValidPosition = smoothedPosition;
            }
        }
        else
        {
            lastValidPosition = smoothedPosition;
        }
        
        transform.position = smoothedPosition;
    }
    
    void UpdateCameraRotation()
    {
        Vector3 lookTarget;
        Vector3 lookDirection;
        
        // 1인칭 시점인지 확인
        if (isFirstPersonMode && useFirstPerson)
        {
            // 1인칭 시점: 플레이어가 바라보는 방향
            lookTarget = target.position + target.forward * lookAheadDistance;
            lookDirection = target.forward;
        }
        else
        {
            // 3인칭 시점: 비행기 앞쪽을 약간 앞서서 바라보는 위치 계산
            lookTarget = target.position + target.forward * lookAheadDistance;
            lookDirection = lookTarget - transform.position;
        }
        
        if (lookDirection.magnitude < 0.1f) return;
        
        // 수직 각도 제한 (위에서 내려다보는 각도 유지)
        if (preventFlip)
        {
            Vector3 horizontalDirection = Vector3.ProjectOnPlane(lookDirection, target.up).normalized;
            float verticalAngle = Vector3.Angle(lookDirection, horizontalDirection);
            
            // 각도가 너무 작거나 크면 조정
            if (verticalAngle < minVerticalAngle)
            {
                // 너무 수평이면 위로 올리기
                Vector3 adjustedDirection = Vector3.RotateTowards(
                    horizontalDirection, 
                    target.up, 
                    (minVerticalAngle - verticalAngle) * Mathf.Deg2Rad, 
                    0f
                );
                lookDirection = adjustedDirection.normalized * lookDirection.magnitude;
            }
            else if (verticalAngle > maxVerticalAngle)
            {
                // 너무 아래면 각도 제한
                lookDirection = Vector3.RotateTowards(
                    lookDirection, 
                    horizontalDirection, 
                    (verticalAngle - maxVerticalAngle) * Mathf.Deg2Rad, 
                    0f
                );
            }
        }
        
        // 목표 회전 계산
        Quaternion targetRotation;
        if (isFirstPersonMode && useFirstPerson)
        {
            // 1인칭 시점: 플레이어 회전을 그대로 사용
            targetRotation = target.rotation;
        }
        else
        {
            targetRotation = Quaternion.LookRotation(lookDirection, target.up);
        }
        
        // 뒤집힘 방지: 급격한 회전 방지 (1인칭 시점에서는 제한 없음)
        if (preventFlip && !isFirstPersonMode)
        {
            float rotationDiff = Quaternion.Angle(transform.rotation, targetRotation);
            if (rotationDiff > 90f)
            {
                targetRotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f);
            }
        }
        
        // 부드럽게 회전
        float smoothSpeed = isFirstPersonMode ? rotationSmoothSpeed * 2f : rotationSmoothSpeed;
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            smoothSpeed * Time.deltaTime
        );
        
        lastValidRotation = transform.rotation;
    }
    
    /// <summary>
    /// 1인칭 시점 설정
    /// </summary>
    public void SetFirstPerson(bool enable)
    {
        isFirstPersonMode = enable;
    }
    
    /// <summary>
    /// 현재 1인칭 시점인지 확인
    /// </summary>
    public bool IsFirstPerson()
    {
        return isFirstPersonMode;
    }
    
    /// <summary>
    /// 기동 중 카메라 회전 업데이트 (뒤집힘 방지 강화)
    /// </summary>
    void UpdateCameraRotationSmooth(bool isManeuvering, bool isBackTurning = false)
    {
        Vector3 lookTarget;
        Vector3 lookDirection;
        
        // 1인칭 시점인지 확인
        if (isFirstPersonMode && useFirstPerson)
        {
            // 1인칭 시점: 플레이어가 바라보는 방향
            lookTarget = target.position + target.forward * lookAheadDistance;
            lookDirection = target.forward;
        }
        else
        {
            // 3인칭 시점: 비행기 앞쪽을 약간 앞서서 바라보는 위치 계산
            lookTarget = target.position + target.forward * lookAheadDistance;
            lookDirection = lookTarget - transform.position;
        }
        
        if (lookDirection.magnitude < 0.1f) return;
        
        // 수직 각도 제한 (위에서 내려다보는 각도 유지)
        if (preventFlip)
        {
            Vector3 horizontalDirection = Vector3.ProjectOnPlane(lookDirection, target.up).normalized;
            float verticalAngle = Vector3.Angle(lookDirection, horizontalDirection);
            
            // 각도가 너무 작거나 크면 조정
            if (verticalAngle < minVerticalAngle)
            {
                // 너무 수평이면 위로 올리기
                Vector3 adjustedDirection = Vector3.RotateTowards(
                    horizontalDirection, 
                    target.up, 
                    (minVerticalAngle - verticalAngle) * Mathf.Deg2Rad, 
                    0f
                );
                lookDirection = adjustedDirection.normalized * lookDirection.magnitude;
            }
            else if (verticalAngle > maxVerticalAngle)
            {
                // 너무 아래면 각도 제한
                lookDirection = Vector3.RotateTowards(
                    lookDirection, 
                    horizontalDirection, 
                    (verticalAngle - maxVerticalAngle) * Mathf.Deg2Rad, 
                    0f
                );
            }
        }
        
        // 목표 회전 계산
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection, target.up);
        
        // 뒤집힘 방지: 급격한 회전 방지 (1인칭 시점이나 Back Turn 중에는 제한 완화)
        if (preventFlip && !isFirstPersonMode)
        {
            float rotationDiff = Quaternion.Angle(transform.rotation, targetRotation);
            
            // Back Turn 중에는 더 빠르게 회전하도록 허용
            float maxRotationDiff = isBackTurning ? 180f : (isManeuvering ? 45f : 90f);
            
            if (rotationDiff > maxRotationDiff && !isBackTurning)
            {
                // 급격한 회전을 부드럽게 제한 (Back Turn 중에는 제한 없음)
                float lerpFactor = isManeuvering ? 0.05f : 0.1f;
                targetRotation = Quaternion.Slerp(transform.rotation, targetRotation, lerpFactor);
            }
        }
        else if (isFirstPersonMode)
        {
            // 1인칭 시점에서는 플레이어 회전을 그대로 따라감
            targetRotation = target.rotation;
        }
        
        // Back Turn 중에는 더 빠르게 회전
        float smoothSpeed;
        if (isBackTurning)
        {
            smoothSpeed = rotationSmoothSpeed * backTurnRotationSpeedMultiplier;
        }
        else if (isManeuvering)
        {
            smoothSpeed = rotationSmoothSpeed * 0.5f; // 기동 중에는 더 부드럽게
        }
        else
        {
            smoothSpeed = rotationSmoothSpeed;
        }
        
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            smoothSpeed * Time.deltaTime
        );
        
        lastValidRotation = transform.rotation;
    }
}
