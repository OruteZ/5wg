using System;
using System.Collections.Generic;
using Alchemy.Inspector;
using FiveWG.Core;
using UnityEngine;

namespace FiveWG.Combat
{
    /// <summary>
    /// 반경 안을 주기적으로 때리는 영역 하나의 수치.
    /// </summary>
    public struct DamageFieldData
    {
        public float Damage;
        public float Radius;

        /// <summary>몇 초 동안 살아 있는가. 0이면 한 번만 때리고 연출만 남긴다(충격파·폭발).</summary>
        public float Duration;

        /// <summary>몇 초마다 때리는가. 0 이하면 생성 시 1회만.</summary>
        public float TickInterval;

        public float Knockback;
        public Faction OwnerFaction;

        /// <summary>따라다닐 대상. null이면 생성 위치에 고정된다(장판). 오라는 여기에 플레이어가 들어온다.</summary>
        public Transform Follow;
    }

    /// <summary>
    /// 반경 안의 적을 주기적으로 때리는 영역.
    ///
    /// 즉발 범위(킥·크래시)와 지속 영역(베이스·신스)을 한 클래스로 덮는다. 셋의 차이는
    /// <see cref="DamageFieldData.Duration"/>과 <see cref="DamageFieldData.Follow"/>뿐이다 —
    /// 충격파는 지속 0, 장판은 유한 지속·고정, 오라는 긴 지속·플레이어 추종이다.
    /// 형태가 같은 것을 클래스로 나누면 같은 타격 루프를 네 번 쓰게 된다.
    ///
    /// 대상은 타격할 때마다 오버랩 쿼리로 찾는다. 트리거 콜라이더로 "지금 안에 있는 목록"을
    /// 유지하는 쪽이 싸 보이지만, 풀에서 꺼낸 순간 이미 겹쳐 있던 적에게는 Enter가 다음 물리
    /// 스텝에나 와서 지속 0인 충격파가 아무도 때리지 못한다. 목록을 미리 채워두는 보정을 붙이면
    /// 콜라이더·Enter·Exit·죽은 참조 정리가 전부 딸려온다.
    /// </summary>
    public sealed class DamageField : MonoBehaviour, IPooledObject<DamageField>
    {
        [Title("연출")]
        [SerializeField, LabelText("영역 스프라이트 (반경에 맞춰 크기가 조절된다)")]
        private SpriteRenderer _sprite;

        [SerializeField, LabelText("사라지며 페이드하는 시간 (sec)")]
        private float _fadeDuration = 0.25f;

        // ponytail: 타격마다 오버랩 쿼리 하나. 동시에 떠 있는 영역이 한 자릿수라 문제가 안 된다.
        // 화면에 수십 개가 깔리기 시작하면 TargetRegistry 순회로 바꾼다(조준이 이미 그렇게 한다).
        private static readonly List<Collider2D> Overlaps = new();

        private Action<DamageField> _release;
        private DamageFieldData _data;
        private ContactFilter2D _filter;
        private Color _baseColor;
        private float _spriteUnitDiameter = 1f;
        private float _elapsed;
        private float _lifetime;
        private float _nextTick;
        private bool _isSpent;

        private void Awake()
        {
            // 플레이어처럼 콜라이더가 트리거인 대상이 이미 있어서, useTriggers를 켜두지 않으면
            // 편이 반대인 영역이 아무도 못 때린다.
            _filter = ContactFilter2D.noFilter;
            _filter.useTriggers = true;

            if (_sprite == null) return;

            _baseColor = _sprite.color;

            // 스프라이트의 원래 지름을 재서 반경을 크기로 환산한다.
            // "1유닛짜리 원 스프라이트를 써야 한다"는 암묵적 약속을 두지 않으려고.
            float diameter = _sprite.sprite != null ? _sprite.sprite.bounds.size.x : 0f;
            if (diameter > 0.0001f) _spriteUnitDiameter = diameter;
        }

        /// <summary>풀 생성 시 1회만 호출한다.</summary>
        public void SetReleaseCallback(Action<DamageField> release) => _release = release;

        /// <summary>풀에서 꺼낼 때마다 호출한다. 재사용되므로 상태를 전부 되돌린다.</summary>
        public void Begin(Vector2 position, in DamageFieldData data)
        {
            _data = data;
            transform.position = position;

            if (_sprite != null)
            {
                _sprite.transform.localScale = Vector3.one * (data.Radius * 2f / _spriteUnitDiameter);
                _sprite.color = _baseColor;
            }

            // 지속 0이어도 연출은 보여야 하므로 페이드 시간만큼은 살려둔다.
            _lifetime = Mathf.Max(data.Duration, _fadeDuration);
            _elapsed = 0f;
            _nextTick = 0f;
            _isSpent = false;

            // 첫 타격은 생성 즉시다. 충격파가 한 프레임 뒤에 터지면 박자와 어긋난다.
            TickDamage();
        }

        private void Update()
        {
            if (_isSpent) return;

            if (_data.Follow != null) transform.position = _data.Follow.position;

            _elapsed += Time.deltaTime;

            if (_elapsed >= _nextTick && _nextTick <= _data.Duration) TickDamage();

            UpdateFade();

            if (_elapsed >= _lifetime) Despawn();
        }

        private void TickDamage()
        {
            // 간격이 0이면 다음 타격이 오지 않도록 수명 밖으로 밀어둔다(1회 타격).
            _nextTick = _data.TickInterval > 0f ? _nextTick + _data.TickInterval : float.MaxValue;

            if (_data.Damage <= 0f || _data.Radius <= 0f) return;

            int count = Physics2D.OverlapCircle(transform.position, _data.Radius, _filter, Overlaps);

            for (int i = 0; i < count; i++)
            {
                Collider2D hit = Overlaps[i];
                if (hit == null) continue;
                if (!hit.TryGetComponent(out IDamageable damageable)) continue;
                if (damageable.Faction == _data.OwnerFaction) continue;

                damageable.TakeDamage(_data.Damage);

                if (_data.Knockback <= 0f) continue;

                Vector2 away = (Vector2)hit.transform.position - (Vector2)transform.position;
                if (away.sqrMagnitude > 0.0001f)
                {
                    damageable.ApplyKnockback(away.normalized * _data.Knockback);
                }
            }
        }

        private void UpdateFade()
        {
            if (_sprite == null || _fadeDuration <= 0f) return;

            float remaining = _lifetime - _elapsed;
            if (remaining >= _fadeDuration) return;

            Color color = _baseColor;
            color.a = _baseColor.a * Mathf.Clamp01(remaining / _fadeDuration);
            _sprite.color = color;
        }

        private void Despawn()
        {
            if (_isSpent) return;
            _isSpent = true;

            _release?.Invoke(this);
        }
    }
}
