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
    
    [Header("참조")]
    public Transform playerTransform;
    public Transform groundCenter;
    
    private List<GameObject> enemyBlips = new List<GameObject>();
    private Dictionary<GameObject, GameObject> enemyToBlipMap = new Dictionary<GameObject, GameObject>();
    private EnemySpawner enemySpawner;
    private GameObject playerBlip; // 플레이어 블립
    
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
        enemySpawner = FindObjectOfType<EnemySpawner>();
        
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
            
            // 플레이어는 녹색으로 표시
            Image playerBlipImage = playerBlip.GetComponent<Image>();
            if (playerBlipImage != null)
            {
                playerBlipImage.color = Color.green; // 플레이어는 녹색
            }
            
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
        if (playerTransform == null || groundCenter == null) return;
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
        Canvas canvas = FindObjectOfType<Canvas>();
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
        
        // 원형 마스크 추가 (먼저 추가해야 자식들이 마스킹됨)
        UnityEngine.UI.Mask mask = panelObj.AddComponent<UnityEngine.UI.Mask>();
        mask.showMaskGraphic = false; // 마스크 그래픽은 숨김
        
        // 원형 배경 이미지 생성
        GameObject circleBg = new GameObject("CircleBackground");
        circleBg.transform.SetParent(panelObj.transform, false);
        RectTransform circleRect = circleBg.AddComponent<RectTransform>();
        circleRect.anchorMin = Vector2.zero;
        circleRect.anchorMax = Vector2.one;
        circleRect.sizeDelta = Vector2.zero;
        circleRect.anchoredPosition = Vector2.zero;
        
        Image circleImage = circleBg.AddComponent<Image>();
        circleImage.color = new Color(0.1f, 0.1f, 0.3f, 0.9f); // 어두운 파란색 배경
        
        // 원형 스프라이트 생성 (런타임)
        Texture2D circleTexture = CreateCircleTexture((int)radarRadius * 2);
        Sprite circleSprite = Sprite.Create(circleTexture, new Rect(0, 0, circleTexture.width, circleTexture.height), new Vector2(0.5f, 0.5f));
        circleImage.sprite = circleSprite;
        circleImage.type = Image.Type.Simple;
        
        // 원형 테두리 생성
        GameObject border = new GameObject("Border");
        border.transform.SetParent(panelObj.transform, false);
        RectTransform borderRect = border.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        borderRect.anchoredPosition = Vector2.zero;
        
        Image borderImage = border.AddComponent<Image>();
        borderImage.color = new Color(0f, 1f, 0f, 0.8f); // 녹색 테두리
        
        // 테두리용 원형 스프라이트 생성 (중앙이 투명한 링 형태)
        Texture2D borderTexture = CreateCircleBorderTexture((int)radarRadius * 2, 3);
        Sprite borderSprite = Sprite.Create(borderTexture, new Rect(0, 0, borderTexture.width, borderTexture.height), new Vector2(0.5f, 0.5f));
        borderImage.sprite = borderSprite;
        borderImage.type = Image.Type.Simple;
        
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
            circleImage.color = new Color(0f, 0.5f, 0f, 0.3f); // 반투명 녹색 원
        }
    }
    
    void CreateBlipPrefab()
    {
        // 블립 프리팹 생성 (런타임용)
        GameObject blipObj = new GameObject("Blip");
        Image blipImage = blipObj.AddComponent<Image>();
        blipImage.color = Color.red; // 적은 빨간색
        
        RectTransform rect = blipObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(2f, 2f); // 2배 크기
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        
        blipPrefab = blipObj;
        blipPrefab.SetActive(false); // 프리팹은 비활성화
    }
    
    void UpdateRadar()
    {
        if (playerTransform == null || groundCenter == null || radarPanel == null) return;
        
        // EnemySpawner에서 적 리스트 가져오기 (더 효율적)
        List<GameObject> enemyList = new List<GameObject>();
        
        if (enemySpawner != null)
        {
            enemyList = enemySpawner.GetActiveEnemies();
        }
        else
        {
            // EnemySpawner가 없으면 직접 찾기
            GameObject[] foundEnemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (foundEnemies.Length == 0)
            {
                foundEnemies = System.Array.FindAll(FindObjectsOfType<GameObject>(), 
                    obj => obj.name.StartsWith("enemy") && obj.activeInHierarchy);
            }
            enemyList.AddRange(foundEnemies);
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
            
            // Ground 중심을 기준으로 플레이어와 적이 같은 반구에 있는지 확인
            Vector3 playerToGround = playerTransform.position - groundCenter.position;
            Vector3 enemyToGround = enemy.transform.position - groundCenter.position;
            
            // 거리가 너무 작으면 스킵 (Ground 중심에 너무 가까운 경우)
            if (playerToGround.magnitude < 0.1f || enemyToGround.magnitude < 0.1f) continue;
            
            Vector3 playerDirection = playerToGround.normalized;
            Vector3 enemyDirection = enemyToGround.normalized;
            float dotProduct = Vector3.Dot(playerDirection, enemyDirection);
            
            // 내적이 양수면 같은 반구, 0이면 수직, 음수면 반대편 반구
            // 같은 반구에 있는지 확인 (엄격하게 양수만 허용, 작은 여유값 추가)
            bool isSameHemisphere = dotProduct > 0.01f; // 양수일 때만 같은 반구로 간주 (작은 여유값으로 부동소수점 오차 방지)
            
            // 거리 계산
            float distance = Vector3.Distance(playerTransform.position, enemy.transform.position);
            
            // 같은 반구에 있고 감지 범위 내에 있으면 표시
            if (isSameHemisphere && distance <= detectionRange)
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
        if (blip == null || enemy == null || playerTransform == null || groundCenter == null) return;
        
        // 다시 한번 반구 체크 (안전장치)
        Vector3 playerToGround = playerTransform.position - groundCenter.position;
        Vector3 enemyToGround = enemy.transform.position - groundCenter.position;
        
        if (playerToGround.magnitude < 0.1f || enemyToGround.magnitude < 0.1f) return;
        
        Vector3 playerDirection = playerToGround.normalized;
        Vector3 enemyDirection = enemyToGround.normalized;
        float dotProduct = Vector3.Dot(playerDirection, enemyDirection);
        
        // 반대편이면 블립 제거
        if (dotProduct <= 0.01f)
        {
            if (enemyToBlipMap.ContainsKey(enemy))
            {
                if (enemyToBlipMap[enemy] != null)
                {
                    Destroy(enemyToBlipMap[enemy]);
                }
                enemyToBlipMap.Remove(enemy);
            }
            return;
        }
        
        // Ground 중심을 기준으로 한 방향 계산
        Vector3 playerUp = playerDirection; // Ground 중심에서 플레이어로의 방향이 "위"
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
