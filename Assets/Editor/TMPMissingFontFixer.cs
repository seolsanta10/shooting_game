#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// TextMeshPro가 없으면 컴파일 자체가 실패하므로 분리
// (프로젝트에 TMP가 이미 들어와 있는 상태를 전제로 함)
using TMPro;

namespace TERRIFYING_FLIGHT.Editor
{
    /// <summary>
    /// "Can't Generate Mesh, No Font Asset has been assigned." 경고의 근본 원인:
    /// TMP_Text(Font Asset)이 비어있는 오브젝트가 존재함.
    ///
    /// 이 툴은:
    /// - 현재 열려있는 씬(loaded scenes) 안의 TMP_Text 중 font == null 을 defaultFontAsset로 채움
    /// - 프로젝트의 모든 Prefab 안 TMP_Text 중 font == null 을 defaultFontAsset로 채움
    /// </summary>
    public static class TMPMissingFontFixer
    {
        private const string DefaultFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [MenuItem("Tools/TMP/List Missing Font Assets (No Changes)")]
        private static void ListOnly()
        {
            int sceneCount = ListMissingInLoadedScenes();
            int prefabCount = ListMissingInAllPrefabs();
            Debug.Log($"[TMPMissingFontFixer] 누락 폰트 목록 출력 완료: 씬 {sceneCount}개, 프리팹 {prefabCount}개 (font==null)");
        }

        [MenuItem("Tools/TMP/Deep Scan Missing Font (Includes Hidden/Packages)")]
        private static void DeepScan()
        {
            // 에디터에 존재하는 TMP_Text를 전부 찾음(숨김/프리뷰 포함)
            TMP_Text[] all = Resources.FindObjectsOfTypeAll<TMP_Text>();
            int count = 0;
            foreach (TMP_Text t in all)
            {
                if (t == null) continue;
                if (t.font != null) continue;

                count++;

                string sceneName = "<none>";
                try
                {
                    if (t.gameObject != null && t.gameObject.scene.IsValid())
                        sceneName = t.gameObject.scene.name;
                }
                catch { /* ignore */ }

                string objPath = t.transform != null ? GetTransformPath(t.transform) : "<no-transform>";

                // asset path(프리팹/패키지/프리뷰) 추정
                string assetPath = AssetDatabase.GetAssetPath(t);
                if (string.IsNullOrEmpty(assetPath))
                {
                    try
                    {
                        assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                    }
                    catch { /* ignore */ }
                }
                if (string.IsNullOrEmpty(assetPath)) assetPath = "<unknown>";

                Debug.LogWarning(
                    $"[TMPMissingFontFixer] (DEEP) font 비어있음: scene='{sceneName}' asset='{assetPath}' object='{objPath}' component='{t.GetType().Name}' hideFlags='{t.hideFlags}' goHideFlags='{(t.gameObject != null ? t.gameObject.hideFlags.ToString() : "<null>")}'",
                    t
                );
            }

            Debug.Log($"[TMPMissingFontFixer] Deep Scan 완료: font==null TMP_Text {count}개");
        }

        [MenuItem("Tools/TMP/Find & Fix Missing Font Assets")]
        private static void FindAndFix()
        {
            TMP_FontAsset defaultFont = GetDefaultFont();
            if (defaultFont == null)
            {
                Debug.LogWarning("[TMPMissingFontFixer] default TMP_FontAsset을 찾을 수 없습니다. TMP Essential Resources가 정상 임포트되었는지 확인하세요.");
                return;
            }

            int fixedInScenes = FixLoadedScenes(defaultFont);
            int fixedInPrefabs = FixAllPrefabs(defaultFont);

            AssetDatabase.SaveAssets();

            Debug.Log($"[TMPMissingFontFixer] 완료: 씬 수정 {fixedInScenes}개, 프리팹 수정 {fixedInPrefabs}개. (font==null → {defaultFont.name})");
        }

