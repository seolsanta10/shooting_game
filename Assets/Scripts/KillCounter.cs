using UnityEngine;
using UnityEngine.UI;

public class KillCounter : MonoBehaviour
{
    [Header("UI 설정")]
    public Text killCountText; // 처치 카운트 텍스트
    
    private int killCount = 0;
    private Canvas canvas;
    
    void Start()
    {
        // Canvas 찾기 또는 생성
        canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // 텍스트가 없으면 자동 생성
        if (killCountText == null)
        {
            CreateKillCounterText();
        }
        
        UpdateKillCountText();
    }
    
    void CreateKillCounterText()
    {
        // 텍스트 오브젝트 생성
        GameObject textObj = new GameObject("KillCounterText");
        textObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(20f, -20f); // 왼쪽 상단
        rectTransform.sizeDelta = new Vector2(200f, 50f);
        
        killCountText = textObj.AddComponent<Text>();
        // Unity 최신 버전: Arial.ttf 내장 폰트 제거됨 → LegacyRuntime.ttf 사용
        killCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        killCountText.fontSize = 24;
        killCountText.color = Color.white;
        killCountText.alignment = TextAnchor.UpperLeft;
        killCountText.text = "Kills: 0";
    }
    
    public void AddKill()
    {
        killCount++;
        UpdateKillCountText();
    }
    
    void UpdateKillCountText()
    {
        if (killCountText != null)
        {
            killCountText.text = $"Kills: {killCount}";
        }
    }
    
    public int GetKillCount()
    {
        return killCount;
    }
}
