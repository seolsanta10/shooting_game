#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TERRIFYING_FLIGHT.Editor
{
    public static class GamePrefabSettingsCreator
    {
        private const string ResourcesDir = "Assets/Resources";
        private const string AssetPath = "Assets/Resources/GamePrefabSettings.asset";

        [MenuItem("Tools/Game/Create or Select GamePrefabSettings")]
        private static void CreateOrSelect()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GamePrefabSettings>(AssetPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            if (!Directory.Exists(ResourcesDir))
            {
                Directory.CreateDirectory(ResourcesDir);
                AssetDatabase.Refresh();
            }

            var asset = ScriptableObject.CreateInstance<GamePrefabSettings>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            Debug.Log("[GamePrefabSettingsCreator] GamePrefabSettings.asset 생성 완료. 여기서 프리팹을 연결하세요.");
        }
    }
}
#endif

