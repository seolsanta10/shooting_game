using UnityEngine;
using System.Collections.Generic;

public class MonsterSpawner : MonoBehaviour
{
    [Header("데이터베이스")]
    public MonsterDatabase monsterDatabase;
    
    [Header("스폰 설정")]
    public Transform planetCenter;
    public float spawnRadius = 30f; // 지구 중심으로부터의 거리
    public float spawnHeight = 30f; // 고도
    public int maxMonsters = 20; // 최대 동시 존재 몬스터 수
    public float spawnInterval = 3f; // 스폰 간격 (초)
    
    [Header("레벨 시스템")]
    public int currentLevel = 1;
    public int monstersPerLevel = 10; // 레벨당 처치해야 할 몬스터 수
    private int monstersKilledThisLevel = 0;
    
    private List<GameObject> activeMonsters = new List<GameObject>();
    private float lastSpawnTime = 0f;
    
    void Start()
    {
        if (planetCenter == null)
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground == null) ground = GameObject.Find("지구");
            if (ground != null)
            {
                planetCenter = ground.transform;
            }
        }
        
        // 초기 몬스터 스폰
        SpawnInitialMonsters();
    }
    
    void Update()
    {
        // 주기적으로 몬스터 스폰
        if (Time.time - lastSpawnTime >= spawnInterval)
        {
            if (activeMonsters.Count < maxMonsters)
            {
                SpawnMonster();
                lastSpawnTime = Time.time;
            }
        }
        
        // 죽은 몬스터 제거
        activeMonsters.RemoveAll(m => m == null);
    }
    
    void SpawnInitialMonsters()
    {
        int initialCount = Mathf.Min(5, maxMonsters);
        for (int i = 0; i < initialCount; i++)
        {
            SpawnMonster();
        }
    }
    
    public void SpawnMonster()
    {
        if (monsterDatabase == null || planetCenter == null) return;
        
        // 레벨에 맞는 몬스터 선택
        MonsterData monsterData = monsterDatabase.GetRandomMonster(currentLevel);
        if (monsterData == null || monsterData.prefab == null) return;
        
        // 랜덤 위치 계산 (지구 주변)
        Vector3 randomDirection = Random.onUnitSphere;
        float planetRadius = 25f; // Ground 스케일의 절반
        Vector3 spawnPosition = planetCenter.position + randomDirection * (planetRadius + spawnHeight);
        
        // 몬스터 생성
        GameObject monster = Instantiate(monsterData.prefab, spawnPosition, Quaternion.identity);
        monster.name = monsterData.monsterName;
        
        // 몬스터 컴포넌트 설정
        MonsterController controller = monster.GetComponent<MonsterController>();
        if (controller == null)
        {
            controller = monster.AddComponent<MonsterController>();
        }
        controller.Initialize(monsterData, planetCenter);
        
        activeMonsters.Add(monster);
    }
    
    public void OnMonsterKilled()
    {
        monstersKilledThisLevel++;
        
        if (monstersKilledThisLevel >= monstersPerLevel)
        {
            LevelUp();
        }
    }
    
    void LevelUp()
    {
        currentLevel++;
        monstersKilledThisLevel = 0;
        Debug.Log($"레벨 업! 현재 레벨: {currentLevel}");
        
        // 레벨업 시 더 강한 몬스터 스폰 확률 증가
        spawnInterval = Mathf.Max(1f, spawnInterval - 0.1f);
    }
    
    public int GetActiveMonsterCount()
    {
        return activeMonsters.Count;
    }
}
