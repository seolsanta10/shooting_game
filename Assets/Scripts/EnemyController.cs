using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("이동 설정")]
    public Transform groundCenter;
    public Transform playerTransform;
    public float moveSpeed = 3f;
    public float rotationSpeed = 90f;
    public float detectionRange = 25f; // 플레이어 감지 범위
    
    private float targetAltitude; // 플레이어와 동일한 고도
    private float groundRadius = 25f;
    private float currentAltitude = 5f; // 현재 고도
    private bool isTrackingPlayer = false; // 플레이어 추적 중인지
    
    void Start()
    {
        // Ground 찾기
        if (groundCenter == null)
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground != null)
            {
                groundCenter = ground.transform;
                groundRadius = ground.transform.localScale.x * 0.5f;
            }
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
        Vector3 directionFromGround = (transform.position - groundCenter.position).normalized;
        float currentDistance = Vector3.Distance(transform.position, groundCenter.position);
        currentAltitude = currentDistance - groundRadius;
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
        
        // 플레이어를 발견했을 때만 고도 맞추기
        if (isTrackingPlayer)
        {
            UpdateTargetAltitude();
            // 플레이어와 같은 고도로 이동
            MoveAtPlayerAltitude();
        }
        else
        {
            // 플레이어를 발견하지 못했으면 현재 고도 유지하며 이동
            MoveAtCurrentAltitude();
        }
    }
    
    void UpdateTargetAltitude()
    {
        if (playerTransform != null && groundCenter != null)
        {
            // 플레이어의 현재 고도 계산
            float playerDistance = Vector3.Distance(playerTransform.position, groundCenter.position);
            targetAltitude = playerDistance - groundRadius;
        }
    }
    
    void MoveAtPlayerAltitude()
    {
        // 현재 위치
        Vector3 directionFromGround = (transform.position - groundCenter.position).normalized;
        float currentDistance = Vector3.Distance(transform.position, groundCenter.position);
        
        // 목표 거리 (Ground 반지름 + 플레이어 고도)
        float targetDistance = groundRadius + targetAltitude;
        
        // 고도 조정 (부드럽게)
        float altitudeAdjustSpeed = 5f; // 고도 조정 속도
        if (Mathf.Abs(currentDistance - targetDistance) > 0.1f)
        {
            // 플레이어와 같은 고도로 부드럽게 이동
            float newDistance = Mathf.Lerp(currentDistance, targetDistance, altitudeAdjustSpeed * Time.deltaTime);
            directionFromGround = directionFromGround.normalized;
            transform.position = groundCenter.position + directionFromGround * newDistance;
            currentAltitude = newDistance - groundRadius;
        }
        
        // 랜덤하게 회전하며 이동
        float angle = moveSpeed * Time.deltaTime / targetDistance;
        Vector3 randomAxis = Random.onUnitSphere;
        Quaternion rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, randomAxis);
        directionFromGround = rotation * directionFromGround;
        directionFromGround.Normalize();
        
        // 위치 업데이트
        transform.position = groundCenter.position + directionFromGround * targetDistance;
        
        // 지구를 향하도록 회전
        Vector3 up = directionFromGround;
        Vector3 forward = transform.forward;
        if (up.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forward, up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    void MoveAtCurrentAltitude()
    {
        // 현재 고도 유지하며 이동
        Vector3 directionFromGround = (transform.position - groundCenter.position).normalized;
        float currentDistance = groundRadius + currentAltitude;
        
        // 랜덤하게 회전하며 이동
        float angle = moveSpeed * Time.deltaTime / currentDistance;
        Vector3 randomAxis = Random.onUnitSphere;
        Quaternion rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, randomAxis);
        directionFromGround = rotation * directionFromGround;
        directionFromGround.Normalize();
        
        // 위치 업데이트
        transform.position = groundCenter.position + directionFromGround * currentDistance;
        
        // 지구를 향하도록 회전
        Vector3 up = directionFromGround;
        Vector3 forward = transform.forward;
        if (up.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forward, up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    public bool IsTrackingPlayer()
    {
        return isTrackingPlayer;
    }
}
