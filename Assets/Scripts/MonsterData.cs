using UnityEngine;

[CreateAssetMenu(fileName = "New Monster", menuName = "비행 게임/몬스터 데이터")]
public class MonsterData : ScriptableObject
{
    [Header("기본 정보")]
    public string monsterName;
    public GameObject prefab;
    public Sprite icon;
    
    [Header("스탯")]
    public int health = 100;
    public float moveSpeed = 3f;
    public float rotationSpeed = 90f;
    
    [Header("전투")]
    public int damage = 10;
    public float attackRange = 5f;
    public float attackCooldown = 2f;
    
    [Header("보상")]
    public int score = 100;
    public int exp = 50;
    
    [Header("시각적")]
    public Color color = Color.white;
    public float scale = 1f;
    
    [Header("스폰 설정")]
    public float spawnWeight = 1f; // 스폰 확률 가중치
    public int minLevel = 1; // 최소 레벨
    public int maxLevel = 10; // 최대 레벨
}
