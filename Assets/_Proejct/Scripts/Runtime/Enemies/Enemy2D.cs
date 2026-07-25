using System;
using Alchemy.Inspector;
using UnityEngine;

namespace FiveWG.Prototype
{
    /// <summary>
    /// 플레이어를 향해 직진하는 적. 투사체에 맞으면 체력이 깎이고 0 이하가 되면 스포너 풀로 반납된다.
    /// </summary>
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
            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.gravityScale = 0f;
            _rigidbody.freezeRotation = true;

            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer != null) _baseColor = _spriteRenderer.color;
        }

        /// <summary>풀 생성 시 1회만 호출한다.</summary>
        public void SetReleaseCallback(Action<Enemy2D> release) => _release = release;

        /// <summary>스폰될 때마다 호출. 체력과 상태를 초기화한다.</summary>
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
}
