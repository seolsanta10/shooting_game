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
        
        UpdateCameraPosition();
        
        // 회전 중이 아니면 카메라 회전 업데이트
        if (!isRolling)
        {
            UpdateCameraRotation();
        }
    }
    
    void UpdateCameraPosition()
    {
        // 비행기의 뒤쪽 방향 계산 (forward의 반대)
        Vector3 backDirection = -target.forward;
        
        // 비행기의 up 방향
        Vector3 upDirection = target.up;
        
        // 카메라 위치: 비행기 뒤쪽 + 위쪽
        Vector3 desiredPosition = target.position 
            + backDirection * distance 
            + upDirection * height;
        
        // 부드럽게 이동
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position, 
            desiredPosition, 
            positionSmoothSpeed * Time.deltaTime
        );
        
        // 뒤집힘 방지
        if (preventFlip)
        {
            Vector3 directionToTarget = (target.position - smoothedPosition).normalized;
            float dot = Vector3.Dot(transform.up, upDirection);
            
            if (dot < 0.1f) // 뒤집힌 경우
            {
                smoothedPosition = lastValidPosition;
            }
            else
            {
                lastValidPosition = smoothedPosition;
            }
        }
        
        transform.position = smoothedPosition;
    }
    
    void UpdateCameraRotation()
    {
        // 비행기 앞쪽을 약간 앞서서 바라보는 위치 계산
        Vector3 lookTarget = target.position + target.forward * lookAheadDistance;
        
        // 카메라가 바라볼 방향
        Vector3 lookDirection = lookTarget - transform.position;
        
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
        
        // 뒤집힘 방지: 급격한 회전 방지
        if (preventFlip)
        {
            float rotationDiff = Quaternion.Angle(transform.rotation, targetRotation);
            if (rotationDiff > 90f)
            {
                targetRotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f);
            }
        }
        
        // 부드럽게 회전
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            rotationSmoothSpeed * Time.deltaTime
        );
        
        lastValidRotation = transform.rotation;
    }
}
