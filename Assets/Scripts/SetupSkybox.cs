using UnityEngine;

public class SetupSkybox : MonoBehaviour
{
    void Start()
    {
        // RenderSettings의 Skybox를 설정하여 하늘만 보이도록
        // 기본 Skybox Material이 없으면 생성
        if (RenderSettings.skybox == null)
        {
            // 간단한 하늘색 Skybox Material 생성
            Material skyboxMaterial = new Material(Shader.Find("Skybox/6 Sided"));
            if (skyboxMaterial != null)
            {
                // 하늘색으로 설정
                skyboxMaterial.SetColor("_Tint", new Color(0.5f, 0.7f, 1f));
                skyboxMaterial.SetFloat("_Exposure", 1.0f);
                RenderSettings.skybox = skyboxMaterial;
            }
        }
        
        // 모든 카메라의 Clear Flags를 Skybox로 설정
        Camera[] cameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in cameras)
        {
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.5f, 0.7f, 1f, 0f); // 하늘색
        }
        
        Debug.Log("[SetupSkybox] 하늘 배경 설정 완료");
    }
}
