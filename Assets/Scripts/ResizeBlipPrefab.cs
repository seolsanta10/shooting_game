using UnityEngine;
using UnityEngine.UI;

public class ResizeBlipPrefab : MonoBehaviour
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
                // 크기를 2배로 늘림 (1 -> 2)
                rectTransform.sizeDelta = new Vector2(2f, 2f);
                Debug.Log("[ResizeBlipPrefab] BlipPrefab 크기를 2x2로 변경");
            }
        }
    }
}
