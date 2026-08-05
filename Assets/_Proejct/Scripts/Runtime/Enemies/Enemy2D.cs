using System;
using Alchemy.Inspector;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class Enemy2D : MonoBehaviour, IDamageable
{
    [Title("스탯")]
    [SerializeField, LabelText("최대 체력")] private float _maxHealth = 30f;
    [SerializeField, LabelText("이동 속도 (units/sec)")] private float _moveSpeed = 2f;

    [Title("보상")]
    [SerializeField, LabelText("드랍 경험치")] private float _expReward = 1f;

    [Title("접촉 공격")]
    [SerializeField, LabelText("접촉 데미지")] private float _contactDamage = 10f;

    // 플레이어 콜라이더가 트리거라 적이 겹친 채 머무르면 Enter가 다시 오지 않는다.
    // 붙어 있는 동안에도 주기적으로 들어가도록 재타격 간격을 둔다.
    [SerializeField, LabelText("재타격 간격 (sec)")] private float _contactInterval = 0.8f;

    [Title("피격 연출")]
    [SerializeField, LabelText("피격 시 색")] private Color _hitColor = Color.white;
    [SerializeField, LabelText("피격 색 지속 (sec)")] private float _hitFlashDuration = 0.08f;

    [ShowInInspector, ReadOnly, LabelText("현재 체력")]
    private float CurrentHealth => _health;

    private Rigidbody2D _rigidbody;
    private SpriteRenderer _spriteRenderer;
    private Action<Enemy2D> _release;
    private Transform _target;
    private Color _baseColor;
    private float _health;
    private float _hitFlashRemaining;
    private float _contactCooldown;
    private bool _isDead;

    /// <summary>
    /// 사망으로 죽었을 때만 발행된다. 인자는 (사망 위치, 드랍 경험치).
    /// 스테이지 종료 시의 일괄 회수는 여기로 오지 않는다 — 그때는 보상이 없어야 하기 때문.
    /// </summary>
    public event Action<Vector2, float> OnDiedWithReward;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;

        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer != null) _baseColor = _spriteRenderer.color;
    }

    /// <summary>풀 생성 시 1회만 호출한다.</summary>
    public void SetReleaseCallback(Action<Enemy2D> release) => _release = release;

    /// <summary>풀에서 꺼낼 때마다 호출한다. 재사용되므로 상태를 전부 되돌린다.</summary>
    public void Spawn(Vector2 position, Transform target)
    {
        transform.position = position;
        _target = target;
        _health = _maxHealth;
        _isDead = false;
        _hitFlashRemaining = 0f;
        _contactCooldown = 0f;

        if (_spriteRenderer != null) _spriteRenderer.color = _baseColor;
    }

    public void TakeDamage(float amount)
    {
        if (_isDead) return;

        _health -= amount;
        _hitFlashRemaining = _hitFlashDuration;
        if (_spriteRenderer != null) _spriteRenderer.color = _hitColor;

        if (_health <= 0f) Die();
    }

    private void OnTriggerEnter2D(Collider2D other) => TryContactDamage(other);

    // 겹친 채 머무르는 동안에도 재타격 간격마다 들어가야 하므로 Stay도 함께 받는다.
    private void OnTriggerStay2D(Collider2D other) => TryContactDamage(other);

    /// <summary>
    /// 상대의 구체 타입은 보지 않는다. 적끼리는 둘 다 트리거가 아니라 여기로 오지 않고,
    /// 투사체는 IDamageable이 아니라 걸러진다.
    /// </summary>
    private void TryContactDamage(Collider2D other)
    {
        if (_isDead || _contactCooldown > 0f || _contactDamage <= 0f) return;
        if (!other.TryGetComponent(out IDamageable damageable)) return;

        damageable.TakeDamage(_contactDamage);
        _contactCooldown = _contactInterval;
    }

    private void Update()
    {
        if (_contactCooldown > 0f) _contactCooldown -= Time.deltaTime;

        if (_hitFlashRemaining <= 0f) return;

        _hitFlashRemaining -= Time.deltaTime;
        if (_hitFlashRemaining <= 0f && _spriteRenderer != null)
        {
            _spriteRenderer.color = _baseColor;
        }
    }

    private void FixedUpdate()
    {
        if (_isDead || _target == null)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = (Vector2)_target.position - _rigidbody.position;
        _rigidbody.linearVelocity = toTarget.normalized * _moveSpeed;
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        _rigidbody.linearVelocity = Vector2.zero;

        // 반납보다 먼저 알린다. 반납 후에는 위치가 다음 스폰으로 덮일 수 있다.
        OnDiedWithReward?.Invoke(_rigidbody.position, _expReward);

        _release?.Invoke(this);
    }
}
