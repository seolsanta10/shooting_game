using UnityEngine;
using UnityEngine.UI;

public class SetupBlipPrefab : MonoBehaviour
{
    void Start()
    {
        // BlipPrefab 찾기
        GameObject blipObj = GameObject.Find("BlipPrefab");
        if (blipObj != null)
        {
            RectTransform rectTransform = blipObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 중앙 정렬
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(2f, 2f); // 2배 크기
                rectTransform.anchoredPosition = Vector2.zero;
                
                // 빨간색으로 설정 (적 표시)
                Image blipImage = blipObj.GetComponent<Image>();
                if (blipImage != null)
                {
                    blipImage.color = Color.red;
                }
            }
        }
    }
}
