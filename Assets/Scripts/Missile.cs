using UnityEngine;

public class Missile : MonoBehaviour
{
    [Header("미사일 설정")]
    public float speed = 20f;
    public float lifetime = 5f;
    public GameObject explosionEffect;
    
    private float timer = 0f;
    private Vector3 direction; // 이동 방향 (Ground 표면에 접하는 방향)
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
        
        // Rigidbody 설정
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        
        // Collider 설정 (미사일끼리 충돌 안하도록)
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
        }
        col.isTrigger = true;
        
        // 레이어 설정
        gameObject.layer = LayerMask.NameToLayer("Default");
        
        // 현재 고도 계산
        if (groundCenter != null)
        {
            float currentDistance = Vector3.Distance(transform.position, groundCenter.position);
            currentAltitude = currentDistance - groundRadius;
            
            // MissileLauncher에서 설정한 회전을 그대로 유지
            // transform.forward를 Ground 표면에 투영하여 이동 방향만 계산
            Vector3 directionFromGround = (transform.position - groundCenter.position).normalized;
            Vector3 up = directionFromGround;
            
            // 현재 회전의 forward를 Ground 표면에 투영하여 이동 방향 결정
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            
            if (forward.magnitude > 0.1f)
            {
                direction = forward.normalized;
                // 회전은 MissileLauncher에서 설정한 것을 그대로 유지
                // Start()에서는 회전을 변경하지 않음
            }
            else
            {
                // forward가 너무 작으면 현재 방향 유지
                direction = transform.forward;
            }
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
            
            // 회전은 MissileLauncher에서 설정한 것을 완전히 유지
            // Update()에서는 회전을 변경하지 않음
            // 이동 방향만 Ground 표면에 맞게 계산하여 위치만 업데이트
        }
        
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // 적 총알과 충돌 시
        if (other.gameObject.GetComponent<EnemyBullet>() != null)
        {
            // 적 총알도 파괴
            Destroy(other.gameObject);
            // 플레이어 미사일도 파괴
            if (explosionEffect != null)
            {
                Instantiate(explosionEffect, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
            return;
        }
        
        // 플레이어 미사일끼리 충돌 안함
        if (other.gameObject.GetComponent<Missile>() != null)
        {
            return;
        }
        
        // 적과 충돌 시
        if (other.gameObject.name.StartsWith("enemy"))
        {
            EnemyHealthBar healthBar = other.GetComponent<EnemyHealthBar>();
            if (healthBar != null)
            {
                healthBar.TakeDamage(50f); // 미사일 데미지
            }
            
            if (explosionEffect != null)
            {
                Instantiate(explosionEffect, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
        // 지구나 다른 오브젝트와 충돌 시 폭발
        else if (other.gameObject.name != "Player" && other.gameObject.name != "Ground" && other.gameObject.name != "지구")
        {
            if (explosionEffect != null)
            {
                Instantiate(explosionEffect, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }
}
