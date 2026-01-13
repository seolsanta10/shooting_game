using UnityEngine;

/// <summary>
/// 파랑 아이템: 5초 무적 보호막(시각 + 적 총알 제거)
/// </summary>
public class PlayerShield : MonoBehaviour
{
    private GameObject shieldObj;

    public void SetActive(bool active)
    {
        Ensure();
        if (shieldObj != null) shieldObj.SetActive(active);
    }

    void Ensure()
    {
        if (shieldObj != null) return;

        shieldObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldObj.name = "Shield";
        shieldObj.transform.SetParent(transform, false);
        shieldObj.transform.localPosition = Vector3.zero;
        shieldObj.transform.localScale = Vector3.one * 2.2f; // 플레이어보다 약간 크게

        // 트리거 콜라이더
        SphereCollider col = shieldObj.GetComponent<SphereCollider>();
        col.isTrigger = true;

        // 렌더: 반투명 파랑
        Renderer r = shieldObj.GetComponent<Renderer>();
        if (r != null)
        {
            Material m = new Material(Shader.Find("Standard"));
            m.color = new Color(0.2f, 0.6f, 1f, 0.25f);

            // Transparent 세팅(간단)
            m.SetFloat("_Mode", 3);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;

            r.material = m;
        }

        // 충돌 처리 컴포넌트
        shieldObj.AddComponent<ShieldTrigger>().Init(this);
    }

    private sealed class ShieldTrigger : MonoBehaviour
    {
        private PlayerShield owner;
        public void Init(PlayerShield o) => owner = o;

        void OnTriggerEnter(Collider other)
        {
            if (other == null) return;

            // 적 총알 제거
            EnemyBullet eb = other.GetComponent<EnemyBullet>();
            if (eb != null)
            {
                Destroy(other.gameObject);
            }
        }
    }
}

