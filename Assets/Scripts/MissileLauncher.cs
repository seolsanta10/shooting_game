using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class MissileLauncher : MonoBehaviour
{
    [Header("미사일 설정")]
    public GameObject missilePrefab;
    public Transform firePoint;
    public float fireRate = 0.5f;

    [Tooltip("미사일 속도 최소값(플레이어보다 항상 빠르게 유지용)")]
    public float missileMinSpeed = 30f;

    [Tooltip("플레이어의 최대 속도 대비 미사일 속도 배율 (예: 1.5면 50% 더 빠름)")]
    public float missileSpeedMultiplierOfPlayerMax = 1.5f;

    [Header("스킬(아이템) 효과")]
    [Tooltip("미사일 크기 배율 (노랑 아이템: 3배)")]
    public float missileScaleMultiplier = 1f;
    
    private float nextFireTime = 0f;
    private Mouse mouse;
    
    void Start()
    {
        mouse = Mouse.current;

        // 전역 프리팹 설정(있으면) 적용
        GamePrefabSettings settings = GamePrefabSettings.LoadOrNull();
        if (settings != null && missilePrefab == null && settings.playerMissilePrefab != null)
        {
            missilePrefab = settings.playerMissilePrefab;
        }
        
        // 발사점이 없으면 자동으로 생성
        if (firePoint == null)
        {
            GameObject firePointObj = new GameObject("FirePoint");
            firePointObj.transform.SetParent(transform);
            firePointObj.transform.localPosition = new Vector3(0, 0, 1f); // 비행기 앞쪽
            firePointObj.transform.localRotation = Quaternion.identity; // 플레이어와 같은 회전
            firePoint = firePointObj.transform;
        }
        
        // FirePoint의 회전을 플레이어와 동기화
        if (firePoint != null)
        {
            firePoint.localRotation = Quaternion.identity; // 플레이어와 같은 회전
        }
        
        // 미사일 프리팹이 없으면 기본 미사일 생성
        if (missilePrefab == null)
        {
            CreateDefaultMissilePrefab();
        }
    }
    
    void Update()
    {
        // FirePoint의 회전을 플레이어와 동기화 (매 프레임)
        if (firePoint != null)
        {
            firePoint.localRotation = Quaternion.identity; // 플레이어와 같은 회전
        }
        
        // 마우스 좌클릭: 미사일 발사
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && Time.time >= nextFireTime)
        {
            FireMissile();
            nextFireTime = Time.time + fireRate;
        }
        
        // L키: 미사일 발사
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.lKey.wasPressedThisFrame && Time.time >= nextFireTime)
        {
            FireMissile();
            nextFireTime = Time.time + fireRate;
        }
    }
    
    void FireMissile()
    {
        if (missilePrefab == null || firePoint == null) return;
        
        // 플레이어의 정면 방향으로 미사일 발사
        // FirePoint의 회전을 플레이어의 회전과 동기화
        Quaternion missileRotation = transform.rotation; // 플레이어의 회전 사용
        
        // Ground 찾기
        GameObject ground = GameObject.Find("Ground");
        if (ground == null) ground = GameObject.Find("지구");
        if (ground != null)
        {
            Transform groundCenter = ground.transform;
            Vector3 directionFromGround = (firePoint.position - groundCenter.position).normalized;
            Vector3 up = directionFromGround;
            
            // 플레이어의 forward를 Ground 표면에 투영
            Vector3 playerForward = transform.forward;
            Vector3 forward = Vector3.ProjectOnPlane(playerForward, up).normalized;
            
            if (forward.magnitude > 0.1f)
            {
                // Ground 표면에 맞게 회전 설정
                missileRotation = Quaternion.LookRotation(forward, up);
            }
        }
        
        GameObject missile = Instantiate(missilePrefab, firePoint.position, missileRotation);
        
        // 미사일 컴포넌트 추가/가져오기
        Missile missileComp = missile.GetComponent<Missile>();
        if (missileComp == null) missileComp = missile.AddComponent<Missile>();

        // 플레이어(기체) 속도보다 항상 빠르게: 현재/최대 속도 기반으로 미사일 속도 세팅
        FlightSimulationController flight = GetComponent<FlightSimulationController>();
        if (flight != null && missileComp != null)
        {
            float playerMax = Mathf.Max(
                flight.dashSpeed,
                flight.baseSpeed + flight.moveSpeed,
                flight.baseSpeed
            );

            float desiredSpeed = Mathf.Max(missileMinSpeed, playerMax * Mathf.Max(1.0f, missileSpeedMultiplierOfPlayerMax));
            missileComp.speed = Mathf.Max(missileComp.speed, desiredSpeed);
        }
        
        // 미사일 크기 설정
        missile.transform.localScale = Vector3.one * (0.2f * Mathf.Max(0.01f, missileScaleMultiplier));
    }
    
    void CreateDefaultMissilePrefab()
    {
        // 기본 미사일 프리팹 생성 (원형)
        GameObject defaultMissile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        defaultMissile.name = "Missile";
        defaultMissile.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f); // 원형이므로 균일한 크기
        
        // 색상 설정
        Renderer renderer = defaultMissile.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.yellow;
            renderer.material = mat;
        }
        
        // Collider 설정
        Collider collider = defaultMissile.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        
        missilePrefab = defaultMissile;
    }
}
