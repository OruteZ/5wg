using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.InputSystem;

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

    public Vector2 MoveInput => _moveInput;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();

        // 탑다운이라 중력과 물리 회전을 쓰지 않는다.
        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;

        // throwIfNotFound: false → 인스펙터 연결 실수를 예외 대신 로그로 잡는다.
        _moveAction = _inputActions == null
            ? null
            : _inputActions.FindAction(_moveActionPath, throwIfNotFound: false);

        if (_moveAction is null)
        {
            Debug.LogError($"[{nameof(PlayerMovement2D)}] '{_moveActionPath}' 액션을 찾지 못했다.", this);
        }
    }

    // InputAction의 소유자는 에셋이므로 Dispose가 아니라 Enable/Disable로 수명만 맞춘다.
    private void OnEnable() => _moveAction?.Enable();

    private void OnDisable() => _moveAction?.Disable();

    private void Update()
    {
        _moveInput = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
    }

    private void FixedUpdate()
    {
        // 대각 입력이 빨라지지 않도록 길이가 1을 넘을 때만 정규화한다.
        Vector2 direction = _moveInput.sqrMagnitude > 1f ? _moveInput.normalized : _moveInput;
        _rigidbody.linearVelocity = direction * _moveSpeed;
    }
}
