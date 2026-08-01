using System;
using Alchemy.Inspector;
using UnityEngine;

/// <summary>
/// 직선으로 날아가는 투사체. 풀에서 재사용되므로 스스로 Destroy 하지 않고 반납 콜백을 호출한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class Projectile2D : MonoBehaviour
{
    [Title("탄 설정")]
    [SerializeField, LabelText("속도 (units/sec)")] private float _speed = 14f;
    [SerializeField, LabelText("수명 (sec)")] private float _lifetime = 2f;
    [SerializeField, LabelText("데미지")] private float _damage = 10f;

    private Rigidbody2D _rigidbody;
    private Action<Projectile2D> _release;
    private float _remainingLifetime;
    private bool _isSpent;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;
    }

    /// <summary>풀 생성 시 1회만 호출한다. 매 발사마다 델리게이트를 새로 만들지 않기 위함.</summary>
    public void SetReleaseCallback(Action<Projectile2D> release) => _release = release;

    /// <summary>지정 위치에서 지정 방향으로 발사한다.</summary>
    public void Launch(Vector2 position, Vector2 direction)
    {
        Vector2 normalized = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        transform.SetPositionAndRotation(
            position,
            Quaternion.Euler(0f, 0f, Mathf.Atan2(normalized.y, normalized.x) * Mathf.Rad2Deg));

        _rigidbody.linearVelocity = normalized * _speed;
        _remainingLifetime = _lifetime;
        _isSpent = false;
    }

    private void Update()
    {
        // 수명 카운트다운. 단순 타이머라 Awaitable/코루틴보다 할당·취소 처리가 없어 풀링과 잘 맞는다.
        _remainingLifetime -= Time.deltaTime;
        if (_remainingLifetime <= 0f)
        {
            Despawn();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isSpent) return;
        if (!other.TryGetComponent(out IDamageable damageable)) return;

        damageable.TakeDamage(_damage);
        Despawn();
    }

    private void Despawn()
    {
        // 같은 프레임에 수명 만료와 충돌이 겹쳐도 두 번 반납되지 않게 막는다.
        if (_isSpent) return;
        _isSpent = true;

        _rigidbody.linearVelocity = Vector2.zero;
        _release?.Invoke(this);
    }
}
