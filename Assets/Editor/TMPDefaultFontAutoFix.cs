// TMP(Font Asset) 미설정으로 발생하는
// "Can't Generate Mesh, No Font Asset has been assigned." 에러를 자동 완화합니다.
//
// - TMP Essential Resources가 이미 들어와 있는데도 defaultFontAsset이 비어있는 경우를 대비
// - TMP_Settings.defaultFontAsset가 static/instance 어떤 형태든 리플렉션으로 대응
// - 에디터에서만 동작

#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TERRIFYING_FLIGHT.Editor
{
    [InitializeOnLoad]
    public static class TMPDefaultFontAutoFix
    {
        private const string TriedKey = "TERRIFYING_FLIGHT.TMPDefaultFontAutoFix.Tried";
        private const string TmpSettingsAssetPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string DefaultFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        static TMPDefaultFontAutoFix()
        {
            // 에디터 시작 직후 한 번만 시도 (무한 루프 방지)
            if (EditorPrefs.GetBool(TriedKey, false))
                return;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    TryFix();
                }
                finally
                {
                    EditorPrefs.SetBool(TriedKey, true);
                }
            };
        }

        [MenuItem("Tools/TMP/Fix Default Font")]
        private static void MenuFix()
        {
            TryFix(forceLog: true);
        }

        private static void TryFix(bool forceLog = false)
        {
            // TMP 패키지 유무 확인
            Type settingsType = Type.GetType("TMPro.TMP_Settings, Unity.TextMeshPro") ??
                                Type.GetType("TMPro.TMP_Settings, UnityEngine.UI");
            if (settingsType == null)
            {
                if (forceLog) Debug.LogWarning("[TMPDefaultFontAutoFix] TMP_Settings 타입을 찾을 수 없습니다.");
                return;
            }

            // 기본 폰트 에셋 로드 (우선 LiberationSans SDF)
            UnityEngine.Object fontAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DefaultFontAssetPath);
            if (fontAsset == null)
            {
                // 프로젝트에 있는 아무 TMP_FontAsset이라도 하나 찾기
                string[] guids = AssetDatabase.FindAssets("t:TMPro.TMP_FontAsset");
                if (guids != null && guids.Length > 0)
                {
                    string p = AssetDatabase.GUIDToAssetPath(guids[0]);
                    fontAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p);
                }
            }

            if (fontAsset == null)
            {
                if (forceLog) Debug.LogWarning("[TMPDefaultFontAutoFix] TMP 폰트 에셋을 찾을 수 없습니다. TMP Essential Resources를 먼저 임포트하세요.");
                return;
            }

            // defaultFontAsset 프로퍼티 찾기(Static/Instance 모두)
            PropertyInfo prop = settingsType.GetProperty(
                "defaultFontAsset",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance
            );
            if (prop == null || !prop.CanWrite)
            {
                if (forceLog) Debug.LogWarning("[TMPDefaultFontAutoFix] TMP_Settings.defaultFontAsset 프로퍼티를 찾을 수 없습니다.");
                return;
            }

            object current = null;
            bool isStatic = (prop.GetGetMethod(true)?.IsStatic ?? false) || (prop.GetSetMethod(true)?.IsStatic ?? false);

            if (isStatic)
            {
                current = prop.GetValue(null, null);
                if (current != null) return; // 이미 세팅됨
                prop.SetValue(null, fontAsset, null);
            }
            else
            {
                // instance 접근: TMP_Settings.instance 가져오기
                PropertyInfo instProp = settingsType.GetProperty("instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                object inst = instProp?.GetValue(null, null);

                // instance가 null이면 asset을 직접 로드해본다
                if (inst == null)
                {
                    UnityEngine.Object settingsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(TmpSettingsAssetPath);
                    inst = settingsAsset;
                }

                if (inst == null) return;
                current = prop.GetValue(inst, null);
                if (current != null) return; // 이미 세팅됨
                prop.SetValue(inst, fontAsset, null);

                EditorUtility.SetDirty((UnityEngine.Object)inst);
            }

            AssetDatabase.SaveAssets();
            if (forceLog)
            {
                Debug.Log("[TMPDefaultFontAutoFix] TMP defaultFontAsset를 자동 설정했습니다.");
            }
        }
    }
}
#endif

