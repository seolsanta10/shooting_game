using UnityEngine;

/// <summary>
/// 월드에 떨어진 아이템
/// - Player와 Trigger 충돌하면 ItemInventory(3칸)에 들어감
/// </summary>
public class ItemPickup : MonoBehaviour
{
    [System.Serializable]
    public struct ItemInfo
    {
        public string id;
        public Color iconColor;
    }

    [Header("아이템 정보")]
    public ItemInfo item;

    [Header("회전/연출")]
    public float spinSpeed = 80f;

    void Reset()
    {
        // 기본값
        item.id = "ITEM";
        item.iconColor = new Color(1f, 0.8f, 0.1f, 1f);
    }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        bool isPlayer = other.gameObject.name == "Player" || other.CompareTag("Player");
        if (!isPlayer) return;

        if (ItemInventory.Instance == null) return;

        if (ItemInventory.Instance.TryAdd(item, out int slotIndex))
        {
            Destroy(gameObject);
        }
        else
        {
            // 인벤토리 꽉 차면 그대로 둠
        }
    }

    public static GameObject SpawnBasicItem(Vector3 position, Transform planetCenter)
    {
        // 기본 아이템(노랑)
        return SpawnItem(position, planetCenter, new ItemInfo
        {
            id = "ITEM_YELLOW",
            iconColor = new Color(1f, 0.85f, 0.1f, 1f)
        });
    }

    public static GameObject SpawnRandomItem(Vector3 position, Transform planetCenter)
    {
        // 파랑/노랑/초록 랜덤
        int r = Random.Range(0, 3);
        ItemInfo info;
        if (r == 0)
        {
            info = new ItemInfo { id = "ITEM_BLUE", iconColor = new Color(0.2f, 0.6f, 1f, 1f) };
        }
        else if (r == 1)
        {
            info = new ItemInfo { id = "ITEM_YELLOW", iconColor = new Color(1f, 0.85f, 0.1f, 1f) };
        }
        else
        {
            info = new ItemInfo { id = "ITEM_GREEN", iconColor = new Color(0.2f, 1f, 0.4f, 1f) };
        }

        return SpawnItem(position, planetCenter, info);
    }

    private static GameObject SpawnItem(Vector3 position, Transform planetCenter, ItemInfo info)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "ItemDrop";

        // 콜라이더 트리거로
        SphereCollider col = go.GetComponent<SphereCollider>();
        col.isTrigger = true;

        // Trigger 이벤트를 위해 Rigidbody 하나 필요(키네마틱)
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // 위치/정렬: 적과 "동일 고도" 유지
        // planetCenter가 있으면: 적 위치의 반지름 그대로 유지해서 같은 고도에 배치
        Vector3 pos = position;
        if (planetCenter != null)
        {
            Vector3 up = (position - planetCenter.position).normalized;
            float radius = Vector3.Distance(position, planetCenter.position);
            pos = planetCenter.position + up * radius;
        }
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.6f;

        // 시각: 아이템 색상
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = info.iconColor;
        }

        ItemPickup pickup = go.AddComponent<ItemPickup>();
        pickup.item = info;

        return go;
    }
}

