using UnityEngine;
using UnityEngine.InputSystem;

public class CubeController : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;
    
    private Vector2 moveInput;
    
    void Update()
    {
        // 새로운 Input System 사용
        Keyboard keyboard = Keyboard.current;
        
        if (keyboard == null) return;
        
        // WASD 입력 처리
        float horizontal = 0f;
        float vertical = 0f;
        
        if (keyboard.wKey.isPressed) vertical = 1f;
        if (keyboard.sKey.isPressed) vertical = -1f;
        if (keyboard.aKey.isPressed) horizontal = -1f;
        if (keyboard.dKey.isPressed) horizontal = 1f;
        
        // 이동 방향 계산
        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
        
        // 이동 적용
        if (moveDirection.magnitude > 0.1f)
        {
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}
