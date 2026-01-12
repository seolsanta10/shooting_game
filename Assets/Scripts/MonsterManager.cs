using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 몬스터 데이터베이스를 쉽게 관리하기 위한 에디터 도구
/// </summary>
public class MonsterManager : MonoBehaviour
{
    [Header("데이터베이스")]
    public MonsterDatabase database;
    
    [Header("프리팹 폴더")]
    public string prefabFolderPath = "Assets/Prefabs/Monsters";
    
    /// <summary>
    /// 프리팹 폴더에서 모든 프리팹을 찾아서 데이터베이스에 추가
    /// </summary>
    [ContextMenu("프리팹에서 몬스터 데이터 자동 생성")]
    public void AutoCreateMonsterDataFromPrefabs()
    {
        if (database == null)
        {
            Debug.LogError("MonsterDatabase가 설정되지 않았습니다!");
            return;
        }
        
        #if UNITY_EDITOR
        // 프리팹 폴더에서 모든 프리팹 찾기
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolderPath });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                // 이미 데이터가 있는지 확인
                MonsterData existingData = database.allMonsters.Find(m => m.prefab == prefab);
                
                if (existingData == null)
                {
                    // 새 몬스터 데이터 생성
                    MonsterData newData = ScriptableObject.CreateInstance<MonsterData>();
                    newData.monsterName = prefab.name;
                    newData.prefab = prefab;
                    newData.spawnWeight = 1f;
                    newData.minLevel = 1;
                    newData.maxLevel = 10;
                    
                    // 데이터베이스에 추가
                    database.allMonsters.Add(newData);
                    
                    // 파일로 저장
                    string dataPath = prefabFolderPath.Replace("Prefabs", "Data") + "/" + prefab.name + "_Data.asset";
                    AssetDatabase.CreateAsset(newData, dataPath);
                    Debug.Log($"새 몬스터 데이터 생성: {prefab.name}");
                }
            }
        }
        
        // 데이터베이스 저장
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("몬스터 데이터 생성 완료!");
        #endif
    }
}
