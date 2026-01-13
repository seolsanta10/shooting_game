using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 공중 기동 기술: 적 추격 중 수직 상승 → 덤블링 → 적 후방으로 순간 이동
/// </summary>
public class AerialManeuverAbility : MonoBehaviour
{
    [Header("기동 기술 설정")]
    [Tooltip("발동 키 (기본: F)")]
    public KeyCode activationKey = KeyCode.F;
    
    [Tooltip("수직 상승 속도")]
    public float ascentSpeed = 15f;
    
    [Tooltip("상승 높이")]
    public float ascentHeight = 10f;
    
    [Tooltip("덤블링 회전 속도 (도/초)")]
    public float tumbleRotationSpeed = 360f;
    
    [Tooltip("적 감지 범위")]
    public float enemyDetectionRange = 50f;
    
    [Tooltip("적 후방으로 이동할 거리")]
    public float teleportDistanceBehindEnemy = 8f;
    
    [Tooltip("기동 기술 쿨타임 (초)")]
    public float cooldownTime = 5f;
    
    [Header("Rigidbody 설정")]
    [Tooltip("Rigidbody 사용 여부 (false면 Transform 직접 조작)")]
    public bool useRigidbody = false;
    
    [Tooltip("Rigidbody가 없으면 자동으로 추가")]
    public bool autoAddRigidbody = true;
    
    [Header("카메라 설정")]
    [Tooltip("카메라 참조 (없으면 자동 찾기)")]
    public CameraFollow cameraFollow;
    
    [Tooltip("기동 중 카메라 부드러움")]
    public float cameraSmoothness = 2f;
    
    // 내부 변수
    private bool isManeuvering = false;
    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;
    private Rigidbody rb;
    private FlightSimulationController flightController;
    private Transform planetCenter;
    private float planetRadius;
    
    void Start()
    {
        // FlightSimulationController 참조
        flightController = GetComponent<FlightSimulationController>();
        if (flightController == null)
        {
            Debug.LogWarning("AerialManeuverAbility: FlightSimulationController를 찾을 수 없습니다!");
        }
        
        // Rigidbody 설정
        rb = GetComponent<Rigidbody>();
        if (rb == null && useRigidbody && autoAddRigidbody)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false; // 중력 비활성화 (비행 게임)
            rb.linearDamping = 0.5f;
            rb.angularDamping = 5f;
            Debug.Log("AerialManeuverAbility: Rigidbody가 자동으로 추가되었습니다.");
        }
        
        // 카메라 찾기
        if (cameraFollow == null)
        {
            cameraFollow = FindAnyObjectByType<CameraFollow>();
        }
        
