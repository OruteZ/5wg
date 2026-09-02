using System;
using Alchemy.Inspector;
using FiveWG.Core;
using UnityEngine;

namespace FiveWG.Enemies
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class Enemy : MonoBehaviour, IDamageable, IPooledObject<Enemy>
    {
        // 디렉터가 EnemyStats를 넘기면 전부 덮인다. 구 EnemySpawner 경로에서만 쓰인다.
        [Title("스탯 (정의 없이 스폰될 때의 기본값)")]
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

        // 넉백 세기는 무기 스탯이 정한다. 여기 있는 건 "얼마나 오래 밀려나는가"뿐이다.
        [SerializeField, LabelText("넉백 지속 (sec)")] private float _knockbackDuration = 0.15f;

        [Title("박 펄스")]
        [SerializeField, LabelText("박마다 커지는 비율 (0이면 끔)"), Min(0f)]
        [Tooltip("이동은 연속이고 박은 크기로만 표현한다. 위치를 박에 스냅시키지 않는다.")]
        private float _beatPulseScale = 0.18f;

        [ShowInInspector, ReadOnly, LabelText("현재 체력")]
        private float CurrentHealth => _health;

        private Rigidbody2D _rigidbody;
        private SpriteRenderer _spriteRenderer;
        private Action<Enemy> _release;
        private Transform _target;
        private Color _prefabColor;
        private Color _baseColor;
        private Vector3 _prefabScale;
        private Vector3 _baseScale;
        private EnemyStats _stats;
        private float _health;
        private float _hitFlashRemaining;
        private float _contactCooldown;
        private Vector2 _knockbackVelocity;
        private float _knockbackRemaining;
        private bool _isDead;

        /// <summary>디스폰 판정이 매 프레임 수백 번 읽으므로 Transform을 거치지 않는다.</summary>
        public Vector2 Position => _rigidbody.position;

        public bool IsElite => _stats.IsElite;
        public bool ImmuneToDespawn => _stats.ImmuneToDespawn;

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
            if (_spriteRenderer != null) _prefabColor = _spriteRenderer.color;
            _baseColor = _prefabColor;

            _prefabScale = transform.localScale;
            _baseScale = _prefabScale;

            // 씬에 손으로 놓은 적은 Spawn을 거치지 않아 스탯이 0이 된다.
            _stats = new EnemyStats(
                _maxHealth, _moveSpeed, _contactDamage, _expReward, 1f, isElite: false, _prefabColor);
            _health = _maxHealth;
        }

        public Faction Faction => Faction.Enemy;

        // 풀이 개체를 껐다 켜는 것만으로 조준 대상 목록이 맞춰진다. 반납 시점을 따로 챙기지 않아도 된다.
        private void OnEnable() => TargetRegistry.Register(this, Faction);

        private void OnDisable() => TargetRegistry.Unregister(this, Faction);

        /// <summary>풀 생성 시 1회만 호출한다.</summary>
        public void SetReleaseCallback(Action<Enemy> release) => _release = release;

        /// <summary>인스펙터 기본 스탯으로 스폰한다. 기획 표를 쓰는 쪽은 아래 오버로드를 쓴다.</summary>
        public void Spawn(Vector2 position, Transform target) => Spawn(
            position, target,
            new EnemyStats(_maxHealth, _moveSpeed, _contactDamage, _expReward, 1f, isElite: false, _prefabColor));

        /// <summary>풀에서 꺼낼 때마다 호출한다. 수치는 스폰 디렉터가 계산해 넘긴다.</summary>
        public void Spawn(Vector2 position, Transform target, in EnemyStats stats)
        {
            _stats = stats;
            _target = target;

            transform.position = position;
            _baseScale = _prefabScale * stats.SizeMultiplier;
            transform.localScale = _baseScale;

            _health = stats.MaxHealth;
            _isDead = false;
            _hitFlashRemaining = 0f;
            _contactCooldown = 0f;
            _knockbackRemaining = 0f;
            _knockbackVelocity = Vector2.zero;

            _baseColor = stats.Color;
            if (_spriteRenderer != null) _spriteRenderer.color = _baseColor;
        }

        /// <summary>
        /// 멀어진 적을 반대편으로 옮긴다. 풀에 반납했다 바로 꺼낼 이유가 없어 같은 개체를 되돌린다.
        /// 사망 경로를 타지 않으므로 보상도 나오지 않는다.
        /// </summary>
        public void RecycleTo(Vector2 position) => Spawn(position, _target, _stats);

        /// <summary>
        /// 박에 맞춰 몸체를 부풀린다. <paramref name="pulse01"/>은 정박 직후 1, 다음 박 직전 0이다.
        /// 색이 아니라 크기를 쓰는 것은 피격 색과 겹치면 맞은 것이 안 보이기 때문이다.
        /// </summary>
        public void ApplyBeatPulse(float pulse01)
        {
            if (_beatPulseScale <= 0f) return;

            transform.localScale = _baseScale * (1f + _beatPulseScale * pulse01);
        }

        public void TakeDamage(float amount)
        {
            if (_isDead) return;

            _health -= amount;
            _hitFlashRemaining = _hitFlashDuration;
            if (_spriteRenderer != null) _spriteRenderer.color = _hitColor;

            if (_health <= 0f) Die();
        }

        /// <summary>
        /// FixedUpdate가 추격 속도를 매 프레임 통째로 덮어쓰므로 AddForce로는 한 프레임도 밀리지 않는다.
        /// 그래서 힘이 아니라 "추격을 잠깐 밀어내기로 갈아끼우는" 방식으로 넣는다.
        /// </summary>
        public void ApplyKnockback(Vector2 impulse)
        {
            if (_isDead || _stats.ImmuneToKnockback) return;
            if (impulse.sqrMagnitude <= 0.0001f || _knockbackDuration <= 0f) return;

            _knockbackVelocity = impulse;
            _knockbackRemaining = _knockbackDuration;
        }

        private void OnTriggerEnter2D(Collider2D other) => TryContactDamage(other);

        // 겹친 채 머무르는 동안에도 재타격 간격마다 들어가야 하므로 Stay도 함께 받는다.
        private void OnTriggerStay2D(Collider2D other) => TryContactDamage(other);

        /// <summary>
        /// 상대의 구체 타입은 보지 않는다. 같은 편과 IDamageable이 아닌 것(투사체 등)은 걸러진다.
        /// </summary>
        private void TryContactDamage(Collider2D other)
        {
            if (_isDead || _contactCooldown > 0f || _stats.ContactDamage <= 0f) return;
            if (!other.TryGetComponent(out IDamageable damageable)) return;
            if (damageable.Faction == Faction) return;

            damageable.TakeDamage(_stats.ContactDamage);
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
            if (_knockbackRemaining > 0f)
            {
                _knockbackRemaining -= Time.fixedDeltaTime;
                _rigidbody.linearVelocity = _knockbackVelocity;
                return;
            }

            if (_isDead || _target == null)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 toTarget = (Vector2)_target.position - _rigidbody.position;
            _rigidbody.linearVelocity = toTarget.normalized * _stats.MoveSpeed;
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            _rigidbody.linearVelocity = Vector2.zero;

            // 반납보다 먼저 알린다. 반납 후에는 위치가 다음 스폰으로 덮일 수 있다.
            OnDiedWithReward?.Invoke(_rigidbody.position, _stats.NoteReward);

            _release?.Invoke(this);
        }
    }
}
