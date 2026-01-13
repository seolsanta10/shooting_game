using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 1/2/3 키로 슬롯 아이템(=스킬) 사용
/// - 초록: 5초간 속도 2배
/// - 노랑: 5초간 거대화 3배 (플레이어 + 미사일)
/// - 파랑: 5초간 무적 보호막
/// </summary>
public class ItemSkillSystem : MonoBehaviour
{
    public float greenDuration = 5f;
    public float yellowDuration = 5f;
    public float blueDuration = 5f;

    private GameObject player;
    private FlightSimulationController flight;
    private MissileLauncher missileLauncher;
    private PlayerShield shield;
    private CameraFollow cameraFollow;

    private Vector3 originalPlayerScale = Vector3.one;
    private float originalBaseSpeed;
    private float originalMoveSpeed;
    private float originalDashSpeed;
    private float originalMissileScaleMult = 1f;
    private float originalCamDistance;
    private float originalCamHeight;
    private bool cachedCameraOriginal = false;

    private Coroutine greenCo;
    private Coroutine yellowCo;
    private Coroutine blueCo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        GameObject go = new GameObject("ItemSkillSystem");
        go.AddComponent<ItemSkillSystem>();
        DontDestroyOnLoad(go);
    }

    void Update()
    {
        EnsureRefs();

        if (ItemInventory.Instance == null) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (WasSlotPressed(kb, 1)) TryUseSlot(1);
        if (WasSlotPressed(kb, 2)) TryUseSlot(2);
        if (WasSlotPressed(kb, 3)) TryUseSlot(3);
    }

    private static bool WasSlotPressed(Keyboard kb, int slot)
    {
        if (slot == 1) return (kb.digit1Key?.wasPressedThisFrame ?? false) || (kb.numpad1Key?.wasPressedThisFrame ?? false);
        if (slot == 2) return (kb.digit2Key?.wasPressedThisFrame ?? false) || (kb.numpad2Key?.wasPressedThisFrame ?? false);
        if (slot == 3) return (kb.digit3Key?.wasPressedThisFrame ?? false) || (kb.numpad3Key?.wasPressedThisFrame ?? false);
        return false;
    }

    private void TryUseSlot(int slot)
    {
        if (!ItemInventory.Instance.TryConsumeSlot(slot, out ItemPickup.ItemInfo item))
            return; // 빈 슬롯이면 무시

        // 아이템 ID로 효과 분기
        if (item.id == "ITEM_GREEN")
        {
            if (greenCo != null) StopCoroutine(greenCo);
            greenCo = StartCoroutine(ApplyGreenSpeedBoost());
        }
        else if (item.id == "ITEM_YELLOW")
        {
            if (yellowCo != null) StopCoroutine(yellowCo);
            yellowCo = StartCoroutine(ApplyYellowGiant());
        }
        else if (item.id == "ITEM_BLUE")
        {
            if (blueCo != null) StopCoroutine(blueCo);
            blueCo = StartCoroutine(ApplyBlueShield());
        }
        else
        {
            // 알 수 없는 아이템이면 소모만 하고 종료
        }
    }

    private void EnsureRefs()
    {
        if (player == null)
        {
            player = GameObject.Find("Player");
            if (player == null) return;

            originalPlayerScale = player.transform.localScale;
        }

        if (flight == null) flight = player.GetComponent<FlightSimulationController>();
        if (missileLauncher == null) missileLauncher = player.GetComponent<MissileLauncher>();
        if (shield == null) shield = player.GetComponent<PlayerShield>() ?? player.AddComponent<PlayerShield>();
        if (cameraFollow == null) cameraFollow = FindAnyObjectByType<CameraFollow>();

        // 원본 값 캐시(최초 1회)
        if (flight != null && originalBaseSpeed <= 0f)
        {
            originalBaseSpeed = flight.baseSpeed;
            originalMoveSpeed = flight.moveSpeed;
            originalDashSpeed = flight.dashSpeed;
        }
        if (missileLauncher != null && originalMissileScaleMult <= 0f)
        {
            originalMissileScaleMult = missileLauncher.missileScaleMultiplier;
        }
        if (cameraFollow != null && !cachedCameraOriginal)
        {
            originalCamDistance = cameraFollow.distance;
            originalCamHeight = cameraFollow.height;
            cachedCameraOriginal = true;
        }
    }

    private IEnumerator ApplyGreenSpeedBoost()
    {
        if (flight == null)
        {
            yield break;
        }

        // 속도 2배
        float base0 = originalBaseSpeed > 0f ? originalBaseSpeed : flight.baseSpeed;
        float move0 = originalMoveSpeed > 0f ? originalMoveSpeed : flight.moveSpeed;
        float dash0 = originalDashSpeed > 0f ? originalDashSpeed : flight.dashSpeed;

        flight.baseSpeed = base0 * 2f;
        flight.moveSpeed = move0 * 2f;
        flight.dashSpeed = dash0 * 2f;

        yield return new WaitForSeconds(greenDuration);

        flight.baseSpeed = base0;
        flight.moveSpeed = move0;
        flight.dashSpeed = dash0;
    }

    private IEnumerator ApplyYellowGiant()
    {
        if (player == null) yield break;

        Vector3 scale0 = originalPlayerScale;
        player.transform.localScale = scale0 * 3f;

        // 카메라도 플레이어 커진만큼 멀어지게 (거리/높이 비례 증가)
        if (cameraFollow != null)
        {
            float d0 = cachedCameraOriginal ? originalCamDistance : cameraFollow.distance;
            float h0 = cachedCameraOriginal ? originalCamHeight : cameraFollow.height;
            cameraFollow.distance = d0 * 3f;
            cameraFollow.height = h0 * 3f;
        }

        if (missileLauncher != null)
        {
            float m0 = originalMissileScaleMult > 0f ? originalMissileScaleMult : 1f;
            missileLauncher.missileScaleMultiplier = m0 * 3f;
        }

        yield return new WaitForSeconds(yellowDuration);

        player.transform.localScale = scale0;

        // 카메라 원복
        if (cameraFollow != null)
        {
            float d0 = cachedCameraOriginal ? originalCamDistance : cameraFollow.distance / 3f;
            float h0 = cachedCameraOriginal ? originalCamHeight : cameraFollow.height / 3f;
            cameraFollow.distance = d0;
            cameraFollow.height = h0;
        }

        if (missileLauncher != null)
        {
            missileLauncher.missileScaleMultiplier = originalMissileScaleMult > 0f ? originalMissileScaleMult : 1f;
        }
    }

    private IEnumerator ApplyBlueShield()
    {
        if (shield == null) yield break;

        shield.SetActive(true);
        yield return new WaitForSeconds(blueDuration);
        shield.SetActive(false);
    }
}

