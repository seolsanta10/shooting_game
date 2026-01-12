using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Missile : MonoBehaviour
{
    [Header("미사일 설정")]
    public float speed = 20f;
    public float lifetime = 5f;
    public GameObject explosionEffect;

    [Header("행성(구면) 이동 설정")]
    public string groundObjectName = "Ground";   // 중심(행성) 오브젝트 이름
    public bool useSphericalMotion = true;       // Ground 있으면 구면 이동
    public float fallbackGroundRadius = 25f;     // Ground 스케일로 계산 실패 시
    public bool alignRotationToMotion = true;    // 이동 방향 바라보게 회전
    public float rotationLerp = 1f;              // 1이면 즉시, 0~1이면 부드럽게

    [Header("충돌/피해")]
    public float damage = 50f;
    public string enemyNamePrefix = "enemy";

    private float timer = 0f;

    private Transform groundCenter;
    private float groundRadius = 25f;

    private Rigidbody rb;
    private Collider col;

    // 구면 좌표계에서의 현재 반지름(=지표면 + 고도)
    private float currentDistance;

    // “진행(접선) 방향” (항상 planetUp에 수직인 방향으로 유지)
    private Vector3 tangentDirection;

    void Awake()
    {
        // Rigidbody 세팅
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true; // MovePosition/MoveRotation 기반이면 kinematic이 안정적

        // Collider 세팅
        col = GetComponent<Collider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;

        // 레이어는 필요 시 프로젝트에 맞게 변경
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    void Start()
    {
        // Ground 찾기
        GameObject ground = GameObject.Find(groundObjectName);
        if (ground != null)
        {
            groundCenter = ground.transform;

            // 스케일 기반 반지름 계산 (구 오브젝트가 Uniform Scale이라고 가정)
            float scaleX = ground.transform.localScale.x;
            if (scaleX > 0.0001f) groundRadius = scaleX * 0.5f;
            else groundRadius = fallbackGroundRadius;
        }

        // 초기 거리(고도 유지용)
        if (groundCenter != null && useSphericalMotion)
        {
            currentDistance = Vector3.Distance(transform.position, groundCenter.position);
            if (currentDistance < 0.0001f)
                currentDistance = groundRadius + 1f;
        }
        else
        {
            currentDistance = 0f;
        }

        // 초기 진행 방향: 현재 forward를 planetUp에 투영해서 접선 방향으로 만든다
        InitTangentDirection();
    }

    void InitTangentDirection()
    {
        Vector3 forward = transform.forward;

        if (groundCenter != null && useSphericalMotion)
        {
            Vector3 planetUp = (transform.position - groundCenter.position).normalized;

            Vector3 projected = Vector3.ProjectOnPlane(forward, planetUp);
            if (projected.sqrMagnitude < 0.0001f)
            {
                // forward가 up에 너무 평행이면 right로 대체
                projected = Vector3.ProjectOnPlane(transform.right, planetUp);
            }

            tangentDirection = projected.normalized;
        }
        else
        {
            tangentDirection = forward.normalized;
        }
    }

    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (groundCenter == null || !useSphericalMotion)
        {
            // Ground가 없으면 직선 이동 + 회전 정렬
            Vector3 newPos = rb.position + tangentDirection * speed * Time.fixedDeltaTime;

            rb.MovePosition(newPos);

            if (alignRotationToMotion && tangentDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(tangentDirection, transform.up);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationLerp));
            }

            return;
        }

        // ---- 구면 이동 ----
        Vector3 center = groundCenter.position;

        // 현재 planetUp (중심 -> 미사일)
        Vector3 radial = (rb.position - center);
        float dist = radial.magnitude;
        if (dist < 0.0001f) dist = currentDistance;
        radial /= dist;

        // 고도 유지: currentDistance를 유지
        if (currentDistance <= 0.0001f)
            currentDistance = dist;

        // 접선 방향이 planetUp에 수직이 되게 정규화
        Vector3 planetUp = radial;
        Vector3 tangent = Vector3.ProjectOnPlane(tangentDirection, planetUp);
        if (tangent.sqrMagnitude < 0.0001f)
        {
            // 접선이 너무 작으면 임의로 만들어준다
            tangent = Vector3.ProjectOnPlane(transform.forward, planetUp);
            if (tangent.sqrMagnitude < 0.0001f)
                tangent = Vector3.ProjectOnPlane(transform.right, planetUp);
        }
        tangent = tangent.normalized;

        // 구면 위를 speed로 이동하기 위한 각도(rad)
        float angleRad = (speed * Time.fixedDeltaTime) / currentDistance;

        // 회전축: radial과 tangent로 결정
        Vector3 axis = Vector3.Cross(radial, tangent);
        if (axis.sqrMagnitude < 0.0001f)
        {
            // 축이 불안정하면 그냥 직선처럼 처리
            Vector3 fallbackPos = rb.position + tangent * speed * Time.fixedDeltaTime;
            rb.MovePosition(fallbackPos);
            tangentDirection = tangent;
            return;
        }
        axis.Normalize();

        // radial을 axis로 angle만큼 회전시켜 새 radial 얻기
        Quaternion stepRot = Quaternion.AngleAxis(angleRad * Mathf.Rad2Deg, axis);
        Vector3 newRadial = (stepRot * radial).normalized;

        // 새 위치 (고도 유지)
        Vector3 newPosOnSphere = center + newRadial * currentDistance;
        rb.MovePosition(newPosOnSphere);

        // 방향/회전 정렬: "이동 접선 방향"을 바라보게
        // 새 planetUp 기준으로 tangent를 다시 투영해 안정화
        Vector3 newUp = newRadial;
        Vector3 newForward = Vector3.ProjectOnPlane(tangent, newUp);
        if (newForward.sqrMagnitude < 0.0001f)
            newForward = tangent;

        newForward.Normalize();

        if (alignRotationToMotion)
        {
            Quaternion targetRot = Quaternion.LookRotation(newForward, newUp);
            // rotationLerp=1이면 즉시, 낮추면 부드럽게
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationLerp));
        }

        // 다음 프레임용 접선 방향 업데이트(항상 안정된 접선)
        tangentDirection = newForward;
    }

    void OnTriggerEnter(Collider other)
    {
        // 적 총알과 충돌 시
        if (other.gameObject.GetComponent<EnemyBullet>() != null)
        {
            Destroy(other.gameObject);
            Explode();
            Destroy(gameObject);
            return;
        }

        // 플레이어 미사일끼리 충돌 무시
        if (other.gameObject.GetComponent<Missile>() != null)
            return;

        // 적과 충돌 시
        if (other.gameObject.name.StartsWith(enemyNamePrefix))
        {
            EnemyHealthBar healthBar = other.GetComponent<EnemyHealthBar>();
            if (healthBar != null)
            {
                healthBar.TakeDamage(damage);
            }

            Explode();
            Destroy(gameObject);
            return;
        }

        // 지구/플레이어 제외, 다른 오브젝트와 충돌 시 폭발
        if (other.gameObject.name != "Player" &&
            other.gameObject.name != groundObjectName &&
            other.gameObject.name != "지구")
        {
            Explode();
            Destroy(gameObject);
        }
    }

    void Explode()
    {
        if (explosionEffect != null)
            Instantiate(explosionEffect, rb != null ? rb.position : transform.position, Quaternion.identity);
    }
}