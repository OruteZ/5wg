using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 탑다운 2D 플레이어 이동. New Input System의 Move 액션(Vector2)을 읽어 Rigidbody2D 속도로 반영한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerMovement2D : MonoBehaviour
{
    [Title("입력")]
    [SerializeField, Required("Player 액션맵을 가진 InputActionAsset이 필요하다.")]
    private InputActionAsset _inputActions;

    [SerializeField, LabelText("Move 액션 경로")]
    private string _moveActionPath = "Player/Move";

    [Title("이동")]
    [SerializeField, LabelText("이동 속도 (units/sec)")]
    private float _moveSpeed = 6f;

    private Rigidbody2D _rigidbody;
    private InputAction _moveAction;
    private Vector2 _moveInput;

    /// <summary>마지막으로 읽은 이동 입력. 다른 시스템이 조준 기본값 등으로 참조할 수 있다.</summary>
    public Vector2 MoveInput => _moveInput;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();

        // 탑다운이므로 중력과 회전은 물리에 맡기지 않는다.
        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;

        // throwIfNotFound: false → 에셋이 비어 있어도 예외 대신 null. 인스펙터 실수를 로그로 잡는다.
        _moveAction = _inputActions == null
            ? null
            : _inputActions.FindAction(_moveActionPath, throwIfNotFound: false);

        if (_moveAction is null)
        {
            Debug.LogError($"[{nameof(PlayerMovement2D)}] '{_moveActionPath}' 액션을 찾지 못했다.", this);
        }
    }

    // InputAction은 에셋이 소유하므로 Dispose가 아니라 Enable/Disable로 수명만 맞춘다.
    private void OnEnable() => _moveAction?.Enable();

    private void OnDisable() => _moveAction?.Disable();

    private void Update()
    {
        _moveInput = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
    }

    private void FixedUpdate()
    {
        // 대각 이동이 빨라지지 않도록 정규화. 입력이 0이면 그대로 정지.
        Vector2 direction = _moveInput.sqrMagnitude > 1f ? _moveInput.normalized : _moveInput;
        _rigidbody.linearVelocity = direction * _moveSpeed;
    }
}
