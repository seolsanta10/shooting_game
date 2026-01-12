using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("HP 바 설정")]
    public GameObject healthBarPrefab; // HP 바 프리팹
    public Vector3 offset = new Vector3(0, 1f, 0); // 머리 위 오프셋
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    
    private GameObject healthBarInstance;
    private Image healthFill;
    private Image healthBackground;
    private Canvas worldCanvas;
    private Camera mainCamera;
    
    void Start()
    {
        currentHealth = maxHealth;
        mainCamera = Camera.main;
        
        // World Space Canvas 찾기 또는 생성
        CreateWorldCanvas();
        
        // HP 바 생성
        CreateHealthBar();
    }
    
    void LateUpdate()
    {
        // HP 바 위치 업데이트
        if (healthBarInstance != null)
        {
            // Ground 찾기
            GameObject ground = GameObject.Find("Ground");
            Transform groundCenter = null;
            if (ground != null)
            {
                groundCenter = ground.transform;
            }
            
            // 적 머리 위에 위치 (Ground 중심을 기준으로 위쪽)
            Vector3 worldPosition = transform.position;
            if (groundCenter != null)
            {
                // Ground 중심에서 적으로의 방향
                Vector3 directionFromGround = (transform.position - groundCenter.position).normalized;
                // Ground 표면에 수직인 방향으로 offset 적용
                worldPosition = transform.position + directionFromGround * offset.magnitude;
            }
            else
            {
                worldPosition = transform.position + offset;
            }
            healthBarInstance.transform.position = worldPosition;
            
            // 카메라를 향하도록 회전 (Billboard 효과 - Ground 중심 기준으로 올바른 방향)
            if (mainCamera != null)
            {
                Vector3 directionToCamera = healthBarInstance.transform.position - mainCamera.transform.position;
                
                if (groundCenter != null)
                {
                    // Ground 중심을 기준으로 한 "위" 방향
                    Vector3 directionFromGround = (transform.position - groundCenter.position).normalized;
                    Vector3 up = directionFromGround;
                    
                    // 카메라 방향을 Ground 표면에 투영
                    Vector3 cameraForward = Vector3.ProjectOnPlane(-directionToCamera, up).normalized;
                    
                    if (cameraForward.magnitude > 0.1f)
                    {
                        // Ground 표면에 접하는 방향으로 회전
                        healthBarInstance.transform.rotation = Quaternion.LookRotation(cameraForward, up);
                    }
                    else
                    {
                        // 카메라가 바로 위/아래에 있으면 기본 방향 사용
                        Vector3 forward = Vector3.ProjectOnPlane(mainCamera.transform.forward, up).normalized;
                        if (forward.magnitude < 0.1f)
                        {
                            forward = Vector3.ProjectOnPlane(Vector3.forward, up).normalized;
                        }
                        healthBarInstance.transform.rotation = Quaternion.LookRotation(forward, up);
                    }
                }
                else
                {
                    // Ground가 없으면 기본 방식 사용
                    if (directionToCamera.magnitude > 0.1f)
                    {
                        healthBarInstance.transform.rotation = Quaternion.LookRotation(-directionToCamera);
                    }
                    else
                    {
                        healthBarInstance.transform.rotation = Quaternion.LookRotation(-mainCamera.transform.forward, mainCamera.transform.up);
                    }
                }
            }
        }
        
        // HP가 0이면 HP 바 숨기기
        if (currentHealth <= 0f && healthBarInstance != null)
        {
            healthBarInstance.SetActive(false);
        }
    }
    
    void CreateWorldCanvas()
    {
        // World Space Canvas 찾기
        worldCanvas = FindObjectOfType<Canvas>();
        if (worldCanvas == null || worldCanvas.renderMode != RenderMode.WorldSpace)
        {
            // World Space Canvas 생성
            GameObject canvasObj = new GameObject("WorldCanvas");
            worldCanvas = canvasObj.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Canvas 크기 설정
            RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
            canvasRect.localScale = Vector3.one * 0.01f; // 작게 스케일링
        }
    }
    
    void CreateHealthBar()
    {
        if (healthBarPrefab != null)
        {
            // 프리팹 사용
            healthBarInstance = Instantiate(healthBarPrefab, worldCanvas.transform);
        }
        else
        {
            // 기본 HP 바 생성
            healthBarInstance = new GameObject("HealthBar");
            healthBarInstance.transform.SetParent(worldCanvas.transform, false);
            
            RectTransform barRect = healthBarInstance.AddComponent<RectTransform>();
            barRect.sizeDelta = new Vector2(100f, 10f);
            barRect.localScale = Vector3.one;
            
            // 배경
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(healthBarInstance.transform, false);
            
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            healthBackground = bgObj.AddComponent<Image>();
            healthBackground.color = new Color(0f, 0f, 0f, 0.8f); // 검은색 배경
            
            // HP 채우기
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(healthBarInstance.transform, false);
            
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            
            healthFill = fillObj.AddComponent<Image>();
            healthFill.color = Color.red; // 빨간색 HP 바
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
        }
        
        // HP 바 초기 위치 설정
        Vector3 worldPosition = transform.position + offset;
        healthBarInstance.transform.position = worldPosition;
    }
    
    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        UpdateHealthBar();
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);
        UpdateHealthBar();
        
        // HP가 0이 되면 적 파괴
        if (currentHealth <= 0f)
        {
            // 처치 카운트 증가
            KillCounter killCounter = FindObjectOfType<KillCounter>();
            if (killCounter != null)
            {
                killCounter.AddKill();
            }
            
            // 적 파괴
            Destroy(gameObject);
        }
    }
    
    void UpdateHealthBar()
    {
        if (healthFill != null)
        {
            float fillAmount = currentHealth / maxHealth;
            healthFill.fillAmount = fillAmount;
            
            // 색상 변경
            if (fillAmount > 0.6f)
            {
                healthFill.color = Color.green;
            }
            else if (fillAmount > 0.3f)
            {
                healthFill.color = Color.yellow;
            }
            else
            {
                healthFill.color = Color.red;
            }
        }
    }
    
    void OnDestroy()
    {
        // 적이 파괴되면 HP 바도 제거
        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance);
        }
    }
}
