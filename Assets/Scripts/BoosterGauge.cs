using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class BoosterGauge : MonoBehaviour
{
    [Header("UI 설정")]
    public Image gaugeFill; // 게이지 채우기 이미지
    public Image gaugeBackground; // 게이지 배경 이미지
    public Text gaugeText; // 게이지 텍스트 (선택사항)
    
    [Header("부스터 설정")]
    public float maxBooster = 100f; // 최대 부스터 값
    public float boosterConsumptionRate = 20f; // 초당 소모량
    public float boosterRecoveryRate = 10f; // 초당 회복량
    
    private float currentBooster;
    private FlightSimulationController playerController;
    
    void Start()
    {
        currentBooster = maxBooster;
        
        // 플레이어 컨트롤러 찾기
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            playerController = player.GetComponent<FlightSimulationController>();
        }
        
        // UI가 없으면 자동 생성
        if (gaugeFill == null || gaugeBackground == null)
        {
            CreateBoosterGaugeUI();
        }
    }
    
    void Update()
    {
        // 부스터(대쉬) 사용 중인지 확인
        bool isBoosting = false;
        
        // FlightSimulationController에서 확인 (B 키로 부스터 활성화)
        if (playerController != null)
        {
            // IsDashing 프로퍼티로 확인
            isBoosting = playerController.IsDashing;
        }
        
        // 부스터 게이지 업데이트
        if (isBoosting && currentBooster > 0f)
        {
            // 부스터 사용 중: 게이지 감소
            currentBooster -= boosterConsumptionRate * Time.deltaTime;
            currentBooster = Mathf.Max(0f, currentBooster);
        }
        else
        {
            // 부스터 미사용: 게이지 회복
            currentBooster += boosterRecoveryRate * Time.deltaTime;
            currentBooster = Mathf.Min(maxBooster, currentBooster);
        }
        
        // UI 업데이트
        UpdateGaugeUI();
    }
    
    void UpdateGaugeUI()
    {
        if (gaugeFill != null)
        {
            // 게이지 채우기 (0~1 범위)
            float fillAmount = currentBooster / maxBooster;
            gaugeFill.fillAmount = fillAmount;
            
            // 색상 변경 (낮을수록 빨간색)
            if (fillAmount < 0.3f)
            {
                gaugeFill.color = Color.red;
            }
            else if (fillAmount < 0.6f)
            {
                gaugeFill.color = Color.yellow;
            }
            else
            {
                gaugeFill.color = Color.green;
            }
        }
        
        // 텍스트 업데이트
        if (gaugeText != null)
        {
            gaugeText.text = $"BOOSTER: {(int)currentBooster}%";
        }
    }
    
    void CreateBoosterGaugeUI()
    {
        // Canvas 찾기 또는 생성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // 부스터 게이지 패널 생성
        GameObject panelObj = new GameObject("BoosterGaugePanel");
        panelObj.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-20f, 20f); // 오른쪽 아래
        panelRect.sizeDelta = new Vector2(200f, 30f);
        
        // 배경 이미지
        gaugeBackground = panelObj.AddComponent<Image>();
        gaugeBackground.color = new Color(0f, 0f, 0f, 0.7f); // 반투명 검은색 배경
        
        // 게이지 채우기 이미지 (자식으로 생성)
        GameObject fillObj = new GameObject("GaugeFill");
        fillObj.transform.SetParent(panelObj.transform, false);
        
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        gaugeFill = fillObj.AddComponent<Image>();
        gaugeFill.color = Color.green;
        gaugeFill.type = Image.Type.Filled;
        gaugeFill.fillMethod = Image.FillMethod.Horizontal;
        
        // 텍스트 (선택사항)
        GameObject textObj = new GameObject("GaugeText");
        textObj.transform.SetParent(panelObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        gaugeText = textObj.AddComponent<Text>();
        gaugeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        gaugeText.fontSize = 14;
        gaugeText.color = Color.white;
        gaugeText.alignment = TextAnchor.MiddleCenter;
        gaugeText.text = "BOOSTER: 100%";
    }
    
    public float GetCurrentBooster()
    {
        return currentBooster;
    }
    
    public bool CanBoost()
    {
        return currentBooster > 0f;
    }
}
