using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RadarSystem : MonoBehaviour
{
    [Header("레이더 설정")]
    public RectTransform radarPanel; // 레이더 UI 패널
    public GameObject blipPrefab; // 레이더 블립 프리팹 (원형 이미지)
    public float radarRadius = 100f; // 레이더 반경 (픽셀)
    public float detectionRange = 50f; // 감지 범위 (월드 단위)

    [Header("레이더 비주얼(C안) 설정")]
    [Range(0f, 1f)]
    [Tooltip("레이더 배경 투명도 (0=완전 투명, 1=불투명)")]
    public float backgroundAlpha = 0.3f;

    [Range(0f, 1f)]
    [Tooltip("그리드 투명도")]
    public float gridAlpha = 0.18f;

    [Range(0f, 1f)]
    [Tooltip("스캔라인 투명도")]
    public float scanAlpha = 0.35f;

    [Range(0f, 1f)]
    [Tooltip("십자선 투명도")]
    public float crosshairAlpha = 0.25f;
    
    [Header("참조")]
    public Transform playerTransform;
    public Transform groundCenter;
    
    private List<GameObject> enemyBlips = new List<GameObject>();
    private Dictionary<GameObject, GameObject> enemyToBlipMap = new Dictionary<GameObject, GameObject>();
    private EnemySpawner enemySpawner;
    private GameObject playerBlip; // 플레이어 블립

    // 런타임 생성 스프라이트 캐시
    private Sprite cachedPanelCircleSprite;
    private Sprite cachedPanelBorderSprite;
    private Sprite cachedBlipCircleSprite;
    private readonly Dictionary<int, Sprite> cachedRingSpritesBySize = new Dictionary<int, Sprite>();
    private Material radarScanMaterial;
    
    void Start()
    {
        // 플레이어 찾기
        if (playerTransform == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        // Ground 찾기
        if (groundCenter == null)
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                ground = GameObject.Find("지구");
            }
            if (ground != null)
            {
                groundCenter = ground.transform;
            }
        }
        
        // 레이더 패널 찾기 또는 생성
        if (radarPanel == null)
        {
            // 씬에서 기존 RadarPanel 찾기
            GameObject existingPanel = GameObject.Find("RadarPanel");
            if (existingPanel != null)
            {
                radarPanel = existingPanel.GetComponent<RectTransform>();
                Debug.Log("[RadarSystem] 기존 RadarPanel 찾음");
            }
            else
            {
                // 없으면 자동 생성
                CreateRadarPanel();
            }
        }

        // 씬에 기존 RadarPanel이 있어도 항상 "원형 레이더"로 보정
        EnsureCircularRadarPanel();
        
        // 블립 프리팹 찾기 또는 생성
        if (blipPrefab == null)
        {
            // 씬에서 기존 BlipPrefab 찾기
            GameObject existingBlip = GameObject.Find("BlipPrefab");
            if (existingBlip != null)
            {
                blipPrefab = existingBlip;
                Debug.Log("[RadarSystem] 기존 BlipPrefab 찾음");
            }
            else
            {
                // 없으면 자동 생성
                CreateBlipPrefab();
            }
        }
        
        // EnemySpawner 찾기
        enemySpawner = FindAnyObjectByType<EnemySpawner>();
        
        // 플레이어 블립 생성
        CreatePlayerBlip();
    }
    
    void CreatePlayerBlip()
    {
        if (radarPanel == null || blipPrefab == null) return;
        
        // 플레이어 블립이 없으면 생성
        if (playerBlip == null)
        {
            playerBlip = Instantiate(blipPrefab, radarPanel);
            playerBlip.name = "PlayerBlip";
            playerBlip.SetActive(true);
            
            // 플레이어 블립 스타일 강제 (프리팹이 흰색이어도 무조건 구분되게)
            ConfigureBlip(playerBlip, isPlayer: true);
            
            // 플레이어 블립은 중앙에 위치
            RectTransform playerBlipRect = playerBlip.GetComponent<RectTransform>();
            if (playerBlipRect != null)
            {
                playerBlipRect.anchoredPosition = Vector2.zero; // 중앙
            }
        }
    }
    
    void Update()
    {
        if (playerTransform == null) return;

        // 런타임에 groundCenter가 늦게 생길 수 있으니 재탐색
        if (groundCenter == null)
        {
            GameObject ground = GameObject.Find("Ground");
            if (ground == null) ground = GameObject.Find("지구");
            if (ground != null) groundCenter = ground.transform;
        }

        UpdateRadar();
        UpdatePlayerBlip();
    }
    
    void UpdatePlayerBlip()
    {
        // 플레이어 블립은 항상 중앙에 위치 (레이더는 플레이어 중심)
        if (playerBlip != null && radarPanel != null)
        {
            RectTransform playerBlipRect = playerBlip.GetComponent<RectTransform>();
            if (playerBlipRect != null)
            {
                playerBlipRect.anchoredPosition = Vector2.zero; // 항상 중앙
            }
        }
    }
    
    void CreateRadarPanel()
    {
        // Canvas 찾기 또는 생성
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            Debug.Log("[RadarSystem] Canvas 생성 완료");
        }
        else
        {
            Debug.Log("[RadarSystem] 기존 Canvas 사용");
        }
        
        // 레이더 패널 생성
        GameObject panelObj = new GameObject("RadarPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        
        radarPanel = panelObj.AddComponent<RectTransform>();
        radarPanel.anchorMin = new Vector2(1f, 1f);
        radarPanel.anchorMax = new Vector2(1f, 1f);
        radarPanel.pivot = new Vector2(1f, 1f);
        radarPanel.anchoredPosition = new Vector2(-20f, -20f); // 오른쪽 상단
        radarPanel.sizeDelta = new Vector2(radarRadius * 2f, radarRadius * 2f);

        // 패널을 원형으로 보정(마스크/배경/테두리)
        EnsureCircularRadarPanel();
        
        // 중앙 십자선 (플레이어 위치 표시)
        CreateCenterCrosshair(panelObj.transform);
        
        // 레이더 범위 표시 (원형 그리드)
        CreateRadarGrid(panelObj.transform);
        
        Debug.Log("[RadarSystem] 레이더 패널 자동 생성 완료 - 위치: 오른쪽 상단 (원형)");
        Debug.Log("[RadarSystem] 직접 만든 패널을 사용하려면 Inspector에서 Radar Panel 필드에 할당하세요");
    }
    
    Texture2D CreateCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    Texture2D CreateCircleBorderTexture(int size, int borderWidth)
    {
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerRadius = size * 0.5f;
        float innerRadius = outerRadius - borderWidth;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= outerRadius && distance >= innerRadius)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    void CreateCenterCrosshair(Transform parent)
    {
        // 수평선
        GameObject hLine = new GameObject("CenterHLine");
        hLine.transform.SetParent(parent, false);
        RectTransform hRect = hLine.AddComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0.5f, 0f);
        hRect.anchorMax = new Vector2(0.5f, 1f);
        hRect.sizeDelta = new Vector2(2f, 0f);
        hRect.anchoredPosition = Vector2.zero;
        
        Image hImage = hLine.AddComponent<Image>();
        hImage.color = new Color(0f, 1f, 0f, 0.5f); // 반투명 녹색
        
        // 수직선
        GameObject vLine = new GameObject("CenterVLine");
        vLine.transform.SetParent(parent, false);
        RectTransform vRect = vLine.AddComponent<RectTransform>();
        vRect.anchorMin = new Vector2(0f, 0.5f);
        vRect.anchorMax = new Vector2(1f, 0.5f);
        vRect.sizeDelta = new Vector2(0f, 2f);
        vRect.anchoredPosition = Vector2.zero;
        
        Image vImage = vLine.AddComponent<Image>();
        vImage.color = new Color(0f, 1f, 0f, 0.5f); // 반투명 녹색
        
        // 중앙 점 (플레이어)
        GameObject centerDot = new GameObject("PlayerDot");
        centerDot.transform.SetParent(parent, false);
        RectTransform dotRect = centerDot.AddComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(0.5f, 0.5f);
        dotRect.anchorMax = new Vector2(0.5f, 0.5f);
        dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.sizeDelta = new Vector2(6f, 6f);
        dotRect.anchoredPosition = Vector2.zero;
        
        Image dotImage = centerDot.AddComponent<Image>();
        dotImage.color = Color.green; // 녹색 플레이어 점
    }
    
    void CreateRadarGrid(Transform parent)
    {
        // 원형 그리드 라인 생성 (반경의 1/3, 2/3 지점)
        for (int i = 1; i <= 2; i++)
        {
            float radius = radarRadius * (i / 3f);
            
            GameObject circle = new GameObject($"GridCircle{i}");
            circle.transform.SetParent(parent, false);
            RectTransform circleRect = circle.AddComponent<RectTransform>();
            circleRect.anchorMin = new Vector2(0.5f, 0.5f);
            circleRect.anchorMax = new Vector2(0.5f, 0.5f);
            circleRect.pivot = new Vector2(0.5f, 0.5f);
            circleRect.sizeDelta = new Vector2(radius * 2f, radius * 2f);
            circleRect.anchoredPosition = Vector2.zero;
            
            Image circleImage = circle.AddComponent<Image>();
            circleImage.color = new Color(0f, 1f, 0f, 0.25f); // 희미한 녹색 링

            // "사각형"으로 보이지 않도록 링 스프라이트 적용
            int ringSize = Mathf.Clamp(Mathf.RoundToInt(radius * 2f), 32, 1024);
            circleImage.sprite = GetOrCreateRingSprite(ringSize, 1);
            circleImage.type = Image.Type.Simple;
        }
    }

    Sprite GetOrCreateRingSprite(int size, int borderWidth)
    {
        if (cachedRingSpritesBySize.TryGetValue(size, out Sprite cached) && cached != null)
        {
            return cached;
        }

        Texture2D ringTex = CreateCircleBorderTexture(size, borderWidth);
        Sprite ringSprite = Sprite.Create(
            ringTex,
            new Rect(0, 0, ringTex.width, ringTex.height),
            new Vector2(0.5f, 0.5f)
        );
        cachedRingSpritesBySize[size] = ringSprite;
        return ringSprite;
    }

    void ConfigureBlip(GameObject blip, bool isPlayer)
    {
        if (blip == null) return;

        Image img = blip.GetComponent<Image>();
        if (img == null) img = blip.AddComponent<Image>();

        // 블립 원형 스프라이트 강제
        if (cachedBlipCircleSprite == null)
        {
            Texture2D blipTex = CreateCircleTexture(16);
            cachedBlipCircleSprite = Sprite.Create(
                blipTex,
                new Rect(0, 0, blipTex.width, blipTex.height),
                new Vector2(0.5f, 0.5f)
            );
        }
        img.sprite = cachedBlipCircleSprite;
        img.type = Image.Type.Simple;

        // 색상 강제 (레이더 배경이 밝게 보이더라도 확실히 구분)
        img.color = isPlayer ? new Color(0f, 1f, 0f, 1f) : new Color(1f, 0f, 0f, 1f);

        // 크기 강제 (너무 작으면 구분이 어려움)
        RectTransform rt = blip.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = isPlayer ? new Vector2(8f, 8f) : new Vector2(6f, 6f);
        }
    }
    
    void CreateBlipPrefab()
    {
        // 블립 프리팹 생성 (런타임용)
        GameObject blipObj = new GameObject("Blip");
        Image blipImage = blipObj.AddComponent<Image>();
        blipImage.color = Color.red; // 적은 빨간색

        // 블립도 원형으로 보이도록 원형 스프라이트 적용
        if (cachedBlipCircleSprite == null)
        {
            Texture2D blipTex = CreateCircleTexture(16);
            cachedBlipCircleSprite = Sprite.Create(
                blipTex,
                new Rect(0, 0, blipTex.width, blipTex.height),
                new Vector2(0.5f, 0.5f)
            );
        }
        blipImage.sprite = cachedBlipCircleSprite;
        blipImage.type = Image.Type.Simple;
        
        RectTransform rect = blipObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2f, 2f); // 2배 크기
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        
        blipPrefab = blipObj;
        blipPrefab.SetActive(false); // 프리팹은 비활성화
    }

    void EnsureCircularRadarPanel()
    {
        if (radarPanel == null) return;

        // radarPanel이 파괴되었는지 확인
        if (radarPanel.gameObject == null)
        {
            Debug.LogWarning("[RadarSystem] EnsureCircularRadarPanel: radarPanel.gameObject가 null입니다.");
            return;
        }

        GameObject panelObj = radarPanel.gameObject;

        // 패널 크기 기준 (씬에 이미 있는 RadarPanel은 sizeDelta로 들어오는 경우가 많음)
        int size = Mathf.RoundToInt(radarPanel.rect.width);
        if (size <= 0) size = Mathf.RoundToInt(radarPanel.sizeDelta.x);
        if (size <= 0) size = Mathf.RoundToInt(radarRadius * 2f);
        size = Mathf.Clamp(size, 64, 1024);

        // Mask가 동작하려면 패널에 Graphic(Image)이 있어야 함
        Image panelImage = panelObj.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = panelObj.AddComponent<Image>();
        }

        // 원형 스프라이트 생성/적용 (패널 마스크용)
        if (cachedPanelCircleSprite == null || cachedPanelCircleSprite.texture == null || cachedPanelCircleSprite.texture.width != size)
        {
            Texture2D circleTexture = CreateCircleTexture(size);
            cachedPanelCircleSprite = Sprite.Create(
                circleTexture,
                new Rect(0, 0, circleTexture.width, circleTexture.height),
                new Vector2(0.5f, 0.5f)
            );
        }

        // 패널은 "마스크" 역할만: 원형 스프라이트를 마스크로 사용
        panelImage.sprite = cachedPanelCircleSprite;
        panelImage.type = Image.Type.Simple;
        panelImage.color = Color.white;

        // 마스크 적용
        UnityEngine.UI.Mask mask = panelObj.GetComponent<UnityEngine.UI.Mask>();
        if (mask == null)
        {
            mask = panelObj.AddComponent<UnityEngine.UI.Mask>();
        }
        // "사각형으로 보임" 방지: 패널 그래픽은 숨기고(마스크만), 실제 레이더 배경은 자식에서 렌더
        mask.showMaskGraphic = false;

        // C안: 스캔라인/그리드/노이즈 레이더 배경(자식) 생성/보정
        EnsureRadarBackground(panelObj.transform);

        // 테두리(링) 생성/보정
        Transform border = panelObj.transform.Find("Border");
        if (border == null || border.gameObject == null)
        {
            // Border가 없으면 새로 생성
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(panelObj.transform, false);
            border = borderObj.transform;
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = Vector2.zero;
            borderRect.anchoredPosition = Vector2.zero;
            borderObj.AddComponent<Image>();
        }

        // Border 설정 (null이 아니면)
        if (border != null && border.gameObject != null)
        {
            Image borderImage = border.GetComponent<Image>();
            if (borderImage == null) borderImage = border.gameObject.AddComponent<Image>();
            borderImage.color = new Color(0f, 1f, 0f, 0.8f); // 녹색 테두리

            if (cachedPanelBorderSprite == null || cachedPanelBorderSprite.texture == null || cachedPanelBorderSprite.texture.width != size)
            {
                Texture2D borderTexture = CreateCircleBorderTexture(size, 3);
                cachedPanelBorderSprite = Sprite.Create(
                    borderTexture,
                    new Rect(0, 0, borderTexture.width, borderTexture.height),
                    new Vector2(0.5f, 0.5f)
                );
            }

            borderImage.sprite = cachedPanelBorderSprite;
            borderImage.type = Image.Type.Simple;
            border.SetAsLastSibling(); // 항상 위에 보이게
        }
        // Border 생성 실패는 무시 (패널은 정상 작동)

        // 기존(레거시) 자식 오브젝트가 배경을 "하얀 사각형"으로 덮는 문제 방지
        FixLegacyRadarChildren(panelObj.transform);
    }

    void EnsureRadarBackground(Transform panelTransform)
    {
        if (panelTransform == null) return;

        // 파괴된 Transform 접근 방지: 항상 try/catch로 안전하게 재생성
        Transform bg = null;
        try
        {
            bg = panelTransform.Find("RadarBackground");
        }
        catch
        {
            bg = null;
        }

        if (bg == null)
        {
            GameObject bgObj = new GameObject("RadarBackground");
            bgObj.transform.SetParent(panelTransform, false);
            bg = bgObj.transform;

            RectTransform rt = bgObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        // bg가 파괴되었을 수 있으니 GetComponent도 보호
        Image bgImage = null;
        try
        {
            bgImage = bg.GetComponent<Image>();
        }
        catch (MissingReferenceException)
        {
            // 파괴되었으면 다시 생성
            GameObject bgObj = new GameObject("RadarBackground");
            bgObj.transform.SetParent(panelTransform, false);
            bg = bgObj.transform;

            RectTransform rt = bgObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            bgImage = bgObj.AddComponent<Image>();
        }

        if (bgImage == null)
        {
            bgImage = bg.gameObject.AddComponent<Image>();
        }

        // 배경은 셰이더가 그리므로 white로 두고 머티리얼만 적용
        bgImage.color = Color.white;
        ApplyRadarScanMaterial(bgImage);

        // 배경이 항상 가장 아래에 깔리게
        if (bg != null)
        {
            bg.SetAsFirstSibling();
        }
    }

    void ApplyRadarScanMaterial(Image panelImage)
    {
        if (panelImage == null) return;

        Shader s = Shader.Find("UI/RadarScanUI");
        if (s == null)
        {
            Debug.LogWarning("[RadarSystem] UI/RadarScanUI 셰이더를 찾을 수 없습니다. (Assets/Shaders/RadarScanUI.shader 확인)");
            return;
        }

        if (radarScanMaterial == null)
        {
            radarScanMaterial = new Material(s);
        }
        else if (radarScanMaterial.shader != s)
        {
            radarScanMaterial.shader = s;
        }

        // 요청: 레이더 배경 검정 + 투명도 30%
        radarScanMaterial.SetColor("_BackgroundColor", new Color(0f, 0f, 0f, Mathf.Clamp01(backgroundAlpha)));

        // 레이더 느낌(초록 그리드/스캔)
        radarScanMaterial.SetColor("_GridColor", new Color(0f, 1f, 0f, Mathf.Clamp01(gridAlpha)));
        radarScanMaterial.SetFloat("_GridRings", 3f);
        radarScanMaterial.SetFloat("_GridLines", 6f);
        radarScanMaterial.SetColor("_CrosshairColor", new Color(0f, 1f, 0f, Mathf.Clamp01(crosshairAlpha)));
        radarScanMaterial.SetColor("_ScanColor", new Color(0f, 1f, 0f, Mathf.Clamp01(scanAlpha)));
        radarScanMaterial.SetFloat("_ScanSpeed", 1.5f);
        radarScanMaterial.SetFloat("_NoiseStrength", 0.08f);

        panelImage.material = radarScanMaterial;
    }

    void OnDestroy()
    {
        if (radarScanMaterial != null)
        {
            Destroy(radarScanMaterial);
            radarScanMaterial = null;
        }
    }

    void FixLegacyRadarChildren(Transform panelTransform)
    {
        if (panelTransform == null) return;

        // 패널 크기(링 스프라이트 생성용)
        int panelSize = Mathf.RoundToInt(radarPanel != null ? radarPanel.rect.width : radarRadius * 2f);
        if (panelSize <= 0) panelSize = Mathf.RoundToInt(radarRadius * 2f);
        panelSize = Mathf.Clamp(panelSize, 64, 1024);

        for (int i = 0; i < panelTransform.childCount; i++)
        {
            Transform child = panelTransform.GetChild(i);
            if (child == null) continue;

            // 예전 버전에서 만들던 전체 배경 오브젝트가 있으면 꺼버림(흰 사각형 덮임 방지)
            if (child.name == "CircleBackground")
            {
                child.gameObject.SetActive(false);
                continue;
            }

            if (child.name == "Border") continue;
            if (child.name == "RadarBackground") continue;
            if (child.name == "CenterHLine" || child.name == "CenterVLine" || child.name == "PlayerDot") continue;
            if (child.name == "PlayerBlip") continue;
            if (child.name.StartsWith("Blip")) continue; // 적/플레이어 블립(클론) 보호

            Image img = child.GetComponent<Image>();
            RectTransform rt = child.GetComponent<RectTransform>();
            if (img == null || rt == null) continue;

            // GridCircle(1/2) 등이 "스프라이트 없는 Image"로 남아있으면 사각형으로 보이기 쉬움
            if (child.name.StartsWith("GridCircle"))
            {
                int ringSize = Mathf.Clamp(Mathf.RoundToInt(rt.rect.width), 32, panelSize);
                img.sprite = GetOrCreateRingSprite(ringSize, 1);
                img.type = Image.Type.Simple;
                img.color = new Color(0f, 1f, 0f, 0.25f);
                continue;
            }

            // 핵심: sprite가 없는 Image는 기본 흰 사각형으로 렌더링되므로(색/알파와 무관하게 "흰 느낌" 유발)
            // 블립/테두리/십자선 외에는 전부 비활성화
            if (img.sprite == null)
            {
                img.enabled = false;
                continue;
            }
        }
    }
    
    void UpdateRadar()
    {
        if (playerTransform == null || radarPanel == null) return;
        
        // EnemySpawner에서 적 리스트 가져오기 (더 효율적)
        List<GameObject> enemyList = new List<GameObject>();
        
        if (enemySpawner != null)
        {
            enemyList = enemySpawner.GetActiveEnemies();

            // 스폰러가 비어있으면(리스트 관리 안 하는 경우) 아래 폴백 로직으로 찾기
            if (enemyList == null || enemyList.Count == 0)
            {
                enemySpawner = null;
            }
        }
        if (enemySpawner == null)
        {
            // EnemySpawner가 없으면 직접 찾기
            GameObject[] foundEnemies = null;
            
            // "Enemy" 태그가 정의되어 있는지 확인 후 사용
            try
            {
                foundEnemies = GameObject.FindGameObjectsWithTag("Enemy");
            }
            catch (UnityException)
            {
                // 태그가 정의되지 않았으면 빈 배열로 처리
                foundEnemies = new GameObject[0];
            }
            
            if (foundEnemies == null || foundEnemies.Length == 0)
            {
                // 태그로 찾지 못했으면 이름으로 찾기
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foundEnemies = System.Array.FindAll(allObjects,
                    obj =>
                        obj != null &&
                        obj.activeInHierarchy &&
                        !string.IsNullOrEmpty(obj.name) &&
                        obj.name.ToLowerInvariant().StartsWith("enemy"));
            }
            
            if (foundEnemies != null)
            {
                enemyList.AddRange(foundEnemies);
            }
        }
        
        GameObject[] enemies = enemyList.ToArray();
        
        // 기존 블립 제거 (없어진 적)
        List<GameObject> enemiesToRemove = new List<GameObject>();
        foreach (var kvp in enemyToBlipMap)
        {
            if (kvp.Key == null || !kvp.Key.activeInHierarchy)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
                enemiesToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var enemy in enemiesToRemove)
        {
            enemyToBlipMap.Remove(enemy);
        }
        
        // 각 적에 대해 블립 업데이트
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            // 거리 계산
            float distance = Vector3.Distance(playerTransform.position, enemy.transform.position);
            
            // 감지 범위 내에 있으면 표시
            // (groundCenter를 못 찾는 경우도 있으니, 기본은 거리 기준으로 표시)
            if (distance <= detectionRange)
            {
                // 블립이 없으면 생성
                if (!enemyToBlipMap.ContainsKey(enemy))
                {
                    GameObject blip = Instantiate(blipPrefab, radarPanel);
                    blip.SetActive(true);
                    enemyToBlipMap[enemy] = blip;
                }
                
                // 블립 위치 업데이트
                UpdateBlipPosition(enemy, enemyToBlipMap[enemy], distance);

                // 적 블립 스타일 강제 (프리팹이 흰색이어도 무조건 빨간색)
                ConfigureBlip(enemyToBlipMap[enemy], isPlayer: false);
            }
            else
            {
                // 범위 밖이거나 반대편이면 블립 제거
                if (enemyToBlipMap.ContainsKey(enemy))
                {
                    if (enemyToBlipMap[enemy] != null)
                    {
                        Destroy(enemyToBlipMap[enemy]);
                    }
                    enemyToBlipMap.Remove(enemy);
                }
            }
        }
    }
    
    void UpdateBlipPosition(GameObject enemy, GameObject blip, float distance)
    {
        if (blip == null || enemy == null || playerTransform == null) return;
        
        // 기준 up 계산: groundCenter가 있으면 구면 기준, 없으면 플레이어 up 기준
        Vector3 playerUp;
        if (groundCenter != null)
        {
            Vector3 playerToGround = playerTransform.position - groundCenter.position;
            if (playerToGround.magnitude < 0.1f) return;
            playerUp = playerToGround.normalized;
        }
        else
        {
            playerUp = playerTransform.up.normalized;
            if (playerUp.magnitude < 0.1f) playerUp = Vector3.up;
        }

        Vector3 playerForward = Vector3.ProjectOnPlane(playerTransform.forward, playerUp).normalized;
        Vector3 playerRight = Vector3.ProjectOnPlane(playerTransform.right, playerUp).normalized;
        
        // forward/right가 너무 작으면 대체 방향 사용
        if (playerForward.magnitude < 0.1f)
        {
            playerForward = Vector3.ProjectOnPlane(Vector3.forward, playerUp).normalized;
            if (playerForward.magnitude < 0.1f)
            {
                playerForward = Vector3.ProjectOnPlane(Vector3.right, playerUp).normalized;
            }
        }
        if (playerRight.magnitude < 0.1f)
        {
            playerRight = Vector3.Cross(playerUp, playerForward).normalized;
        }
        
        // 적의 상대 위치를 Ground 표면에 투영
        Vector3 enemyRelative = enemy.transform.position - playerTransform.position;
        Vector3 enemyForward = Vector3.ProjectOnPlane(enemyRelative, playerUp).normalized;
        
        // 레이더 좌표 계산 (Ground 표면 기준)
        float forwardDot = Vector3.Dot(enemyForward, playerForward);
        float rightDot = Vector3.Dot(enemyForward, playerRight);
        
        // 레이더 위치 (정규화)
        float normalizedDistance = Mathf.Clamp01(distance / detectionRange);
        Vector2 radarPos = new Vector2(rightDot, forwardDot) * radarRadius * normalizedDistance;
        
        // 블립 위치 설정
        RectTransform blipRect = blip.GetComponent<RectTransform>();
        if (blipRect != null)
        {
            blipRect.anchoredPosition = radarPos;
        }
    }
}
