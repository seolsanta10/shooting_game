using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Back Turn Ability: W 더블 탭으로 발동하는 플레이어 주도 공중 기동 기술
/// - Phase A: 수직 상승
/// - Phase B: 뒤로 덤블링 360도 회전
/// - Phase C: 안정화 및 Exit Heading 정렬
/// </summary>
public class BackTurnAbility : MonoBehaviour
{
    [Header("Back Turn 설정")]
    [Tooltip("수직 상승 속도")]
    public float ascentSpeed = 25f;
    
    [Tooltip("상승 높이 (더 크게 돌기 위해 증가)")]
    public float ascentHeight = 12f;
    
    [Tooltip("Phase A 지속 시간 (초)")]
    public float phaseADuration = 0.35f;
    
    [Tooltip("Phase B 지속 시간 (초) - 더 크게 돌기 위해 증가")]
    public float phaseBDuration = 0.6f;
    
    [Tooltip("Phase C 지속 시간 (초)")]
    public float phaseCDuration = 0.3f;
    
    [Tooltip("덤블링 회전 속도 (도/초) - 더 빠르게 회전")]
    public float tumbleRotationSpeed = 900f; // 360도 / 0.6초 = 600도/초, 하지만 더 빠르게
    
    [Tooltip("회전 반경 배율 (더 크게 돌기)")]
    public float rotationRadiusMultiplier = 1.5f; // 회전 반경을 더 크게
    
    [Tooltip("회전 가속 사용 여부")]
    public bool useRotationAcceleration = true;
    
    [Tooltip("초기 회전 속도 배율 (가속 시작)")]
    public float initialRotationSpeedMultiplier = 0.5f; // 초반에는 느리게
    
    [Tooltip("최종 회전 속도 배율 (가속 종료)")]
    public float finalRotationSpeedMultiplier = 2.0f; // 후반에는 빠르게
    
    [Tooltip("적 감지 범위")]
    public float enemyDetectionRange = 50f;
    
    [Tooltip("쿨타임 (초)")]
    public float cooldownTime = 4.5f;
    
    [Header("Exit Heading 설정")]
    [Tooltip("Exit Heading 계산 방식")]
    public ExitHeadingMode exitHeadingMode = ExitHeadingMode.ThreatAware;
    
    [Tooltip("Threat-aware: 뒤에 있는 적에 대한 가중치")]
    public float behindEnemyWeight = 2f;
    
    [Tooltip("Threat-aware: 가까운 적에 대한 가중치")]
    public float distanceWeight = 1f;
    
    [Header("Rigidbody 설정")]
    [Tooltip("Rigidbody 사용 여부 (false면 Transform 직접 조작)")]
    public bool useRigidbody = false; // 기본값을 false로 변경 (Rigidbody 없이도 작동)
    
    [Tooltip("Rigidbody가 없으면 자동 추가")]
    public bool autoAddRigidbody = true;
    
    [Tooltip("상승 중 적용할 힘")]
    public float ascentForce = 500f;
    
    [Tooltip("안정화 중 속도 감쇠")]
    public float stabilizationDamping = 0.8f;
    
    // 내부 변수
    private bool isBackTurning = false;
    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;
    private Rigidbody rb;
    private Transform planetCenter;
    private float planetRadius;
    private Vector3 planetUp;
    
    // Exit Heading 관련
    private Vector3 computedExitHeading;
    private Transform threatEnemy;
    
    // 카메라 관련
    private CameraFollow cameraFollow;
    private bool wasFirstPerson = false; // 이전 1인칭 상태 저장
    
    public enum ExitHeadingMode
    {
        ThreatAware,    // 가장 위협적인 적을 마주보도록
        PlayerChoice    // 플레이어 입력으로 선택 (확장용)
    }
    
