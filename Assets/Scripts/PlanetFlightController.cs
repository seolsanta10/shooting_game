using UnityEngine;

public class PlanetFlightController : MonoBehaviour
{
    [Header("지구 설정")]
    public Transform planetCenter;
    public float planetRadius = 5f;
    
    [Header("비행 설정")]
    public float moveSpeed = 5f;
    public float altitude = 2f;
    public float minAltitude = 1f;
    public float maxAltitude = 10f;
    public float altitudeChangeSpeed = 2f;
    
    [Header("회전 설정")]
    public float rotationSpeed = 5f;
    
    private float currentAltitude;
    private Vector3 currentDirection = Vector3.forward;
    
    void Start()
    {
        // 지구를 자동으로 찾기
        if (planetCenter == null)
        {
            GameObject planet = GameObject.Find("지구");
            if (planet != null)
            {
                planetCenter = planet.transform;
                planetRadius = planet.transform.localScale.x * 0.5f; // 스케일의 절반이 반지름
            }
        }
        
        currentAltitude = altitude;
        
        // 초기 위치 설정 (지구 위에 배치)
        if (planetCenter != null)
        {
            Vector3 directionFromPlanet = (transform.position - planetCenter.position).normalized;
            float currentDistance = Vector3.Distance(transform.position, planetCenter.position);
            currentAltitude = currentDistance - planetRadius;
            currentDirection = directionFromPlanet;
        }
    }
    
    void Update()
    {
        if (planetCenter == null) return;
        
        // WASD 입력 처리
        float horizontal = 0f;
        float vertical = 0f;
        
        if (Input.GetKey(KeyCode.W)) vertical = 1f;
        if (Input.GetKey(KeyCode.S)) vertical = -1f;
        if (Input.GetKey(KeyCode.A)) horizontal = -1f;
        if (Input.GetKey(KeyCode.D)) horizontal = 1f;
        
        // 고도 조절 (Space/Shift)
        if (Input.GetKey(KeyCode.Space))
        {
            currentAltitude += altitudeChangeSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentAltitude -= altitudeChangeSpeed * Time.deltaTime;
        }
        
        currentAltitude = Mathf.Clamp(currentAltitude, minAltitude, maxAltitude);
        
        // 지구 중심으로부터의 방향 벡터
        Vector3 directionFromPlanet = (transform.position - planetCenter.position).normalized;
        
        // 구 표면에 접하는 방향으로 이동
        if (horizontal != 0f || vertical != 0f)
        {
            // 지구 표면에 접하는 평면에서의 방향 계산
            Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward, directionFromPlanet).normalized;
            Vector3 right = Vector3.ProjectOnPlane(Vector3.right, directionFromPlanet).normalized;
            
            // 방향 벡터가 너무 작으면 현재 transform 기준으로 계산
            if (forward.magnitude < 0.1f)
            {
                forward = Vector3.ProjectOnPlane(transform.forward, directionFromPlanet).normalized;
                right = Vector3.ProjectOnPlane(transform.right, directionFromPlanet).normalized;
            }
            
            // 이동 방향 계산
            Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;
            
            // 구 표면을 따라 이동 (구의 중심을 기준으로 회전)
            if (moveDirection.magnitude > 0.1f)
            {
                // 구 표면을 따라 이동하기 위한 각도 계산
                float angle = moveSpeed * Time.deltaTime / (planetRadius + currentAltitude);
                Vector3 rotationAxis = Vector3.Cross(directionFromPlanet, moveDirection).normalized;
                
                if (rotationAxis.magnitude > 0.1f)
                {
                    Quaternion rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, rotationAxis);
                    directionFromPlanet = rotation * directionFromPlanet;
                    directionFromPlanet.Normalize();
                    
                    currentDirection = directionFromPlanet;
                }
            }
        }
        
        // 위치 업데이트 (지구 중심으로부터 고도만큼 떨어진 위치)
        float totalDistance = planetRadius + currentAltitude;
        transform.position = planetCenter.position + currentDirection * totalDistance;
        
        // 비행기가 지구를 향하도록 회전
        Vector3 up = directionFromPlanet;
        Vector3 flightForward = transform.forward;
        
        // 이동 방향이 있으면 그 방향으로 회전
        if (horizontal != 0f || vertical != 0f)
        {
            Vector3 moveDir = Vector3.ProjectOnPlane(Vector3.forward, up).normalized * vertical +
                             Vector3.ProjectOnPlane(Vector3.right, up).normalized * horizontal;
            
            if (moveDir.magnitude > 0.1f)
            {
                flightForward = moveDir.normalized;
            }
        }
        
        // 비행기 회전 (지구를 향하도록)
        if (up.magnitude > 0.1f && flightForward.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flightForward, up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    void OnDrawGizmos()
    {
        // 디버그용: 현재 고도 표시
        if (planetCenter != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(planetCenter.position, planetRadius);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(planetCenter.position, planetRadius + currentAltitude);
        }
    }
}
