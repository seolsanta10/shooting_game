using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 좌상단 플레이어 에너지바(UI) 자동 생성
/// - 10대 맞으면 사라짐(PlayerHealth)과 연동
/// - TMP 미사용 (UnityEngine.UI.Text 사용)
/// </summary>
public class PlayerEnergyBarUI : MonoBehaviour
{
    private static PlayerEnergyBarUI instance;

    [Header("오브젝트 이름(중복 생성 방지)")]
    public string panelName = "PlayerEnergyPanel";
    public string hudCanvasName = "HUDCanvas";
    public int hudSortingOrder = 1000;

    [Header("레이아웃")]
    public Vector2 panelSize = new Vector2(260f, 34f);
    public Vector2 panelOffset = new Vector2(20f, -20f); // 좌상단 기준

    [Header("색상")]
    public Color panelBgColor = new Color(0f, 0f, 0f, 0.5f);
    public Color fillColor = new Color(0f, 1f, 0f, 0.85f);
    public Color borderColor = new Color(0f, 1f, 0f, 0.35f);

    private Image fill;
    private Text label;

    private PlayerHealth cachedHealth;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject host = new GameObject("PlayerEnergyBarUI_Auto");
        instance = host.AddComponent<PlayerEnergyBarUI>();
    }

    void Start()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureUI();
    }

    void Update()
    {
        EnsureHealthRef();
    }

    private void EnsureHealthRef()
    {
        if (cachedHealth != null) return;
        GameObject player = GameObject.Find("Player");
        if (player == null) return;

        cachedHealth = player.GetComponent<PlayerHealth>();
        if (cachedHealth == null) return;

        cachedHealth.OnChanged += OnHealthChanged;
        cachedHealth.OnDead += OnDead;
        OnHealthChanged(cachedHealth.hitsRemaining, cachedHealth.maxHits);
    }

    private void OnHealthChanged(int remaining, int max)
    {
        if (fill != null)
        {
            float f = max > 0 ? (float)remaining / max : 0f;
            fill.fillAmount = Mathf.Clamp01(f);

            // 낮을수록 빨강 느낌(간단)
            if (f < 0.3f) fill.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            else if (f < 0.6f) fill.color = new Color(1f, 0.85f, 0.1f, 0.9f);
            else fill.color = fillColor;
        }

        if (label != null)
        {
            label.text = $"ENERGY {remaining}/{max}";
        }
    }

    private void OnDead()
    {
        if (label != null) label.text = "ENERGY 0/0";
    }

    private void EnsureUI()
    {
        GameObject existing = GameObject.Find(panelName);
        if (existing != null)
        {
            // 이미 있으면 참조만 잡고 종료
            fill = existing.transform.Find("Fill")?.GetComponent<Image>();
            label = existing.transform.Find("Label")?.GetComponent<Text>();
            return;
        }

        Canvas canvas = GetOrCreateHudCanvas();

        GameObject panelObj = new GameObject(panelName);
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform prt = panelObj.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 1f);
        prt.anchorMax = new Vector2(0f, 1f);
        prt.pivot = new Vector2(0f, 1f);
        prt.anchoredPosition = panelOffset;
        prt.sizeDelta = panelSize;

        Image bg = panelObj.AddComponent<Image>();
        bg.color = panelBgColor;

        // Border
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(panelObj.transform, false);
        RectTransform brt = borderObj.AddComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.sizeDelta = Vector2.zero;
        Image border = borderObj.AddComponent<Image>();
        border.color = borderColor;

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(panelObj.transform, false);
        RectTransform frt = fillObj.AddComponent<RectTransform>();
        frt.anchorMin = new Vector2(0f, 0f);
        frt.anchorMax = new Vector2(1f, 1f);
        frt.sizeDelta = new Vector2(-8f, -8f);
        frt.anchoredPosition = new Vector2(4f, -4f);

        fill = fillObj.AddComponent<Image>();
        fill.color = fillColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(panelObj.transform, false);
        RectTransform lrt = labelObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.sizeDelta = Vector2.zero;

        label = labelObj.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 16;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.text = "ENERGY 10/10";
    }

    private Canvas GetOrCreateHudCanvas()
    {
        GameObject hudObj = GameObject.Find(hudCanvasName);
        Canvas canvas = null;
        if (hudObj != null)
        {
            canvas = hudObj.GetComponent<Canvas>();
        }

        if (canvas == null)
        {
            GameObject canvasObj = new GameObject(hudCanvasName);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = hudSortingOrder;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasObj);
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = hudSortingOrder;
            DontDestroyOnLoad(canvas.gameObject);
        }

        return canvas;
    }
}

