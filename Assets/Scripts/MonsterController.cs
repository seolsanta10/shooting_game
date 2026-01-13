using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [Header("몬스터 데이터")]
    private MonsterData data;
    private Transform planetCenter;
    
    [Header("상태")]
    private int currentHealth;
    private float lastAttackTime = 0f;
    
    void Start()
    {
        if (data != null)
        {
            currentHealth = data.health;
        }
    }
    
    public void Initialize(MonsterData monsterData, Transform planet)
    {
        data = monsterData;
        planetCenter = planet;
        currentHealth = data.health;
        
        // 색상 적용
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && data.color != Color.white)
        {
            Material mat = renderer.material;
            mat.color = data.color;
        }
        
        // 스케일 적용
        if (data.scale != 1f)
        {
            transform.localScale *= data.scale;
        }
    }
    
    void Update()
    {
        if (planetCenter == null) return;
        
        // 지구를 중심으로 회전하며 이동
        MoveAroundPlanet();
        
        // 플레이어 공격 체크
        CheckAttackPlayer();
    }
    
    void MoveAroundPlanet()
    {
        // 지구 중심으로부터의 방향
        Vector3 directionFromPlanet = (transform.position - planetCenter.position).normalized;
        
        // 랜덤하게 회전하며 이동
        float angle = data.moveSpeed * Time.deltaTime / Vector3.Distance(transform.position, planetCenter.position);
        Vector3 randomAxis = Random.onUnitSphere;
        Quaternion rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, randomAxis);
        directionFromPlanet = rotation * directionFromPlanet;
        
        // 위치 업데이트
        float distance = Vector3.Distance(transform.position, planetCenter.position);
        transform.position = planetCenter.position + directionFromPlanet * distance;
        
        // 지구를 향하도록 회전
        Vector3 up = directionFromPlanet;
        Vector3 forward = transform.forward;
        if (up.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forward, up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, data.rotationSpeed * Time.deltaTime);
        }
    }
    
    void CheckAttackPlayer()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.transform.position);
        
        if (distance <= data.attackRange && Time.time - lastAttackTime >= data.attackCooldown)
        {
            AttackPlayer(player);
            lastAttackTime = Time.time;
        }
    }
    
    void AttackPlayer(GameObject player)
    {
        // 플레이어에게 데미지 주기 (플레이어 스크립트에 데미지 함수가 있다고 가정)
        // PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        // if (playerHealth != null)
        // {
        //     playerHealth.TakeDamage(data.damage);
        // }
        
        Debug.Log($"{data.monsterName}이(가) 플레이어를 공격했습니다! 데미지: {data.damage}");
    }
    
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        // 스폰러에 알리기
        MonsterSpawner spawner = FindAnyObjectByType<MonsterSpawner>();
        if (spawner != null)
        {
            spawner.OnMonsterKilled();
        }
        
        // 보상 지급 (점수 시스템이 있다면)
        // ScoreManager.AddScore(data.score);
        // ScoreManager.AddExp(data.exp);
        
        Destroy(gameObject);
    }
    
    void OnTriggerEnter(Collider other)
    {
        // 미사일과 충돌 시
        if (other.CompareTag("Missile"))
        {
            TakeDamage(50); // 미사일 데미지
            Destroy(other.gameObject);
        }
    }
}
