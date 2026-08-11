using System;
using Alchemy.Inspector;
using FiveWG.Core;
using UnityEngine;

namespace FiveWG.Combat
{
    /// <summary>
    /// 발사 한 발의 수치. 인자를 늘리는 대신 묶었다 — 관통·유도·크기가 붙으면서
    /// <see cref="Projectile.Launch"/>의 파라미터가 아홉 개가 되기 때문이다.
    ///
    /// 값의 출처는 무기의 레벨 표 하나다. 프리팹에 같은 값을 두지 않는다.
    /// </summary>
    public struct ProjectileSpawnData
    {
        public float Damage;
        public float Speed;

        /// <summary>추가로 뚫는 대상 수. 0이면 하나 맞히고 사라진다.</summary>
        public int Pierce;

        /// <summary>초당 몇 도까지 방향을 틀 수 있는가. 0이면 직선.</summary>
        public float TurnRateDegrees;

        /// <summary>유도 대상. 발사 시점에 무기가 골라 넣는다. 사라지면 그대로 직진한다.</summary>
        public Transform HomingTarget;

        public float Scale;
        public float Knockback;

        /// <summary>발사 주체. 이 계층에 속한 대상은 통과시킨다.</summary>
        public Transform Owner;

        public Faction OwnerFaction;
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class Projectile : MonoBehaviour, IPooledObject<Projectile>
    {
        [Title("탄 설정")]
        [SerializeField, LabelText("수명 (sec)")] private float _lifetime = 2f;

        private Rigidbody2D _rigidbody;
        private ProjectileSpawnData _data;
        private Transform _ownerRoot;
        private Vector3 _baseScale;
        private Action<Projectile> _release;
        private float _remainingLifetime;
        private int _remainingHits;
        private bool _isSpent;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.gravityScale = 0f;
            _rigidbody.freezeRotation = true;

            // 크기 배수는 프리팹의 원래 크기에 곱한다. 재사용되므로 원본을 여기서 한 번만 기억한다.
            _baseScale = transform.localScale;
        }

        /// <summary>풀 생성 시 1회만 호출한다.</summary>
        public void SetReleaseCallback(Action<Projectile> release) => _release = release;

        /// <summary>
        /// 아군 배제는 두 겹이다. <see cref="ProjectileSpawnData.OwnerFaction"/>이 같은 편을 걸러내고,
        /// owner 계층은 그와 별개로 통과시킨다. 발사구가 주체의 콜라이더 안에 있어서
        /// 스폰 직후 자기 자신을 때리는 사고를 편 설정과 무관하게 막기 위함이다.
        /// </summary>
        public void Launch(Vector2 position, Vector2 direction, in ProjectileSpawnData data)
        {
            _data = data;
            _ownerRoot = data.Owner != null ? data.Owner.root : null;
            _remainingHits = Mathf.Max(0, data.Pierce) + 1;

            Vector2 normalized = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

            transform.SetPositionAndRotation(position, RotationFor(normalized));
            transform.localScale = _baseScale * (data.Scale > 0f ? data.Scale : 1f);

            _rigidbody.linearVelocity = normalized * data.Speed;
            _remainingLifetime = _lifetime;
            _isSpent = false;
        }

        private void Update()
        {
            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f) Despawn();
        }

        private void FixedUpdate()
        {
            if (_isSpent || _data.TurnRateDegrees <= 0f || _data.HomingTarget == null) return;

            // 속도 방향만 돌린다. 속력을 유지해야 사거리·수명 계산이 그대로 성립한다.
            Vector2 current = _rigidbody.linearVelocity;
            float speed = current.magnitude;
            if (speed <= 0.0001f) return;

            Vector2 desired = (Vector2)_data.HomingTarget.position - _rigidbody.position;
            if (desired.sqrMagnitude <= 0.0001f) return;

            Vector2 steered = Vector3.RotateTowards(
                current / speed,
                desired.normalized,
                _data.TurnRateDegrees * Mathf.Deg2Rad * Time.fixedDeltaTime,
                0f);

            _rigidbody.linearVelocity = steered * speed;
            transform.rotation = RotationFor(steered);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isSpent) return;

            // 발사 주체는 통과시킨다. 스폰 위치가 주체의 콜라이더 안이라 이걸 빼면 즉시 자해하고 사라진다.
            if (_ownerRoot != null && other.transform.root == _ownerRoot) return;

            if (!other.TryGetComponent(out IDamageable damageable)) return;
            if (damageable.Faction == _data.OwnerFaction) return;

            damageable.TakeDamage(_data.Damage);

            if (_data.Knockback > 0f)
            {
                Vector2 away = (Vector2)other.transform.position - _rigidbody.position;
                damageable.ApplyKnockback(away.normalized * _data.Knockback);
            }

            // 트리거는 콜라이더당 한 번만 들어오므로 뚫고 지나간 대상을 다시 때리지 않는다.
            _remainingHits--;
            if (_remainingHits <= 0) Despawn();
        }

        private static Quaternion RotationFor(Vector2 direction) =>
            Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        private void Despawn()
        {
            // 같은 프레임에 수명 만료와 피격이 겹쳐도 두 번 반납되지 않게 막는다.
            if (_isSpent) return;
            _isSpent = true;

            _rigidbody.linearVelocity = Vector2.zero;
            _release?.Invoke(this);
        }
    }
}
