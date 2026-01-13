#if UNITY_EDITOR
using System;
using UnityEngine;

namespace TERRIFYING_FLIGHT.Editor
{
    /// <summary>
    /// "Can't Generate Mesh, No Font Asset has been assigned." 경고가
    /// 콘솔에서는 짧은 스택(HandleUtility:BeginHandles 등)만 보이는 경우가 있어
    /// Unity가 제공하는 stackTrace 문자열을 그대로 다시 출력해 원인 추적을 돕습니다.
    /// </summary>
    [UnityEditor.InitializeOnLoad]
    public static class MissingFontAssetTraceLogger
    {
        private const string Target = "Can't Generate Mesh, No Font Asset has been assigned.";
        private static bool _reentryGuard;

        static MissingFontAssetTraceLogger()
        {
            Application.logMessageReceivedThreaded -= OnLog;
            Application.logMessageReceivedThreaded += OnLog;
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (_reentryGuard) return;
            if (string.IsNullOrEmpty(condition)) return;
            if (!condition.Contains(Target, StringComparison.Ordinal)) return;

            try
            {
                _reentryGuard = true;

                // 콘솔에서 클릭 가능한 형태로 남기기 위해 Error로 한 번 더 출력
                Debug.LogError(
                    "[MissingFontAssetTraceLogger] 전체 스택 트레이스:\n" +
                    condition + "\n" +
                    (string.IsNullOrEmpty(stackTrace) ? "<no stackTrace provided>" : stackTrace)
                );
            }
            finally
            {
                _reentryGuard = false;
            }
        }
    }
}
#endif

