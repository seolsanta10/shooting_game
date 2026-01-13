using System;
using UnityEngine;

/// <summary>
/// 플레이어 에너지(체력) 시스템
/// - 기본: 10번 피격(히트)되면 플레이어가 사라짐(SetActive(false))
/// - UI(PlayerEnergyBarUI)에서 좌상단 에너지바로 표시
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Header("에너지(체력) 설정")]
    [Tooltip("총 몇 번 맞으면 사라지는지 (히트 수)")]
    public int maxHits = 10;

    [Tooltip("현재 남은 히트 수")]
    public int hitsRemaining = 10;

    public event Action<int, int> OnChanged; // (remaining, max)
    public event Action OnDead;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // 씬/프리팹 어느 쪽이든 Player에 PlayerHealth가 없으면 자동 부착
        GameObject host = new GameObject("PlayerHealth_Bootstrapper");
        host.AddComponent<PlayerHealthBootstrapper>();
        DontDestroyOnLoad(host);
    }

    private sealed class PlayerHealthBootstrapper : MonoBehaviour
    {
        private float nextTryTime;

        void Update()
        {
            if (Time.time < nextTryTime) return;
            nextTryTime = Time.time + 1f;

            GameObject player = GameObject.Find("Player");
            if (player == null) return;

            if (player.GetComponent<PlayerHealth>() == null)
            {
                player.AddComponent<PlayerHealth>();
            }
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        hitsRemaining = Mathf.Clamp(hitsRemaining, 0, Mathf.Max(1, maxHits));
        if (hitsRemaining == 0) hitsRemaining = maxHits;
        RaiseChanged();
    }

    public void TakeHit(int hits = 1)
    {
        if (!gameObject.activeInHierarchy) return;
        if (maxHits <= 0) maxHits = 10;

        hits = Mathf.Max(1, hits);
        hitsRemaining = Mathf.Max(0, hitsRemaining - hits);
        RaiseChanged();

        if (hitsRemaining <= 0)
        {
            OnDead?.Invoke();
            // "사라지게"
            gameObject.SetActive(false);
        }
    }

    public void ResetHealth()
    {
        maxHits = Mathf.Max(1, maxHits);
        hitsRemaining = maxHits;
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        OnChanged?.Invoke(hitsRemaining, maxHits);
    }
}

