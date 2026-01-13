using UnityEngine;

/// <summary>
/// 아이템 슬롯(=스킬 슬롯) 3칸 인벤토리
/// - 아이템 줍기 시 빈 칸에 들어감
/// - UI(SkillBarUI)와 연동
/// </summary>
public class ItemInventory : MonoBehaviour
{
    public static ItemInventory Instance { get; private set; }

    private ItemPickup.ItemInfo[] slots = new ItemPickup.ItemInfo[3];
    private bool initializedTestLoadout = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("ItemInventory");
        go.AddComponent<ItemInventory>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 테스트용: 시작 시 슬롯 1/2/3에 각각 스킬(아이템) 채워넣기
        EnsureTestLoadout();
    }

    public bool TryAdd(ItemPickup.ItemInfo item, out int placedIndex)
    {
        placedIndex = -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (string.IsNullOrEmpty(slots[i].id))
            {
                slots[i] = item;
                placedIndex = i;

                // UI 갱신 (슬롯은 1/2/3 표시지만 인덱스는 0/1/2)
                if (SkillBarUI.Instance != null)
                {
                    SkillBarUI.Instance.SetSlotItem(i + 1, item);
                }
                return true;
            }
        }
        return false;
    }

    public bool TryConsumeSlot(int slotNumber1To3, out ItemPickup.ItemInfo item)
    {
        item = default;
        int idx = slotNumber1To3 - 1;
        if (idx < 0 || idx >= slots.Length) return false;
        if (string.IsNullOrEmpty(slots[idx].id)) return false;

        item = slots[idx];
        slots[idx] = default;

        if (SkillBarUI.Instance != null)
        {
            SkillBarUI.Instance.ClearSlot(slotNumber1To3);
        }
        return true;
    }

    private void EnsureTestLoadout()
    {
        if (initializedTestLoadout) return;
        initializedTestLoadout = true;

        // 이미 뭔가 들어있으면 덮어쓰지 않음
        if (!string.IsNullOrEmpty(slots[0].id) || !string.IsNullOrEmpty(slots[1].id) || !string.IsNullOrEmpty(slots[2].id))
        {
            Invoke(nameof(RefreshUI), 0.1f);
            return;
        }

        slots[0] = new ItemPickup.ItemInfo { id = "ITEM_GREEN", iconColor = new Color(0.2f, 1f, 0.4f, 1f) };
        slots[1] = new ItemPickup.ItemInfo { id = "ITEM_YELLOW", iconColor = new Color(1f, 0.85f, 0.1f, 1f) };
        slots[2] = new ItemPickup.ItemInfo { id = "ITEM_BLUE", iconColor = new Color(0.2f, 0.6f, 1f, 1f) };

        // UI는 SkillBarUI가 늦게 생성될 수 있으니 살짝 딜레이 후 갱신
        Invoke(nameof(RefreshUI), 0.1f);
    }

    private void RefreshUI()
    {
        if (SkillBarUI.Instance == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (!string.IsNullOrEmpty(slots[i].id))
            {
                SkillBarUI.Instance.SetSlotItem(i + 1, slots[i]);
            }
            else
            {
                SkillBarUI.Instance.ClearSlot(i + 1);
            }
        }
    }
}