    void Start()
    {
        // Rigidbody 설정
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            if (useRigidbody && autoAddRigidbody)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.linearDamping = 0.5f;
                rb.angularDamping = 5f;
                Debug.Log("BackTurnAbility: Rigidbody가 자동으로 추가되었습니다.");
            }
            else if (useRigidbody)
            {
                Debug.LogWarning("BackTurnAbility: useRigidbody가 true이지만 Rigidbody가 없습니다. autoAddRigidbody를 true로 설정하거나 수동으로 Rigidbody를 추가해주세요.");
            }
            else
            {
                Debug.Log("BackTurnAbility: Rigidbody 없이 작동합니다 (Transform 직접 조작 모드).");
            }
        }
        else
        {
            Debug.Log("BackTurnAbility: 기존 Rigidbody를 사용합니다.");
        }
        
        // 행성 중심 찾기
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = GameObject.Find("지구");
        }
        if (ground != null)
        {
            planetCenter = ground.transform;
            planetRadius = ground.transform.localScale.x * 0.5f;
        }
        
        // 카메라 찾기
        if (cameraFollow == null)
        {
            cameraFollow = FindObjectOfType<CameraFollow>();
        }
        
        // 카메라가 없으면 경고
        if (cameraFollow == null)
        {
            Debug.LogWarning("BackTurnAbility: CameraFollow를 찾을 수 없습니다. 1인칭 시점 전환이 작동하지 않을 수 있습니다.");
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
        
        // 행성 up 벡터 업데이트
        if (planetCenter != null)
        {
            planetUp = (transform.position - planetCenter.position).normalized;
        }
        else
        {
            planetUp = Vector3.up;
        }
    }
    
    /// <summary>
    /// Back Turn 발동 (외부에서 호출)
    /// </summary>
    public bool ActivateBackTurn()
    {
        if (isBackTurning)
        {
            Debug.LogWarning("BackTurnAbility: 이미 Back Turn이 실행 중입니다.");
            return false;
        }
        
        if (isOnCooldown)
        {
            Debug.LogWarning($"BackTurnAbility: 쿨타임 중입니다. 남은 시간: {cooldownTimer:F1}초");
            return false;
        }
        
        Debug.Log("BackTurnAbility: ActivateBackTurn() 호출됨");
        StartCoroutine(ExecuteBackTurn());
        return true;
    }
    
    /// <summary>
    /// Back Turn 실행 (메인 Coroutine)
    /// </summary>
    IEnumerator ExecuteBackTurn()
    {
        isBackTurning = true;
        Debug.Log("BackTurnAbility: Back Turn 시작!");
        
        // Exit Heading 계산
        ComputeExitHeading();
        
        // Phase A: 수직 상승
        yield return StartCoroutine(PhaseA_Ascent());
        
        // Phase B: 뒤로 덤블링
        yield return StartCoroutine(PhaseB_Tumble());
        
        // Phase C: 안정화 및 Exit Heading 정렬
        yield return StartCoroutine(PhaseC_Stabilize());
        
        isBackTurning = false;
        isOnCooldown = true;
        cooldownTimer = cooldownTime;
        
        Debug.Log($"BackTurnAbility: Back Turn 완료! Exit Heading: {computedExitHeading}");
    }
    
    /// <summary>
    /// Phase A: 수직 상승
    /// </summary>
    IEnumerator PhaseA_Ascent()
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + planetUp * ascentHeight;
        
        float elapsedTime = 0f;
        float duration = phaseADuration;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // 부드러운 상승 (EaseOut)
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, smoothT);
            
            if (useRigidbody && rb != null)
            {
                // Rigidbody로 상승
                Vector3 force = planetUp * ascentForce * (1f - t); // 시간에 따라 힘 감소
                rb.AddForce(force * Time.deltaTime, ForceMode.Force);
                rb.MovePosition(currentPosition);
            }
            else
            {
                transform.position = currentPosition;
            }
            
            yield return null;
        }
        
        // 정확한 위치로 설정
        if (useRigidbody && rb != null)
        {
            rb.MovePosition(targetPosition);
            rb.linearVelocity = Vector3.zero; // 상승 완료 후 속도 초기화
        }
        else
        {
            transform.position = targetPosition;
        }
        
        Debug.Log("BackTurnAbility: Phase A (상승) 완료");
    }
    
    /// <summary>
    /// Phase B: 뒤로 덤블링 (360도 회전) - 더 크게 회전
    /// </summary>
    IEnumerator PhaseB_Tumble()
    {
        // 카메라를 1인칭 시점으로 전환
        if (cameraFollow != null)
        {
            wasFirstPerson = cameraFollow.IsFirstPerson();
            cameraFollow.SetFirstPerson(true);
            Debug.Log("BackTurnAbility: 카메라를 1인칭 시점으로 전환");
        }
        
        float totalRotation = 0f;
        float targetRotation = 360f;
        Vector3 rotationAxis = transform.right; // 오른쪽 축을 중심으로 회전 (뒤로 덤블링)
        
        Quaternion startRotation = transform.rotation;
        Vector3 startPosition = transform.position;
        
        // 더 크게 돌기 위해 회전 반경 증가 (상승 중 회전)
        float rotationRadius = ascentHeight * 0.3f * rotationRadiusMultiplier; // 회전 반경 배율 적용
        
        float elapsedTime = 0f;
        float duration = phaseBDuration;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // 회전 가속 적용
            float currentSpeedMultiplier = 1f;
            if (useRotationAcceleration)
            {
                // 초반에는 느리게, 후반에는 빠르게 (EaseIn 효과)
                // t가 0일 때 initialMultiplier, t가 1일 때 finalMultiplier
                currentSpeedMultiplier = Mathf.Lerp(initialRotationSpeedMultiplier, finalRotationSpeedMultiplier, t);
                // 더 부드러운 가속을 위해 EaseIn 적용
                float easedT = t * t; // EaseIn (Quadratic)
                currentSpeedMultiplier = Mathf.Lerp(initialRotationSpeedMultiplier, finalRotationSpeedMultiplier, easedT);
            }
            
            // 회전 각도 계산 (가속 적용)
            float rotationAngle = tumbleRotationSpeed * currentSpeedMultiplier * Time.deltaTime;
            totalRotation += rotationAngle;
            
            // 회전 적용 (항상 planetUp을 up으로 고정)
            Quaternion deltaRotation = Quaternion.AngleAxis(rotationAngle, rotationAxis);
            Quaternion newRotation = deltaRotation * transform.rotation;
            
            // planetUp을 up으로 고정하여 뒤집힘 방지
            Vector3 currentForward = newRotation * Vector3.forward;
            Quaternion stabilizedRotation = Quaternion.LookRotation(currentForward, planetUp);
            
            if (useRigidbody && rb != null)
            {
                rb.MoveRotation(stabilizedRotation);
                
                // 더 크게 돌기 위해 원형 경로 추가 (가속에 따라 반경도 증가)
                if (rotationRadius > 0f && planetCenter != null)
                {
                    Vector3 centerToPlayer = (startPosition - planetCenter.position).normalized;
                    Vector3 rightDir = Vector3.Cross(centerToPlayer, planetUp).normalized;
                    Vector3 forwardDir = Vector3.Cross(rightDir, centerToPlayer).normalized;
                    
                    // 가속에 따라 반경도 증가 (회전이 빠를수록 더 큰 원)
                    float currentRadius = rotationRadius;
                    if (useRotationAcceleration)
                    {
                        // 가속에 따라 반경 증가
                        float radiusMultiplier = Mathf.Lerp(1f, 1.5f, t);
                        currentRadius = rotationRadius * radiusMultiplier;
                    }
                    
                    // 원형 경로 계산
                    float angle = totalRotation * Mathf.Deg2Rad;
                    Vector3 offset = (rightDir * Mathf.Sin(angle) + forwardDir * Mathf.Cos(angle)) * currentRadius;
                    Vector3 desiredPosition = startPosition + offset;
                    
                    // 행성 표면 위에 유지
                    Vector3 directionFromPlanet = (desiredPosition - planetCenter.position).normalized;
                    float currentDistance = Vector3.Distance(startPosition, planetCenter.position);
                    desiredPosition = planetCenter.position + directionFromPlanet * currentDistance;
                    
                    rb.MovePosition(desiredPosition);
                }
            }
            else
            {
                transform.rotation = stabilizedRotation;
                
                // 더 크게 돌기 위해 원형 경로 추가 (가속에 따라 반경도 증가)
                if (rotationRadius > 0f && planetCenter != null)
                {
                    Vector3 centerToPlayer = (startPosition - planetCenter.position).normalized;
                    Vector3 rightDir = Vector3.Cross(centerToPlayer, planetUp).normalized;
                    Vector3 forwardDir = Vector3.Cross(rightDir, centerToPlayer).normalized;
                    
                    // 가속에 따라 반경도 증가 (회전이 빠를수록 더 큰 원)
                    float currentRadius = rotationRadius;
                    if (useRotationAcceleration)
                    {
                        // 가속에 따라 반경 증가
                        float radiusMultiplier = Mathf.Lerp(1f, 1.5f, t);
                        currentRadius = rotationRadius * radiusMultiplier;
                    }
                    
                    // 원형 경로 계산
                    float angle = totalRotation * Mathf.Deg2Rad;
                    Vector3 offset = (rightDir * Mathf.Sin(angle) + forwardDir * Mathf.Cos(angle)) * currentRadius;
                    Vector3 desiredPosition = startPosition + offset;
                    
                    // 행성 표면 위에 유지
                    Vector3 directionFromPlanet = (desiredPosition - planetCenter.position).normalized;
                    float currentDistance = Vector3.Distance(startPosition, planetCenter.position);
                    desiredPosition = planetCenter.position + directionFromPlanet * currentDistance;
                    
                    transform.position = desiredPosition;
                }
            }
            
            // 회전 축도 업데이트 (로컬 공간에서)
            rotationAxis = transform.right;
            
            yield return null;
        }
        
        float avgSpeedMultiplier = useRotationAcceleration 
            ? (initialRotationSpeedMultiplier + finalRotationSpeedMultiplier) * 0.5f 
            : 1f;
        Debug.Log($"BackTurnAbility: Phase B (덤블링) 완료! 총 {totalRotation}도 회전, 반경: {rotationRadius}m, 평균 속도 배율: {avgSpeedMultiplier:F2}x");
        
        // 카메라를 원래 시점으로 복귀
        if (cameraFollow != null)
        {
            cameraFollow.SetFirstPerson(wasFirstPerson);
            Debug.Log($"BackTurnAbility: 카메라를 원래 시점으로 복귀 (1인칭: {wasFirstPerson})");
        }
    }
    
    /// <summary>
    /// Phase C: 안정화 및 Exit Heading 정렬
    /// </summary>
    IEnumerator PhaseC_Stabilize()
    {
        float elapsedTime = 0f;
        float duration = phaseCDuration;
        
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(computedExitHeading, planetUp);
        
        // 현재 속도 저장 (Rigidbody 사용 시)
        Vector3 currentVelocity = Vector3.zero;
        if (useRigidbody && rb != null)
        {
            currentVelocity = rb.linearVelocity;
        }
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // 부드러운 회전 정렬
            Quaternion smoothRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            
            // planetUp을 up으로 고정
            Vector3 smoothForward = smoothRotation * Vector3.forward;
            Quaternion finalRotation = Quaternion.LookRotation(smoothForward, planetUp);
            
            if (useRigidbody && rb != null)
            {
                rb.MoveRotation(finalRotation);
                
                // 속도 재정렬 (Exit Heading 방향으로)
                Vector3 desiredVelocity = computedExitHeading.normalized * currentVelocity.magnitude;
                rb.linearVelocity = Vector3.Lerp(currentVelocity, desiredVelocity, t * stabilizationDamping);
            }
            else
            {
                transform.rotation = finalRotation;
            }
            
            yield return null;
        }
        
        // 최종 정렬
        Quaternion finalTargetRotation = Quaternion.LookRotation(computedExitHeading, planetUp);
        if (useRigidbody && rb != null)
        {
            rb.MoveRotation(finalTargetRotation);
            Vector3 finalVelocity = computedExitHeading.normalized * currentVelocity.magnitude;
            rb.linearVelocity = finalVelocity;
        }
        else
        {
            transform.rotation = finalTargetRotation;
        }
        
        Debug.Log("BackTurnAbility: Phase C (안정화) 완료");
    }
    
    /// <summary>
    /// Exit Heading 계산 (적이 전방에 오도록)
    /// </summary>
    void ComputeExitHeading()
    {
        switch (exitHeadingMode)
        {
            case ExitHeadingMode.ThreatAware:
                computedExitHeading = ComputeThreatAwareExitHeading();
                break;
            case ExitHeadingMode.PlayerChoice:
                computedExitHeading = ComputePlayerChoiceExitHeading();
                break;
            default:
                computedExitHeading = transform.forward; // 폴백
                break;
        }
        
        // Exit Heading 정규화
        if (computedExitHeading.magnitude < 0.1f)
        {
            computedExitHeading = transform.forward; // 안전 폴백
        }
        else
        {
            computedExitHeading = computedExitHeading.normalized;
        }
        
        Debug.Log($"BackTurnAbility: Exit Heading 계산 완료 - {computedExitHeading}, Mode: {exitHeadingMode}");
    }
    
    /// <summary>
    /// Threat-aware Exit Heading 계산
    /// 가장 위협적인 적(가까운/뒤에 있는)을 마주보도록
    /// </summary>
    Vector3 ComputeThreatAwareExitHeading()
    {
        EnemyController[] enemies = FindObjectsOfType<EnemyController>();
        
        if (enemies.Length == 0)
        {
            Debug.Log("BackTurnAbility: 적이 없음. 현재 진행 방향 유지");
            return transform.forward; // 안전 폴백
        }
        
        Transform mostThreateningEnemy = null;
        float maxThreatScore = float.MinValue;
        
        foreach (EnemyController enemy in enemies)
        {
            if (enemy == null) continue;
            
            Transform enemyTransform = enemy.transform;
            Vector3 toEnemy = enemyTransform.position - transform.position;
            float distance = toEnemy.magnitude;
            
            // 감지 범위 체크
            if (distance > enemyDetectionRange) continue;
            
            // 위협 점수 계산
            float threatScore = 0f;
            
            // 1. 거리 기반 점수 (가까울수록 높음)
            float distanceScore = 1f / (distance + 1f) * distanceWeight;
            threatScore += distanceScore;
            
            // 2. 뒤에 있는 적에 대한 가중치 (뒤에 있을수록 위협적)
            Vector3 playerForward = transform.forward;
            float dotProduct = Vector3.Dot(playerForward, toEnemy.normalized);
            
            // dotProduct < 0 이면 뒤에 있음
            if (dotProduct < 0f)
            {
                float behindScore = Mathf.Abs(dotProduct) * behindEnemyWeight;
                threatScore += behindScore;
            }
            
            // 가장 위협적인 적 선택
            if (threatScore > maxThreatScore)
            {
                maxThreatScore = threatScore;
                mostThreateningEnemy = enemyTransform;
            }
        }
        
        if (mostThreateningEnemy == null)
        {
            Debug.Log("BackTurnAbility: 위협적인 적을 찾을 수 없음. 현재 진행 방향 유지");
            return transform.forward; // 안전 폴백
        }
        
        threatEnemy = mostThreateningEnemy;
        
        // 적을 마주보는 방향 계산
        Vector3 toThreatEnemy = mostThreateningEnemy.position - transform.position;
        
        // planetUp에 평행한 평면에 투영
        Vector3 projectedDirection = Vector3.ProjectOnPlane(toThreatEnemy, planetUp).normalized;
        
        if (projectedDirection.magnitude < 0.1f)
        {
            // 투영이 실패하면 현재 forward 사용
            projectedDirection = Vector3.ProjectOnPlane(transform.forward, planetUp).normalized;
        }
        
        Debug.Log($"BackTurnAbility: 가장 위협적인 적 발견 - 거리: {Vector3.Distance(transform.position, mostThreateningEnemy.position):F1}m, 위협 점수: {maxThreatScore:F2}");
        
        return projectedDirection;
    }
    
    /// <summary>
    /// Player-choice Exit Heading 계산 (확장용)
    /// 현재는 입력 없으면 Threat-aware와 동일
    /// </summary>
    Vector3 ComputePlayerChoiceExitHeading()
    {
        // TODO: 플레이어 입력(A/D)으로 좌/우 선택 구현
        // 현재는 Threat-aware로 폴백
        return ComputeThreatAwareExitHeading();
    }
    
    /// <summary>
    /// Back Turn 중인지 확인
    /// </summary>
    public bool IsBackTurning()
    {
        return isBackTurning;
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
        if (isBackTurning)
        {
            GUI.Label(new Rect(10, 10, 400, 20), "Back Turn 실행 중...");
            GUI.Label(new Rect(10, 30, 400, 20), $"Exit Heading: {computedExitHeading}");
            if (threatEnemy != null)
            {
                float distance = Vector3.Distance(transform.position, threatEnemy.position);
                GUI.Label(new Rect(10, 50, 400, 20), $"위협 적 거리: {distance:F1}m");
            }
        }
        
        if (isOnCooldown)
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"Back Turn 쿨타임: {cooldownTimer:F1}초");
        }
    }
}
