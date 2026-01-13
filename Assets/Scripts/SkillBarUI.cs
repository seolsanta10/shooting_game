using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오른쪽 아래 스킬창(UI) 자동 생성
/// - 슬롯 3개 (1 / 2 / 3)
/// - TMP 미사용(기본 UI.Text 사용)
/// </summary>
public class SkillBarUI : MonoBehaviour
{
    private static SkillBarUI instance;
    public static SkillBarUI Instance => instance;

    [Header("레이아웃")]
    public Vector2 panelSize = new Vector2(260f, 80f);
    public Vector2 panelOffset = new Vector2(-20f, 20f); // 오른쪽 아래 기준 오프셋
    public float slotSize = 60f;
    public float slotSpacing = 12f;

    [Header("색상")]
    public Color panelBgColor = new Color(0f, 0f, 0f, 0.35f);
    public Color slotBgColor = new Color(0.1f, 0.1f, 0.1f, 0.75f);
    public Color slotBorderColor = new Color(0f, 1f, 0f, 0.45f);
    public Color keyTextColor = new Color(0f, 1f, 0f, 0.9f);

    [Header("오브젝트 이름(중복 생성 방지)")]
    public string panelName = "SkillBarPanel";
    public string hudCanvasName = "HUDCanvas";
    public int hudSortingOrder = 1000;

    private RectTransform panelRect;
    private Image[] slotIcons = new Image[3]; // 0/1/2 => 슬롯 1/2/3

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // 씬에 아무 것도 안 붙여도 항상 생성되게
        if (instance != null) return;

        GameObject host = new GameObject("SkillBarUI_Auto");
        instance = host.AddComponent<SkillBarUI>();
    }

    void Start()
    {
        // 항상 보이도록: 싱글톤 + DontDestroyOnLoad
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureSkillBar();
    }

    void EnsureSkillBar()
    {
        // 이미 있으면 재사용
        GameObject existing = GameObject.Find(panelName);
        if (existing != null)
        {
            panelRect = existing.GetComponent<RectTransform>();
            return;
        }

        Canvas canvas = GetOrCreateHudCanvas();

        // 패널 생성
        GameObject panelObj = new GameObject(panelName);
        panelObj.transform.SetParent(canvas.transform, false);

        panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = panelOffset;
        panelRect.sizeDelta = panelSize;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = panelBgColor;

        // 슬롯 3개 생성
        CreateSlot(panelObj.transform, index: 1);
        CreateSlot(panelObj.transform, index: 2);
        CreateSlot(panelObj.transform, index: 3);
    }

    public void SetSlotItem(int slotNumber1To3, ItemPickup.ItemInfo item)
    {
        int idx = slotNumber1To3 - 1;
        if (idx < 0 || idx >= slotIcons.Length) return;
        if (slotIcons[idx] == null) return;

        // 색상 아이콘(원형) 생성해서 표시
        slotIcons[idx].sprite = CreateCircleSprite(item.iconColor, 64);
        slotIcons[idx].color = Color.white;
    }

    public void ClearSlot(int slotNumber1To3)
    {
        int idx = slotNumber1To3 - 1;
        if (idx < 0 || idx >= slotIcons.Length) return;
        if (slotIcons[idx] == null) return;

        slotIcons[idx].sprite = null;
        slotIcons[idx].color = new Color(1f, 1f, 1f, 0.05f);
    }

    private Sprite CreateCircleSprite(Color color, int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
        float r = size * 0.48f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                if (d <= r) tex.SetPixel(x, y, color);
                else tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    Canvas GetOrCreateHudCanvas()
    {
        // HUD 전용 캔버스가 있으면 그걸 사용
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
            // 혹시 다른 캔버스 설정이 바뀌었으면 보정
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = hudSortingOrder;
            DontDestroyOnLoad(canvas.gameObject);
        }

        return canvas;
    }

    void CreateSlot(Transform parent, int index)
    {
        GameObject slotObj = new GameObject($"SkillSlot{index}");
        slotObj.transform.SetParent(parent, false);

        RectTransform rt = slotObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(slotSize, slotSize);

        float totalWidth = (slotSize * 3f) + (slotSpacing * 2f);
        float rightEdge = -12f; // 패널 안쪽 여백
        float xFromRight = rightEdge - ((index - 1) * (slotSize + slotSpacing));
        rt.anchoredPosition = new Vector2(xFromRight, 0f);

        // 배경
        Image bg = slotObj.AddComponent<Image>();
        bg.color = slotBgColor;

        // 테두리(링) 비슷하게: 별도 자식 이미지
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(slotObj.transform, false);
        RectTransform brt = borderObj.AddComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.sizeDelta = Vector2.zero;
        brt.anchoredPosition = Vector2.zero;

        Image border = borderObj.AddComponent<Image>();
        border.color = slotBorderColor;

        // 가운데 아이콘 자리(나중에 스프라이트 교체 가능)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slotObj.transform, false);
        RectTransform irt = iconObj.AddComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.5f, 0.5f);
        irt.anchorMax = new Vector2(0.5f, 0.5f);
        irt.pivot = new Vector2(0.5f, 0.5f);
        irt.sizeDelta = new Vector2(slotSize - 12f, slotSize - 12f);
        irt.anchoredPosition = Vector2.zero;

        Image icon = iconObj.AddComponent<Image>();
        icon.color = new Color(1f, 1f, 1f, 0.05f);
        if (index >= 1 && index <= 3)
        {
            slotIcons[index - 1] = icon;
        }

        // 키 텍스트 (1/2/3)
        GameObject keyObj = new GameObject("KeyText");
        keyObj.transform.SetParent(slotObj.transform, false);
        RectTransform krt = keyObj.AddComponent<RectTransform>();
        krt.anchorMin = new Vector2(0f, 1f);
        krt.anchorMax = new Vector2(0f, 1f);
        krt.pivot = new Vector2(0f, 1f);
        krt.anchoredPosition = new Vector2(6f, -4f);
        krt.sizeDelta = new Vector2(20f, 20f);

        Text t = keyObj.AddComponent<Text>();
        // Unity 최신 버전: Arial.ttf 내장 폰트 제거됨 → LegacyRuntime.ttf 사용
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 16;
        t.color = keyTextColor;
        t.alignment = TextAnchor.UpperLeft;
        t.text = index.ToString();
    }
}

