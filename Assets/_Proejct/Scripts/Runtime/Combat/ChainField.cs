using System;
using System.Collections.Generic;
using FiveWG.Core;
using UnityEngine;

namespace FiveWG.Combat
{
    /// <summary>
    /// 연결된 대상 전원에게 일정 간격으로 틱 데미지를 주는 사슬 하나의 수치. 신스(체인 빔)가 쓴다.
    /// </summary>
    public struct ChainFieldData
    {
        public float TickDamage;
        public float TickInterval;
        public float Duration;
        public Faction OwnerFaction;
    }

    /// <summary>
    /// 사거리 쿼리가 아니라 무기가 골라준 대상 목록을 그대로 물고 있다가 틱마다 때리는 영역.
    /// <see cref="DamageField"/>와 갈라 만든 이유는 판정 방식 자체가 다르기 때문이다 —
    /// 저건 "지금 반경 안에 누가 있나"를 매 틱 다시 묻고, 이건 "발동 순간 골라둔 대상들"을 고정해서 문다.
    /// 링크된 대상이 죽어 비활성화되면(풀에 반납되면) 그 이후로만 조용히 빠진다.
    /// </summary>
    public sealed class ChainField : MonoBehaviour, IPooledObject<ChainField>
    {
        private readonly List<Transform> _links = new();

        private Action<ChainField> _release;
        private ChainFieldData _data;
        private float _elapsed;
        private float _nextTick;
        private bool _isSpent;

        public void SetReleaseCallback(Action<ChainField> release) => _release = release;

        /// <summary>풀에서 꺼낼 때마다 호출한다. 링크 목록은 무기가 발동 순간에 한 번 골라 넘긴다.</summary>
        public void Begin(IReadOnlyList<Transform> targets, in ChainFieldData data)
        {
            _data = data;
            _links.Clear();
            for (int i = 0; i < targets.Count; i++) _links.Add(targets[i]);

            _elapsed = 0f;
            _nextTick = 0f;
            _isSpent = false;

            TickDamage();
        }

        private void Update()
        {
            if (_isSpent) return;

            _elapsed += Time.deltaTime;

            if (_elapsed >= _nextTick && _data.TickInterval > 0f) TickDamage();
            if (_elapsed >= _data.Duration) Despawn();
        }

        private void TickDamage()
        {
            _nextTick += _data.TickInterval > 0f ? _data.TickInterval : float.MaxValue;

            for (int i = 0; i < _links.Count; i++)
            {
                Transform link = _links[i];

                // 죽어서 풀에 반납된 대상은 비활성화만 되고 파괴되지는 않는다 — 활성 여부로 거른다.
                if (link == null || !link.gameObject.activeInHierarchy) continue;
                if (!link.TryGetComponent(out IDamageable damageable)) continue;
                if (damageable.Faction == _data.OwnerFaction) continue;

                damageable.TakeDamage(_data.TickDamage);
            }
        }

        private void Despawn()
        {
            if (_isSpent) return;
            _isSpent = true;

            _links.Clear();
            _release?.Invoke(this);
        }
    }
}
