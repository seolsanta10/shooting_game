using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Header("총알 설정")]
    public float speed = 15f;
    public float lifetime = 3f;
    public int damage = 10;
    
    private float timer = 0f;
    private Vector3 direction; // 이동 방향 (Ground 표면에 접하는 방향)
    private Transform targetPlayer; // 플레이어 추적
    private Transform groundCenter;
    private float groundRadius = 25f;
    private float currentAltitude; // 현재 고도
    
    void Start()
    {
        // Ground 찾기
        GameObject ground = GameObject.Find("Ground");
        if (ground != null)
        {
            groundCenter = ground.transform;
            groundRadius = ground.transform.localScale.x * 0.5f;
        }
        
        // 플레이어 찾기
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            targetPlayer = player.transform;
        }
        
        // Rigidbody 설정
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        
        // Collider 설정 (적 총알끼리 충돌 안하도록)
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
        }
        col.isTrigger = true;
        
        // 레이어 설정으로 총알끼리 물리 충돌 방지
        // 같은 레이어의 총알끼리는 Physics Layer에서 충돌 안하도록 설정 필요
        gameObject.layer = LayerMask.NameToLayer("Default");
        
        // 현재 고도 계산
        if (groundCenter != null)
        {
            float currentDistance = Vector3.Distance(transform.position, groundCenter.position);
            currentAltitude = currentDistance - groundRadius;
        }
        
        // 플레이어 방향 계산 (Ground 표면을 고려)
        if (targetPlayer != null && groundCenter != null)
        {
            Vector3 directionFromGround = (transform.position - groundCenter.position).normalized;
            Vector3 up = directionFromGround;
            
            // 플레이어 방향
            Vector3 toPlayer = targetPlayer.position - transform.position;
            
            // Ground 표면에 접하는 평면에서의 방향
            Vector3 forward = Vector3.ProjectOnPlane(toPlayer, up).normalized;
            if (forward.magnitude > 0.1f)
            {
                direction = forward.normalized;
                // 총알이 Ground 표면에 접하는 방향으로 회전 (forward가 앞, up이 위)
                transform.rotation = Quaternion.LookRotation(forward, up);
            }
            else
            {
                // forward가 너무 작으면 현재 회전 유지
                direction = Vector3.ProjectOnPlane(transform.forward, up).normalized;
                if (direction.magnitude < 0.1f)
                {
                    direction = transform.forward;
                }
            }
        }
        else if (targetPlayer != null)
        {
            direction = (targetPlayer.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(direction);
        }
        else
        {
            direction = transform.forward;
        }
    }
    
    void Update()
    {
        if (groundCenter == null)
        {
            // Ground가 없으면 직선 이동
            transform.position += direction * speed * Time.deltaTime;
        }
        else
        {
            // Ground 중심을 기준으로 구면 좌표계에서 이동
            Vector3 directionFromGround = (transform.position - groundCenter.position).normalized;
            float currentDistance = Vector3.Distance(transform.position, groundCenter.position);
            
            // Ground 표면에 접하는 방향으로 이동
            Vector3 up = directionFromGround;
            Vector3 moveDirection = Vector3.ProjectOnPlane(direction, up).normalized;
            
            if (moveDirection.magnitude > 0.1f)
            {
                // 구면 좌표계에서 이동 (Ground 중심을 기준으로 회전)
                float angle = speed * Time.deltaTime / currentDistance;
                Vector3 rotationAxis = Vector3.Cross(directionFromGround, moveDirection).normalized;
                
                if (rotationAxis.magnitude > 0.1f)
                {
                    Quaternion rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, rotationAxis);
                    directionFromGround = rotation * directionFromGround;
                    directionFromGround.Normalize();
                }
            }
            
            // 위치 업데이트 (고도 유지)
            transform.position = groundCenter.position + directionFromGround * currentDistance;
            
            // 총알이 Ground 표면에 접하는 방향으로 회전 유지
            Vector3 forward = Vector3.ProjectOnPlane(direction, up).normalized;
            if (forward.magnitude > 0.1f)
            {
                // forward가 앞, up이 위가 되도록 회전
                Quaternion targetRotation = Quaternion.LookRotation(forward, up);
                transform.rotation = targetRotation; // 즉시 회전 (Slerp 제거)
            }
        }
        
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // 플레이어 미사일과 충돌 시
        if (other.gameObject.GetComponent<Missile>() != null)
        {
            // 플레이어 미사일도 파괴
            Destroy(other.gameObject);
            // 적 총알도 파괴
            Destroy(gameObject);
            return;
        }
        
        // 플레이어와 충돌 시
        if (other.gameObject.name == "Player")
        {
            // 플레이어에게 데미지 주기 (플레이어 스크립트에 데미지 함수가 있다고 가정)
            // PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            // if (playerHealth != null)
            // {
            //     playerHealth.TakeDamage(damage);
            // }
            
            Destroy(gameObject);
        }
        // Ground나 다른 오브젝트와 충돌 시 (적 총알끼리는 충돌 안함)
        else if (other.gameObject.name != "EnemyBullet" && 
                 !other.gameObject.name.StartsWith("enemy") &&
                 other.gameObject.GetComponent<EnemyBullet>() == null)
        {
            Destroy(gameObject);
        }
    }
}
