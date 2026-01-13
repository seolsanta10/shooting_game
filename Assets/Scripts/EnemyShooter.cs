using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("발사 설정")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRange = 20f; // 발사 범위
    public float fireRate = 2f; // 발사 속도 (초당)
    public float bulletSpeed = 15f;
    
    private Transform playerTransform;
    private float lastFireTime = 0f;
    private Transform groundCenter;
    
    void Start()
    {
        // 전역 프리팹 설정(있으면) 적용
        GamePrefabSettings settings = GamePrefabSettings.LoadOrNull();
        if (settings != null && bulletPrefab == null && settings.enemyBulletPrefab != null)
        {
            bulletPrefab = settings.enemyBulletPrefab;
        }

        // 플레이어 찾기
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // Ground 찾기
        GameObject ground = GameObject.Find("Ground");
        if (ground == null) ground = GameObject.Find("지구");
        if (ground != null)
        {
            groundCenter = ground.transform;
        }
        
        // 발사점이 없으면 자동으로 생성
        if (firePoint == null)
        {
            GameObject firePointObj = new GameObject("EnemyFirePoint");
            firePointObj.transform.SetParent(transform);
            firePointObj.transform.localPosition = new Vector3(0, 0, 0.5f); // 적 앞쪽
            firePoint = firePointObj.transform;
        }
        
        // 총알 프리팹이 없으면 기본 총알 생성
        if (bulletPrefab == null)
        {
            CreateDefaultBulletPrefab();
        }
    }
    
    void Update()
    {
        if (playerTransform == null) return;
        
        // 플레이어와의 거리 확인
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        
        // 범위 내에 있으면 플레이어를 계속 바라보기
        if (distance <= fireRange)
        {
            // EnemyController가 플레이어를 추적 중인지 확인
            EnemyController enemyController = GetComponent<EnemyController>();
            bool canFire = enemyController == null || enemyController.IsTrackingPlayer();
            
            if (canFire)
            {
                LookAtPlayer();
                
                // 발사 가능하면 총알 발사
                if (Time.time - lastFireTime >= 1f / fireRate)
                {
                    FireBullet();
                    lastFireTime = Time.time;
                }
            }
        }
    }
    
    void FireBullet()
    {
        if (bulletPrefab == null || firePoint == null || playerTransform == null) return;
        
        // 적이 플레이어를 바라보도록 회전
        LookAtPlayer();
        
        // 플레이어 방향 계산 (정확한 위치를 향함 - 고도 포함)
        Vector3 directionToPlayer = (playerTransform.position - firePoint.position).normalized;
        
        // 총알 생성
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        
        // 총알이 플레이어를 향하도록 회전 (Ground 표면에 접하는 방향)
        if (directionToPlayer.magnitude > 0.1f)
        {
            // Ground 중심을 기준으로 up 벡터 계산
            if (groundCenter != null)
            {
                Vector3 directionFromGround = (firePoint.position - groundCenter.position).normalized;
                Vector3 up = directionFromGround;
                
                // 플레이어 방향을 Ground 표면에 투영
                Vector3 forward = Vector3.ProjectOnPlane(directionToPlayer, up).normalized;
                
                if (forward.magnitude > 0.1f)
                {
                    // Ground 표면에 접하는 방향으로 회전 (forward가 앞, up이 위)
                    bullet.transform.rotation = Quaternion.LookRotation(forward, up);
                }
                else
                {
                    // forward가 너무 작으면 기본 방향 사용
                    bullet.transform.rotation = Quaternion.LookRotation(directionToPlayer, up);
                }
            }
            else
            {
                bullet.transform.rotation = Quaternion.LookRotation(directionToPlayer);
            }
        }
        
        // 총알 컴포넌트 추가
        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        if (bulletScript == null)
        {
            bulletScript = bullet.AddComponent<EnemyBullet>();
        }
        
        // 총알 크기 설정
        bullet.transform.localScale = Vector3.one * 0.15f;
    }
    
    void LookAtPlayer()
    {
        if (playerTransform == null || groundCenter == null) return;
        
        // Ground 중심으로부터의 방향
        Vector3 directionFromGround = (transform.position - groundCenter.position).normalized;
        Vector3 up = directionFromGround;
        
        // 플레이어 방향 계산
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        Vector3 forward = Vector3.ProjectOnPlane(directionToPlayer, up).normalized;
        
        if (forward.magnitude > 0.1f)
        {
            // 적이 플레이어를 향하도록 회전 (빠르게)
            Quaternion targetRotation = Quaternion.LookRotation(forward, up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
        }
    }
    
    void CreateDefaultBulletPrefab()
    {
        // 기본 총알 프리팹 생성 (원형 구)
        GameObject defaultBullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        defaultBullet.name = "EnemyBullet";
        defaultBullet.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f); // 원형이므로 균일한 크기
        
        // 색상 설정
        Renderer renderer = defaultBullet.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.red; // 적 총알은 빨간색
            renderer.material = mat;
        }
        
        // Collider 설정
        Collider collider = defaultBullet.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        
        bulletPrefab = defaultBullet;
    }
}
