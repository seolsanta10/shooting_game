using UnityEngine;

public class AutoCreatePlanetAndPlayer : MonoBehaviour
{
    void Awake()
    {
        GamePrefabSettings settings = GamePrefabSettings.LoadOrNull();

        // Ground(행성/지면) 생성: 프리팹이 있으면 그걸, 없으면 기본 Sphere
        GameObject planet = GameObject.Find("Ground");
        if (planet == null)
            planet = GameObject.Find("지구");

        if (planet == null)
        {
            if (settings != null && settings.groundPrefab != null)
            {
                planet = Instantiate(settings.groundPrefab, Vector3.zero, Quaternion.identity);
                planet.name = "Ground";
            }
            else
            {
                planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                planet.name = "Ground";
                planet.transform.position = Vector3.zero;
                planet.transform.localScale = new Vector3(50, 50, 50); // 반지름 25 기준

                // 시각: 기본 색상
                Renderer renderer = planet.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(0.2f, 0.4f, 0.8f);
                    renderer.material = mat;
                }
            }
        }
        
        // 플레이어 생성
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            if (settings != null && settings.playerPrefab != null)
            {
                // 프리팹 플레이어
                Vector3 spawnPos = new Vector3(0, 0, 0);
                if (planet != null)
                    spawnPos = planet.transform.position + Vector3.up * (planet.transform.localScale.x * 0.5f + 5f);

                player = Instantiate(settings.playerPrefab, spawnPos, Quaternion.identity);
                player.name = "Player";
            }
            else
            {
                // 기본 플레이어(큐브)
                player = GameObject.CreatePrimitive(PrimitiveType.Cube);
                player.name = "Player";

                float radius = 25f;
                if (planet != null) radius = planet.transform.localScale.x * 0.5f;
                player.transform.position = (planet != null ? planet.transform.position : Vector3.zero) + Vector3.up * (radius + 5f);
                player.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

                Renderer playerRenderer = player.GetComponent<Renderer>();
                if (playerRenderer != null)
                {
                    Material playerMat = new Material(Shader.Find("Standard"));
                    playerMat.color = Color.red;
                    playerRenderer.material = playerMat;
                }

                // 현재 프로젝트 메인 컨트롤러(FlightSimulationController)로 연결
                FlightSimulationController controller = player.GetComponent<FlightSimulationController>();
                if (controller == null) controller = player.AddComponent<FlightSimulationController>();
                if (controller != null && planet != null)
                    controller.planetCenter = planet.transform;

                // 미사일 런처도 기본으로 붙임(프리팹 없을 때도 플레이 가능)
                if (player.GetComponent<MissileLauncher>() == null)
                    player.AddComponent<MissileLauncher>();

                // 플레이어 체력(에너지)도 기본으로 붙임
                if (player.GetComponent<PlayerHealth>() == null)
                    player.AddComponent<PlayerHealth>();
            }

            // Tag는 있으면 설정(없으면 예외 날 수 있어 보호)
            try { player.tag = "Player"; } catch { /* ignore */ }
        }
    }
}
