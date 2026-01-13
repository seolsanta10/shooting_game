using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("프리팹 설정")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();
    public GameObject enemyBulletPrefab; // 적 총알 프리팹
    
    [Header("스폰 설정")]
    public Transform groundCenter;
    public Transform playerTransform; // 플레이어 Transform
    public float groundRadius = 25f; // Ground 스케일의 절반 (50 * 0.5)
    public int initialSpawnCount = 10; // 초기 스폰 개수
    public int maxEnemies = 20; // 최대 동시 존재 적 수
    public float spawnInterval = 5f; // 추가 스폰 간격 (초)
    
    private List<GameObject> activeEnemies = new List<GameObject>();
    private float lastSpawnTime = 0f;
    
    void Start()
    {
        // 전역 프리팹 설정(있으면) 적용
        GamePrefabSettings settings = GamePrefabSettings.LoadOrNull();
        if (settings != null)
        {
            if (enemyPrefabs == null) enemyPrefabs = new List<GameObject>();
            if (enemyPrefabs.Count == 0 && settings.enemyPrefabs != null && settings.enemyPrefabs.Count > 0)
                enemyPrefabs.AddRange(settings.enemyPrefabs);

            if (enemyBulletPrefab == null && settings.enemyBulletPrefab != null)
                enemyBulletPrefab = settings.enemyBulletPrefab;
        }

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
        
        // 플레이어 찾기
        if (playerTransform == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        // 프리팹 폴더에서 자동으로 로드
        LoadEnemyPrefabs();
        
        // 초기 적 스폰
        SpawnInitialEnemies();
    }
    
    void Update()
    {
        // 주기적으로 적 스폰
        if (Time.time - lastSpawnTime >= spawnInterval)
        {
            if (activeEnemies.Count < maxEnemies)
            {
                SpawnRandomEnemy();
                lastSpawnTime = Time.time;
            }
        }
        
        // 죽은 적 제거
        activeEnemies.RemoveAll(e => e == null);
    }
    
    void LoadEnemyPrefabs()
    {
        // Inspector에서 할당되지 않았으면 자동으로 찾기
        if (enemyPrefabs.Count == 0)
        {
            #if UNITY_EDITOR
            // 에디터에서만 작동: Assets/prefabs 폴더에서 enemy 프리팹 찾기
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/prefabs" });
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.name.StartsWith("enemy"))
                {
                    enemyPrefabs.Add(prefab);
                }
            }
            #endif
            
            // 런타임에서도 Resources 폴더를 통해 로드 시도
            if (enemyPrefabs.Count == 0)
            {
                for (int i = 1; i <= 10; i++)
                {
                    GameObject prefab = Resources.Load<GameObject>($"prefabs/enemy{i}");
                    if (prefab == null)
                    {
                        // Resources가 아닌 경우 직접 경로로 시도 (에디터 전용)
                        #if UNITY_EDITOR
                        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/prefabs/enemy{i}.prefab");
                        #endif
                    }
                    if (prefab != null)
                    {
                        enemyPrefabs.Add(prefab);
                    }
                }
            }
            
            if (enemyPrefabs.Count == 0)
            {
                Debug.LogWarning("EnemySpawner: 적 프리팹을 찾을 수 없습니다. Inspector에서 수동으로 할당해주세요!");
            }
            else
            {
                Debug.Log($"EnemySpawner: {enemyPrefabs.Count}개의 적 프리팹을 찾았습니다.");
            }
        }
    }
    
    void SpawnInitialEnemies()
    {
        if (enemyPrefabs.Count == 0 || groundCenter == null) return;
        
        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnRandomEnemy();
        }
    }
    
    public void SpawnRandomEnemy()
    {
        if (enemyPrefabs.Count == 0 || groundCenter == null) return;
        
        // 플레이어가 없으면 기본 고도 사용
        float playerAltitude = 5f;
        if (playerTransform != null)
        {
            // 플레이어의 현재 고도 계산
            Vector3 playerDirection = (playerTransform.position - groundCenter.position).normalized;
            float playerDistance = Vector3.Distance(playerTransform.position, groundCenter.position);
            playerAltitude = playerDistance - groundRadius;
        }
        
        // 랜덤 프리팹 선택
        GameObject randomPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        if (randomPrefab == null) return;
        
        // Ground 주변 랜덤 위치 계산 (플레이어와 동일한 고도)
        // Ground 중심을 기준으로 구면 좌표계에서 랜덤 위치 생성
        Vector3 randomDirection = Random.onUnitSphere;
        float totalDistance = groundRadius + playerAltitude; // 플레이어와 동일한 고도
        Vector3 spawnPosition = groundCenter.position + randomDirection * totalDistance;
        
        // 적 생성
        GameObject enemy = Instantiate(randomPrefab, spawnPosition, Quaternion.identity);
        
        // Ground 중심을 기준으로 올바른 방향 설정
        Vector3 directionFromGround = (spawnPosition - groundCenter.position).normalized;
        Vector3 up = directionFromGround; // Ground 중심에서 바깥쪽이 "위"
        
        // 플레이어 방향을 기준으로 forward 계산 (플레이어가 있으면)
        Vector3 forward;
        if (playerTransform != null)
        {
            Vector3 toPlayer = playerTransform.position - spawnPosition;
            forward = Vector3.ProjectOnPlane(toPlayer, up).normalized;
        }
        else
        {
            // 플레이어가 없으면 랜덤 방향
            forward = Vector3.ProjectOnPlane(Random.onUnitSphere, up).normalized;
        }
        
        // forward가 너무 작으면 대체 방향 사용
        if (forward.magnitude < 0.1f)
        {
            forward = Vector3.ProjectOnPlane(Vector3.forward, up).normalized;
            if (forward.magnitude < 0.1f)
            {
                forward = Vector3.ProjectOnPlane(Vector3.right, up).normalized;
            }
        }
        
        // Ground 표면에 접하는 방향으로 회전 설정
        if (forward.magnitude > 0.1f)
        {
            enemy.transform.rotation = Quaternion.LookRotation(forward, up);
        }
        
        // 적 이동 컨트롤러 추가 (플레이어와 같은 고도 유지)
        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        if (enemyController == null)
        {
            enemyController = enemy.AddComponent<EnemyController>();
        }
        
        // 총알 발사 컴포넌트 추가
        EnemyShooter shooter = enemy.GetComponent<EnemyShooter>();
        if (shooter == null)
        {
            shooter = enemy.AddComponent<EnemyShooter>();
        }
        
        // 적 총알 프리팹 할당
        if (enemyBulletPrefab == null)
        {
            // 자동으로 찾기
            #if UNITY_EDITOR
            enemyBulletPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/EnemyBullet.prefab");
            #endif
        }
        
        if (enemyBulletPrefab != null)
        {
            shooter.bulletPrefab = enemyBulletPrefab;
        }
        
        // HP 바 컴포넌트 추가
        EnemyHealthBar healthBar = enemy.GetComponent<EnemyHealthBar>();
        if (healthBar == null)
        {
            healthBar = enemy.AddComponent<EnemyHealthBar>();
            healthBar.maxHealth = 100f;
            healthBar.currentHealth = 100f;
        }
        
        // FirePoint는 EnemyShooter의 Start()에서 자동 생성됨
        
        activeEnemies.Add(enemy);
    }
    
    public int GetActiveEnemyCount()
    {
        return activeEnemies.Count;
    }
    
    public List<GameObject> GetActiveEnemies()
    {
        // 죽은 적 제거
        activeEnemies.RemoveAll(e => e == null);
        return activeEnemies;
    }
}
