using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "Monster Database", menuName = "비행 게임/몬스터 데이터베이스")]
public class MonsterDatabase : ScriptableObject
{
    [Header("모든 몬스터 데이터")]
    public List<MonsterData> allMonsters = new List<MonsterData>();
    
    [Header("카테고리별 분류")]
    public List<MonsterData> tier1Monsters = new List<MonsterData>(); // 레벨 1-3
    public List<MonsterData> tier2Monsters = new List<MonsterData>(); // 레벨 4-6
    public List<MonsterData> tier3Monsters = new List<MonsterData>(); // 레벨 7-9
    public List<MonsterData> tier4Monsters = new List<MonsterData>(); // 레벨 10+
    
    /// <summary>
    /// 이름으로 몬스터 데이터 찾기
    /// </summary>
    public MonsterData GetMonsterByName(string name)
    {
        return allMonsters.FirstOrDefault(m => m.monsterName == name);
    }
    
    /// <summary>
    /// 레벨에 맞는 몬스터 리스트 가져오기
    /// </summary>
    public List<MonsterData> GetMonstersByLevel(int level)
    {
        return allMonsters.Where(m => level >= m.minLevel && level <= m.maxLevel).ToList();
    }
    
    /// <summary>
    /// 티어에 맞는 몬스터 리스트 가져오기
    /// </summary>
    public List<MonsterData> GetMonstersByTier(int tier)
    {
        switch (tier)
        {
            case 1: return tier1Monsters;
            case 2: return tier2Monsters;
            case 3: return tier3Monsters;
            case 4: return tier4Monsters;
            default: return allMonsters;
        }
    }
    
    /// <summary>
    /// 가중치 기반 랜덤 몬스터 선택
    /// </summary>
    public MonsterData GetRandomMonster(int level = 1)
    {
        var availableMonsters = GetMonstersByLevel(level);
        if (availableMonsters.Count == 0) return null;
        
        float totalWeight = availableMonsters.Sum(m => m.spawnWeight);
        float random = Random.Range(0f, totalWeight);
        
        float currentWeight = 0f;
        foreach (var monster in availableMonsters)
        {
            currentWeight += monster.spawnWeight;
            if (random <= currentWeight)
            {
                return monster;
            }
        }
        
        return availableMonsters[0];
    }
    
    /// <summary>
    /// 데이터베이스 자동 정리 (에디터에서 호출)
    /// </summary>
    [ContextMenu("카테고리 자동 분류")]
    public void AutoCategorize()
    {
        tier1Monsters.Clear();
        tier2Monsters.Clear();
        tier3Monsters.Clear();
        tier4Monsters.Clear();
        
        foreach (var monster in allMonsters)
        {
            if (monster.maxLevel <= 3)
                tier1Monsters.Add(monster);
            else if (monster.maxLevel <= 6)
                tier2Monsters.Add(monster);
            else if (monster.maxLevel <= 9)
                tier3Monsters.Add(monster);
            else
                tier4Monsters.Add(monster);
        }
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
}
