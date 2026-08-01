using Alchemy.Inspector;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerMovement : MonoBehaviour
{
    [Title("이동")]
    [SerializeField, LabelText("이동 속도 (units/sec)")]
    private float _moveSpeed = 6f;

    private Rigidbody2D _rigidbody;
    private Vector2 _moveInput;

    public Vector2 MoveInput => _moveInput;

    /// <summary>마지막으로 0이 아니었던 이동 방향. 조준 입력이 없을 때의 폴백으로 쓴다.</summary>
    public Vector2 FacingDirection { get; private set; } = Vector2.right;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();

        // 탑다운이라 중력과 물리 회전을 쓰지 않는다.
        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;
    }

    /// <summary>PlayerController가 매 프레임 전달하는 이동 입력을 받는다.</summary>
    public void SetMoveInput(Vector2 input)
    {
        _moveInput = input;

        if (input.sqrMagnitude > 0.0001f)
        {
            FacingDirection = input.normalized;
        }
    }

    private void FixedUpdate()
    {
        // 대각 입력이 빨라지지 않도록 길이가 1을 넘을 때만 정규화한다.
        Vector2 direction = _moveInput.sqrMagnitude > 1f ? _moveInput.normalized : _moveInput;
        _rigidbody.linearVelocity = direction * _moveSpeed;
    }
}