        private static TMP_FontAsset GetDefaultFont()
        {
            // TMP Settings에 기본 폰트가 설정되어 있으면 그걸 사용
            if (TMP_Settings.defaultFontAsset != null)
                return TMP_Settings.defaultFontAsset;

            // 아니면 패키지 기본 리소스 경로에서 로드 시도
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontAssetPath);
        }

        private static int FixLoadedScenes(TMP_FontAsset defaultFont)
        {
            int fixedCount = 0;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                bool sceneModified = false;
                GameObject[] roots = scene.GetRootGameObjects();
                foreach (GameObject root in roots)
                {
                    if (root == null) continue;
                    TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                    foreach (TMP_Text t in texts)
                    {
                        if (t == null) continue;
                        if (t.font != null) continue;

                        Debug.LogWarning($"[TMPMissingFontFixer] (SCENE) font 비어있음: scene='{scene.name}' object='{GetTransformPath(t.transform)}' component='{t.GetType().Name}'", t);
                        Undo.RecordObject(t, "Fix TMP missing font");
                        t.font = defaultFont;
                        EditorUtility.SetDirty(t);
                        fixedCount++;
                        sceneModified = true;
                    }
                }

                if (sceneModified)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }

            return fixedCount;
        }

        private static int FixAllPrefabs(TMP_FontAsset defaultFont)
        {
            int fixedCount = 0;

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                // Assets/와 Packages/ 모두 처리 (패키지 내부 프리팹이 원인일 수도 있음)
                bool isAssets = path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
                bool isPackages = path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase);
                if (!isAssets && !isPackages) continue;

                // Prefab 내용을 로드해서 수정
                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    if (root == null) continue;

                    bool modified = false;
                    TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                    foreach (TMP_Text t in texts)
                    {
                        if (t == null) continue;
                        if (t.font != null) continue;

                        Debug.LogWarning($"[TMPMissingFontFixer] (PREFAB) font 비어있음: prefab='{path}' object='{GetTransformPath(t.transform)}' component='{t.GetType().Name}'");
                        t.font = defaultFont;
                        EditorUtility.SetDirty(t);
                        fixedCount++;
                        modified = true;
                    }

                    if (modified)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TMPMissingFontFixer] 프리팹 처리 실패: {path}\n{e.Message}");
                }
                finally
                {
                    if (root != null)
                        PrefabUtility.UnloadPrefabContents(root);
                }
            }

            return fixedCount;
        }

        private static int ListMissingInLoadedScenes()
        {
            int count = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                GameObject[] roots = scene.GetRootGameObjects();
                foreach (GameObject root in roots)
                {
                    if (root == null) continue;
                    TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                    foreach (TMP_Text t in texts)
                    {
                        if (t == null) continue;
                        if (t.font != null) continue;
                        count++;
                        Debug.LogWarning($"[TMPMissingFontFixer] (SCENE) font 비어있음: scene='{scene.name}' object='{GetTransformPath(t.transform)}' component='{t.GetType().Name}'", t);
                    }
                }
            }
            return count;
        }

        private static int ListMissingInAllPrefabs()
        {
            int count = 0;
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                bool isAssets = path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
                bool isPackages = path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase);
                if (!isAssets && !isPackages) continue;

                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    if (root == null) continue;

                    TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                    foreach (TMP_Text t in texts)
                    {
                        if (t == null) continue;
                        if (t.font != null) continue;
                        count++;
                        Debug.LogWarning($"[TMPMissingFontFixer] (PREFAB) font 비어있음: prefab='{path}' object='{GetTransformPath(t.transform)}' component='{t.GetType().Name}'");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TMPMissingFontFixer] 프리팹 스캔 실패: {path}\n{e.Message}");
                }
                finally
                {
                    if (root != null)
                        PrefabUtility.UnloadPrefabContents(root);
                }
            }
            return count;
        }

        private static string GetTransformPath(Transform t)
        {
            if (t == null) return "<null>";
            string path = t.name;
            Transform p = t.parent;
            while (p != null)
            {
                path = p.name + "/" + path;
                p = p.parent;
            }
            return path;
        }
    }
}
#endif

