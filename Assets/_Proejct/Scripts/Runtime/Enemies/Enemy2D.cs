using System;
using Alchemy.Inspector;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class Enemy2D : MonoBehaviour, IDamageable
{
    [Title("스탯")]
    [SerializeField, LabelText("최대 체력")] private float _maxHealth = 30f;
    [SerializeField, LabelText("이동 속도 (units/sec)")] private float _moveSpeed = 2f;

    [Title("피격 연출")]
    [SerializeField, LabelText("피격 시 색")] private Color _hitColor = Color.white;
    [SerializeField, LabelText("피격 색 지속 (sec)")] private float _hitFlashDuration = 0.08f;

    [ShowInInspector, ReadOnly, LabelText("현재 체력")]
    private float CurrentHealth => _health;

    /// <summary>EnemyRegistry 전용. 목록에서의 자기 위치라 O(1) 해제가 가능하다. -1이면 미등록.</summary>
    public int RegistryIndex { get; set; } = -1;

    /// <summary>매번 transform 프로퍼티를 타지 않도록 캐시해둔 것. 타겟 탐색이 프레임마다 훑는다.</summary>
    public Transform Transform { get; private set; }

    private Rigidbody2D _rigidbody;
    private SpriteRenderer _spriteRenderer;
    private Action<Enemy2D> _release;
    private Transform _target;
    private Color _baseColor;
    private float _health;
    private float _hitFlashRemaining;
    private bool _isDead;

    private void Awake()
    {
        Transform = transform;
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.gravityScale = 0f;
        _rigidbody.freezeRotation = true;

        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer != null) _baseColor = _spriteRenderer.color;
    }

    // 풀이 SetActive로 꺼내고 넣으므로 등록·해제 시점이 활성 여부와 정확히 일치한다.
    private void OnEnable() => EnemyRegistry.Register(this);

    private void OnDisable() => EnemyRegistry.Unregister(this);

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

    private void Update()
    {
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
        _release?.Invoke(this);
    }
}
