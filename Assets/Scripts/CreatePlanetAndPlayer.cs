using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public class CreatePlanetAndPlayer
{
    static CreatePlanetAndPlayer()
    {
        // 에디터가 시작될 때 자동으로 실행되지 않도록 함
    }
    
    [MenuItem("GameObject/비행 게임/지구와 플레이어 생성")]
    static void CreatePlanetAndPlayerObjects()
    {
        // 지구 생성
        GameObject planet = GameObject.Find("지구");
        if (planet == null)
        {
            planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planet.name = "지구";
            planet.transform.position = Vector3.zero;
            planet.transform.localScale = new Vector3(10, 10, 10);
            
            // 지구에 색상 추가 (선택사항)
            Renderer renderer = planet.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.2f, 0.4f, 0.8f); // 파란색
                renderer.material = mat;
            }
            
            Undo.RegisterCreatedObjectUndo(planet, "Create 지구");
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
            
            Undo.RegisterCreatedObjectUndo(player, "Create Player");
        }
        
        // 선택을 플레이어로 변경
        Selection.activeGameObject = player;
        
        Debug.Log("지구와 플레이어가 생성되었습니다!");
    }
}
#endif
