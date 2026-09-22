using System.Collections.Generic;
using FiveWG.Combat;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 가장 가까운 적에서 시작해 연결 거리 안의 다음 적을 최대 연결 수까지 물고 틱으로 때리는 무기.
    ///
    /// 관통 수(Pierce) 축을 최대 연결 수로 재사용한다 — 사슬 무기에게 관통은 뜻이 없고,
    /// 새 축을 만드는 대신 15축 중 안 쓰는 자리를 이 무기가 쓰는 뜻으로 채우는 게 기존 방침이다.
    /// </summary>
    public sealed class ChainWeapon : WeaponBase
    {
        private readonly ChainWeaponDefinition _definition;
        private readonly List<Transform> _links = new();
        private readonly List<Transform> _candidates = new();

        public ChainWeapon(ChainWeaponDefinition definition, IFireTiming timing)
            : base(definition, timing)
        {
            _definition = definition;
        }

        protected override void OnFire(in FireContext context)
        {
            if (_definition.FieldPrefab == null || Context.projectiles == null || Context.targets is null) return;

            WeaponLevelData stats = Stats;
            int maxLinks = Mathf.Max(1, stats.Pierce);

            ResolveChain(context.origin, maxLinks);
            if (_links.Count == 0) return;

            ChainField field = Context.projectiles.Get(_definition.FieldPrefab);
            if (field == null) return;

            field.Begin(_links, new ChainFieldData
            {
                TickDamage = stats.Damage,
                TickInterval = _definition.TickInterval,
                Duration = _definition.ActiveDuration,
                OwnerFaction = Context.Faction,
            });
        }

        /// <summary>
        /// 첫 연결은 사거리 안 최근접, 이후는 직전 링크에서 연결 거리 안의 최근접을 반복해서 찾는다.
        /// 이미 링크된 대상은 다시 고르지 않는다.
        /// </summary>
        private void ResolveChain(Vector2 origin, int maxLinks)
        {
            _links.Clear();

            if (!Context.targets.TryGetNearest(origin, _definition.FirstLinkRange, out Transform current)) return;
            _links.Add(current);

            Vector2 from = current.position;

            while (_links.Count < maxLinks)
            {
                int count = Context.targets.GetInRange(from, _definition.LinkDistance, _candidates);
                Transform next = null;
                float bestSqr = float.MaxValue;

                for (int i = 0; i < count; i++)
                {
                    Transform candidate = _candidates[i];
                    if (_links.Contains(candidate)) continue;

                    float sqr = ((Vector2)candidate.position - from).sqrMagnitude;
                    if (sqr >= bestSqr) continue;

                    bestSqr = sqr;
                    next = candidate;
                }

                if (next == null) break;

                _links.Add(next);
                from = next.position;
            }
        }
    }
}
