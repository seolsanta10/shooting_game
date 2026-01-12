using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("이동 설정")]
    public Transform groundCenter;
    public Transform playerTransform;
    public float baseSpeed = 3f; // 기본 전진 속도 (플레이어와 동일한 방식)
    public float rotationSpeed = 90f; // 회전 속도
    public float detectionRange = 25f; // 플레이어 감지 범위
    public float smoothRotationSpeed = 5f; // 부드러운 회전 속도
    
    [Header("고도 설정")]
    public float altitude = 5f; // 초기 고도
    public float minAltitude = 2f;
    public float maxAltitude = 20f;
    
    private float currentAltitude;
    private Vector3 currentDirection = Vector3.up;
    private float groundRadius = 25f;
    private bool isTrackingPlayer = false; // 플레이어 추적 중인지
    
    void Start()
    {
        // Ground 찾기
        if (groundCenter == null)
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                ground = GameObject.Find("지구");
            }
            if (ground != null)
            {
                groundCenter = ground.transform;
                groundRadius = ground.transform.localScale.x * 0.5f;
            }
        }
        else
        {
            groundRadius = groundCenter.localScale.x * 0.5f;
        }
        
        // 플레이어 찾기
        if (playerTransform == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        // 초기 고도 설정
        if (groundCenter != null)
        {
            Vector3 directionFromGround = (transform.position - groundCenter.position).normalized;
            float currentDistance = Vector3.Distance(transform.position, groundCenter.position);
            currentAltitude = currentDistance - groundRadius;
            currentDirection = directionFromGround;
        }
        else
        {
            currentAltitude = altitude;
        }
    }
    
    void Update()
    {
        if (groundCenter == null) return;
        
        // 플레이어 감지
        bool playerDetected = false;
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            playerDetected = distanceToPlayer <= detectionRange;
        }
        
        isTrackingPlayer = playerDetected;
        
        // 플레이어를 발견했을 때만 이동
        if (isTrackingPlayer)
        {
            MoveTowardsPlayer();
        }
        else
        {
            // 플레이어를 발견하지 못했으면 정지 (또는 직진만)
            // 현재 위치 유지
            MaintainPosition();
        }
    }
    
    /// <summary>
    /// 플레이어 쪽으로 이동 (플레이어와 동일한 이동 방식)
    /// </summary>
    void MoveTowardsPlayer()
    {
        if (playerTransform == null || groundCenter == null) return;
        
        // 지구 중심으로부터의 방향 벡터
        Vector3 directionFromPlanet = (transform.position - groundCenter.position).normalized;
        
        // 플레이어 방향 계산
        Vector3 toPlayer = playerTransform.position - transform.position;
        
        // 지구 표면에 접하는 평면에 투영 (구 표면을 따라 이동)
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, directionFromPlanet).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, directionFromPlanet).normalized;
        
        // 방향 벡터가 너무 작으면 월드 기준으로 계산
        if (forward.magnitude < 0.1f)
        {
            Vector3 toEnemy = (transform.position - groundCenter.position).normalized;
            Vector3 worldUp = Vector3.up;
            
            if (Vector3.Dot(toEnemy, worldUp) > 0.9f)
            {
                forward = Vector3.forward;
                right = Vector3.right;
            }
            else
            {
                forward = Vector3.ProjectOnPlane(Vector3.forward, directionFromPlanet).normalized;
                right = Vector3.ProjectOnPlane(Vector3.right, directionFromPlanet).normalized;
            }
        }
        
        // 플레이어를 향하는 방향 계산 (구 표면을 따라)
        Vector3 toPlayerProjected = Vector3.ProjectOnPlane(toPlayer, directionFromPlanet).normalized;
        
        // 이동 방향 결정 (플레이어 쪽으로)
        Vector3 moveDirection = toPlayerProjected;
        
        if (moveDirection.magnitude < 0.1f)
        {
            // 플레이어가 바로 위/아래에 있으면 현재 forward 방향으로 직진
            moveDirection = forward;
        }
        
        // 구 표면을 따라 이동 (플레이어와 동일한 방식)
        if (moveDirection.magnitude > 0.1f && baseSpeed > 0f)
        {
            float angle = baseSpeed * Time.deltaTime / (groundRadius + currentAltitude);
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
        float totalDistance = groundRadius + currentAltitude;
        transform.position = groundCenter.position + currentDirection * totalDistance;
        
        // 비행기가 지구를 향하도록 회전
        Vector3 up = directionFromPlanet;
        Vector3 flightForward = moveDirection;
        
        // forward 방향을 구 표면에 평행하게 조정
        Vector3 moveDir = Vector3.ProjectOnPlane(flightForward, up).normalized;
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
    
    /// <summary>
    /// 현재 위치 유지 (플레이어를 발견하지 못했을 때)
    /// </summary>
    void MaintainPosition()
    {
        if (groundCenter == null) return;
        
        // 현재 위치 유지 (직진하지 않음)
        Vector3 directionFromPlanet = (transform.position - groundCenter.position).normalized;
        float totalDistance = groundRadius + currentAltitude;
        
        // 위치는 유지하되, 회전만 업데이트 (지구를 향하도록)
        Vector3 up = directionFromPlanet;
        Vector3 forward = transform.forward;
        
        // forward를 구 표면에 평행하게 조정
        Vector3 moveDir = Vector3.ProjectOnPlane(forward, up).normalized;
        if (moveDir.magnitude > 0.1f)
        {
            forward = moveDir.normalized;
        }
        
        if (up.magnitude > 0.1f && forward.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forward, up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothRotationSpeed * Time.deltaTime);
        }
        
        // 위치는 정확히 유지
        transform.position = groundCenter.position + directionFromPlanet.normalized * totalDistance;
    }
    
    /// <summary>
    /// 플레이어 추적 중인지 확인
    /// </summary>
    public bool IsTrackingPlayer()
    {
        return isTrackingPlayer;
    }
}
