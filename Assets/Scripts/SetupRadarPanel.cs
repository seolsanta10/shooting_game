using UnityEngine;
using UnityEngine.UI;

public class SetupRadarPanel : MonoBehaviour
{
    void Start()
    {
        // RadarPanel 찾기
        GameObject radarPanelObj = GameObject.Find("RadarPanel");
        if (radarPanelObj != null)
        {
            RectTransform rectTransform = radarPanelObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 오른쪽 상단에 배치
                rectTransform.anchorMin = new Vector2(1f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(1f, 1f);
                rectTransform.anchoredPosition = new Vector2(-20f, -20f);
                rectTransform.sizeDelta = new Vector2(200f, 200f);
                
                // 배경색 설정
                Image bgImage = radarPanelObj.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.color = new Color(0f, 0f, 0f, 0.8f); // 반투명 검은색
                }
            }
        }
    }
}
