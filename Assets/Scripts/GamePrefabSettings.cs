using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프로젝트에서 사용하는 프리팹(플레이어/적/총알/지면)을 한 곳에서 관리하기 위한 설정.
/// - Resources/GamePrefabSettings.asset 로 만들어두면 런타임에서도 자동 로드됩니다.
/// - 프리팹이 비어있으면 기존 코드의 "기본 프리미티브 생성" 로직이 그대로 동작합니다.
/// </summary>
[CreateAssetMenu(menuName = "TERRIFYING_FLIGHT/Game Prefab Settings", fileName = "GamePrefabSettings")]
public class GamePrefabSettings : ScriptableObject
{
    [Header("World")]
    public GameObject groundPrefab; // 행성/지면(중심) 프리팹

    [Header("Player")]
    public GameObject playerPrefab;
    public GameObject playerMissilePrefab;

    [Header("Enemy")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();
    public GameObject enemyBulletPrefab;

    public static GamePrefabSettings LoadOrNull()
    {
        // Resources/GamePrefabSettings.asset 를 사용
        return Resources.Load<GamePrefabSettings>("GamePrefabSettings");
    }
}

