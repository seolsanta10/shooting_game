#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace TERRIFYING_FLIGHT.Editor
{
    /// <summary>
    /// 에디터에서 가끔 "Can't Generate Mesh, No Font Asset has been assigned." 경고가
    /// 씬/프리팹 스캔에 잡히지 않는 형태로 반복되는 경우가 있습니다.
    /// (숨김 오브젝트, 프리뷰 오브젝트, 패키지 프리팹 인스턴스 등)
    ///
    /// 이 감시자는:
    /// - 일정 주기(기본 1초)로 Resources.FindObjectsOfTypeAll<TMP_Text>()를 스캔
    /// - font == null 인 TMP_Text를 발견하면 위치(씬/에셋/계층 경로)를 콘솔에 출력
    /// - 씬/임시 오브젝트(비-퍼시스턴트)는 defaultFontAsset로 즉시 채워서 경고를 멈춤
    ///
    /// 주의: Packages/ 아래 퍼시스턴트 에셋은 자동 수정하지 않고 경로만 로그로 남깁니다.
    /// </summary>
    [InitializeOnLoad]
    public static class TMPMissingFontWatcher
    {
        private const string EnabledKey = "TERRIFYING_FLIGHT.TMPMissingFontWatcher.Enabled";
        private const double IntervalSeconds = 1.0;

        private static double _nextScanTime;

        static TMPMissingFontWatcher()
        {
            if (!EditorPrefs.GetBool(EnabledKey, true))
                return;

            // defaultFontAsset이 비어있으면 최소한 기본값을 잡아둔다
            TryEnsureDefaultFont();

            _nextScanTime = EditorApplication.timeSinceStartup + 0.5;
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        [MenuItem("Tools/TMP/Toggle Missing Font Watcher")]
        private static void Toggle()
        {
            bool enabled = EditorPrefs.GetBool(EnabledKey, true);
            enabled = !enabled;
            EditorPrefs.SetBool(EnabledKey, enabled);

            if (enabled)
            {
                TryEnsureDefaultFont();
                _nextScanTime = EditorApplication.timeSinceStartup + 0.2;
                EditorApplication.update -= OnUpdate;
                EditorApplication.update += OnUpdate;
                Debug.Log("[TMPMissingFontWatcher] Enabled");
            }
            else
            {
                EditorApplication.update -= OnUpdate;
                Debug.Log("[TMPMissingFontWatcher] Disabled");
            }
        }

        private static void OnUpdate()
        {
            if (EditorApplication.timeSinceStartup < _nextScanTime)
                return;

            _nextScanTime = EditorApplication.timeSinceStartup + IntervalSeconds;

            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
            {
                // default가 없으면 감시해도 채울 수가 없음. 로그만 남기고 종료.
                Debug.LogWarning("[TMPMissingFontWatcher] TMP_Settings.defaultFontAsset 가 null 입니다. TMP Settings에서 기본 폰트를 지정하세요.");
                return;
            }

            TMP_Text[] all = Resources.FindObjectsOfTypeAll<TMP_Text>();
            foreach (TMP_Text t in all)
            {
                if (t == null) continue;
                if (t.font != null) continue;

                string assetPath = AssetDatabase.GetAssetPath(t);
                if (string.IsNullOrEmpty(assetPath))
                {
                    try
                    {
                        if (t.gameObject != null)
                            assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                    }
                    catch { /* ignore */ }
                }
                if (string.IsNullOrEmpty(assetPath)) assetPath = "<unknown>";

                string objPath = t.transform != null ? GetTransformPath(t.transform) : "<no-transform>";
                string sceneName = "<none>";
                try
                {
                    if (t.gameObject != null && t.gameObject.scene.IsValid())
                        sceneName = t.gameObject.scene.name;
                }
                catch { /* ignore */ }

                bool isPersistent = EditorUtility.IsPersistent(t);
                Debug.LogWarning(
                    $"[TMPMissingFontWatcher] font 비어있음 감지: scene='{sceneName}' asset='{assetPath}' object='{objPath}' persistent={isPersistent} component='{t.GetType().Name}'",
                    t
                );

                // 씬/임시(비-퍼시스턴트) 오브젝트는 즉시 폰트를 채워 경고를 멈춤
                if (!isPersistent)
                {
                    try
                    {
                        t.font = defaultFont;
                        EditorUtility.SetDirty(t);
                    }
                    catch { /* ignore */ }
                }
                else
                {
                    // 퍼시스턴트 에셋은 자동 수정 범위를 제한
                    // Assets/ 아래면 수정(저장) 가능, Packages/는 로그만.
                    if (assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            t.font = defaultFont;
                            EditorUtility.SetDirty(t);
                            // 저장은 즉시 하지 않고, 사용자가 Fix 메뉴를 돌리거나 저장 시 반영되도록 둠
                        }
                        catch { /* ignore */ }
                    }
                }
            }
        }

        private static void TryEnsureDefaultFont()
        {
            if (TMP_Settings.defaultFontAsset != null) return;

            // 프로젝트에 TMP Essential Resources가 들어있다면 기본 LiberationSans SDF가 존재함
            const string defaultPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(defaultPath);
            if (font != null)
            {
                TMP_Settings.defaultFontAsset = font;
                EditorUtility.SetDirty(TMP_Settings.instance);
                AssetDatabase.SaveAssets();
            }
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