        // 행성 중심 찾기
        GameObject ground = GameObject.Find("Ground");
        if (ground != null)
        {
            planetCenter = ground.transform;
            planetRadius = ground.transform.localScale.x * 0.5f;
        }
    }
    
    void Update()
    {
        // 쿨타임 업데이트
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                cooldownTimer = 0f;
            }
        }
        
        // 키 입력 감지
        if (Input.GetKeyDown(activationKey) && !isManeuvering && !isOnCooldown)
        {
            // 적이 추격 중인지 확인
            if (IsEnemyTrackingPlayer())
            {
                StartCoroutine(ExecuteAerialManeuver());
            }
            else
            {
                Debug.Log("AerialManeuverAbility: 적이 추격 중이 아닙니다!");
            }
        }
    }
    
    /// <summary>
    /// 적이 플레이어를 추격 중인지 확인
    /// </summary>
    bool IsEnemyTrackingPlayer()
    {
        // EnemyController를 가진 모든 적 찾기
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        
        foreach (EnemyController enemy in enemies)
        {
            if (enemy != null && enemy.IsTrackingPlayer())
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance <= enemyDetectionRange)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 가장 가까운 적 찾기
    /// </summary>
    Transform FindNearestEnemy()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        Transform nearestEnemy = null;
        float nearestDistance = float.MaxValue;
        
        foreach (EnemyController enemy in enemies)
        {
            if (enemy != null && enemy.IsTrackingPlayer())
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < nearestDistance && distance <= enemyDetectionRange)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy.transform;
                }
            }
        }
        
        return nearestEnemy;
    }
    
    /// <summary>
    /// 공중 기동 기술 실행 (Coroutine)
    /// </summary>
    IEnumerator ExecuteAerialManeuver()
    {
        isManeuvering = true;
        
        // 가장 가까운 적 찾기
        Transform targetEnemy = FindNearestEnemy();
        if (targetEnemy == null)
        {
            Debug.LogWarning("AerialManeuverAbility: 적을 찾을 수 없습니다!");
            isManeuvering = false;
            yield break;
        }
        
        Debug.Log("AerialManeuverAbility: 공중 기동 기술 시작!");
        
        // 1단계: 수직 상승
        yield return StartCoroutine(AscentPhase());
        
        // 2단계: 덤블링 회전 (상승 중)
        yield return StartCoroutine(TumblePhase());
        
        // 3단계: 적 후방으로 순간 이동
        yield return StartCoroutine(TeleportBehindEnemy(targetEnemy));
        
        // 4단계: 착지 및 안정화
        yield return StartCoroutine(StabilizePhase());
        
        isManeuvering = false;
        isOnCooldown = true;
        cooldownTimer = cooldownTime;
        
        Debug.Log("AerialManeuverAbility: 공중 기동 기술 완료!");
    }
    
    /// <summary>
    /// 1단계: 수직 상승
    /// </summary>
    IEnumerator AscentPhase()
    {
        Vector3 startPosition = transform.position;
        Vector3 upDirection = transform.up;
        
        if (planetCenter != null)
        {
            // 행성 중심에서 멀어지는 방향으로 상승
            upDirection = (transform.position - planetCenter.position).normalized;
        }
        
        Vector3 targetPosition = startPosition + upDirection * ascentHeight;
        float elapsedTime = 0f;
        float duration = ascentHeight / ascentSpeed;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, t);
            
            if (useRigidbody && rb != null)
            {
                rb.MovePosition(currentPosition);
            }
            else
            {
                transform.position = currentPosition;
            }
            
            // 카메라 업데이트
            UpdateCameraDuringManeuver();
            
            yield return null;
        }
        
        // 정확한 위치로 설정
        if (useRigidbody && rb != null)
        {
            rb.MovePosition(targetPosition);
        }
        else
        {
            transform.position = targetPosition;
        }
    }
    
    /// <summary>
    /// 2단계: 덤블링 회전 (뒤로 한 바퀴)
    /// </summary>
    IEnumerator TumblePhase()
    {
        float totalRotation = 0f;
        float targetRotation = 360f;
        Vector3 rotationAxis = transform.right; // 오른쪽 축을 중심으로 회전 (뒤로 덤블링)
        
        while (totalRotation < targetRotation)
        {
            float rotationAngle = tumbleRotationSpeed * Time.deltaTime;
            totalRotation += rotationAngle;
            
            // 회전 적용
            if (useRigidbody && rb != null)
            {
                rb.MoveRotation(rb.rotation * Quaternion.AngleAxis(rotationAngle, rotationAxis));
            }
            else
            {
                transform.Rotate(rotationAxis, rotationAngle, Space.Self);
            }
            
            // 카메라 업데이트 (뒤집힘 방지)
            UpdateCameraDuringManeuver();
            
            yield return null;
        }
        
        Debug.Log($"AerialManeuverAbility: 덤블링 완료! 총 {totalRotation}도 회전");
    }
    
    /// <summary>
    /// 3단계: 적 후방으로 순간 이동
    /// </summary>
    IEnumerator TeleportBehindEnemy(Transform enemy)
    {
        if (enemy == null)
        {
            yield break;
        }
        
        // 적의 후방 방향 계산
        Vector3 enemyBackDirection = -enemy.forward;
        
        // 적의 후방 위치 계산
        Vector3 teleportPosition = enemy.position + enemyBackDirection * teleportDistanceBehindEnemy;
        
        // 행성 표면 위에 위치하도록 조정
        if (planetCenter != null)
        {
            Vector3 directionFromPlanet = (teleportPosition - planetCenter.position).normalized;
            float currentAltitude = Vector3.Distance(transform.position, planetCenter.position) - planetRadius;
            teleportPosition = planetCenter.position + directionFromPlanet * (planetRadius + currentAltitude);
        }
        
        // 순간 이동 (부드러운 전환을 위해 짧은 시간 동안 이동)
        Vector3 startPosition = transform.position;
        float transitionTime = 0.2f; // 순간 이동 시간
        float elapsedTime = 0f;
        
        while (elapsedTime < transitionTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionTime;
            
            // 부드러운 이동 (EaseOut 효과)
            t = 1f - Mathf.Pow(1f - t, 3f);
            
            Vector3 currentPosition = Vector3.Lerp(startPosition, teleportPosition, t);
            
            if (useRigidbody && rb != null)
            {
                rb.MovePosition(currentPosition);
            }
            else
            {
                transform.position = currentPosition;
            }
            
            // 적을 바라보도록 회전
            Vector3 lookDirection = (enemy.position - transform.position).normalized;
            if (lookDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection, transform.up);
                
                if (useRigidbody && rb != null)
                {
                    rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.deltaTime * 10f));
                }
                else
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                }
            }
            
            // 카메라 업데이트
            UpdateCameraDuringManeuver();
            
            yield return null;
        }
        
        // 정확한 위치로 설정
        if (useRigidbody && rb != null)
        {
            rb.MovePosition(teleportPosition);
        }
        else
        {
            transform.position = teleportPosition;
        }
        
        Debug.Log("AerialManeuverAbility: 적 후방으로 이동 완료!");
    }
    
    /// <summary>
    /// 4단계: 착지 및 안정화
    /// </summary>
    IEnumerator StabilizePhase()
    {
        // 비행 안정화 시간
        float stabilizeTime = 0.5f;
        float elapsedTime = 0f;
        
        while (elapsedTime < stabilizeTime)
        {
            elapsedTime += Time.deltaTime;
            
            // 행성 표면을 향하도록 회전 조정
            if (planetCenter != null)
            {
                Vector3 directionFromPlanet = (transform.position - planetCenter.position).normalized;
                Vector3 up = directionFromPlanet;
                Vector3 forward = transform.forward;
                
                // forward를 행성 표면에 평행하게 조정
                Vector3 projectedForward = Vector3.ProjectOnPlane(forward, up).normalized;
                if (projectedForward.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(projectedForward, up);
                    
                    if (useRigidbody && rb != null)
                    {
                        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.deltaTime * 5f));
                    }
                    else
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                    }
                }
            }
            
            // 카메라 업데이트
            UpdateCameraDuringManeuver();
            
            yield return null;
        }
    }
    
    /// <summary>
    /// 기동 중 카메라 업데이트 (뒤집힘 방지)
    /// </summary>
    void UpdateCameraDuringManeuver()
    {
        if (cameraFollow == null) return;
        
        // 카메라가 부드럽게 따라오도록 강제 업데이트
        // CameraFollow의 preventFlip 기능이 자동으로 작동함
        // 추가적인 부드러움을 위해 카메라 위치를 직접 조정할 수도 있음
        
        // 카메라가 너무 급격하게 회전하지 않도록 제한
        Vector3 cameraToPlayer = transform.position - cameraFollow.transform.position;
        float distance = cameraToPlayer.magnitude;
        
        if (distance > 20f) // 카메라가 너무 멀어지면
        {
            // 카메라를 플레이어 쪽으로 부드럽게 이동
            Vector3 desiredCameraPos = transform.position - transform.forward * 8f + transform.up * 3f;
            cameraFollow.transform.position = Vector3.Lerp(
                cameraFollow.transform.position,
                desiredCameraPos,
                cameraSmoothness * Time.deltaTime
            );
        }
    }
    
    /// <summary>
    /// 기동 중인지 확인 (외부에서 호출 가능)
    /// </summary>
    public bool IsManeuvering()
    {
        return isManeuvering;
    }
    
    /// <summary>
    /// 쿨타임 남은 시간 반환
    /// </summary>
    public float GetCooldownRemaining()
    {
        return Mathf.Max(0f, cooldownTimer);
    }
    
    void OnGUI()
    {
        // 디버그 정보 표시
        if (isManeuvering)
        {
            GUI.Label(new Rect(10, 10, 300, 20), "공중 기동 기술 실행 중...");
        }
        
        if (isOnCooldown)
        {
            GUI.Label(new Rect(10, 30, 300, 20), $"쿨타임: {cooldownTimer:F1}초");
        }
    }
}
