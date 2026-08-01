using System;
using Alchemy.Inspector;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class Projectile2D : MonoBehaviour
{
    [Title("탄 설정")]
    [SerializeField, LabelText("속도 (units/sec)")] private float _speed = 14f;
    [SerializeField, LabelText("수명 (sec)")] private float _lifetime = 2f;
    [SerializeField, LabelText("기본 데미지 (무기가 덮어쓸 수 있음)")] private float _damage = 10f;

    private Rigidbody2D _rigidbody;
    private float _currentDamage;
    private Transform _ownerRoot;
    private Action<Projectile2D> _release;
    private float _remainingLifetime;
    private bool _isSpent;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;
    }

    /// <summary>풀 생성 시 1회만 호출한다.</summary>
    public void SetReleaseCallback(Action<Projectile2D> release) => _release = release;

    /// <summary>프리팹에 설정된 기본 데미지로 발사한다.</summary>
    public void Launch(Vector2 position, Vector2 direction) => Launch(position, direction, _damage, null);

    /// <summary>
    /// 데미지와 발사 주체를 지정해 발사한다.
    /// owner를 넘기면 그 계층에 속한 대상은 맞히지 않는다. 발사구가 주체의 콜라이더 안에 있으면
    /// 스폰 직후 자기 자신을 때리고 사라지므로 자동공격에서는 반드시 넘겨야 한다.
    /// </summary>
    public void Launch(Vector2 position, Vector2 direction, float damage, Transform owner = null)
    {
        _currentDamage = damage;
        _ownerRoot = owner != null ? owner.root : null;

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
        _remainingLifetime -= Time.deltaTime;
        if (_remainingLifetime <= 0f)
        {
            Despawn();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isSpent) return;

        // 발사 주체는 통과시킨다. 스폰 위치가 주체의 콜라이더 안이라 이걸 빼면 즉시 자해하고 사라진다.
        if (_ownerRoot != null && other.transform.root == _ownerRoot) return;

        if (!other.TryGetComponent(out IDamageable damageable)) return;

        damageable.TakeDamage(_currentDamage);
        Despawn();
    }

    private void Despawn()
    {
        // 같은 프레임에 수명 만료와 피격이 겹쳐도 두 번 반납되지 않게 막는다.
        if (_isSpent) return;
        _isSpent = true;

        _rigidbody.linearVelocity = Vector2.zero;
        _release?.Invoke(this);
    }
}
