using UnityEngine;

public class AutoCreatePlanetAndPlayer : MonoBehaviour
{
    void Awake()
    {
        // 지구 생성
        GameObject planet = GameObject.Find("지구");
        if (planet == null)
        {
            planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planet.name = "지구";
            planet.transform.position = Vector3.zero;
            planet.transform.localScale = new Vector3(10, 10, 10);
            
            // 지구에 색상 추가
            Renderer renderer = planet.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.2f, 0.4f, 0.8f); // 파란색
                renderer.material = mat;
            }
        }
        
        // 플레이어 생성
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Cube);
            player.name = "Player";
            player.transform.position = new Vector3(0, 12, 0);
            player.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            
            // 플레이어에 색상 추가
            Renderer playerRenderer = player.GetComponent<Renderer>();
            if (playerRenderer != null)
            {
                Material playerMat = new Material(Shader.Find("Standard"));
                playerMat.color = Color.red; // 빨간색
                playerRenderer.material = playerMat;
            }
            
            // PlanetFlightController 컴포넌트 추가
            PlanetFlightController controller = player.GetComponent<PlanetFlightController>();
            if (controller == null)
            {
                controller = player.AddComponent<PlanetFlightController>();
                if (planet != null)
                {
                    controller.planetCenter = planet.transform;
                }
            }
        }
    }
}
