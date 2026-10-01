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
    /// 다만 지금은 레벨 표 자체를 1(단일 타겟)에서 시작해 레벨마다 늘어나게 잡아뒀다(→ Synth.asset) —
    /// 여러 마리를 잇는 체이닝은 초반부터 주는 대신 성장으로 여는 게 낫다는 판단.
    /// </summary>
    public sealed class ChainWeapon : WeaponBase
    {
        private readonly ChainWeaponDefinition _definition;
        private readonly List<Transform> _links = new();
        private readonly List<Transform> _candidates = new();

        private ChainField _activeField;

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

            // 재발동 시 옛 사슬을 먼저 끊는다. 안 그러면 옛 사슬이 자기 수명이 남은 동안 계속
            // 옛 타겟을 때려서, 가장 가까운 적이 바뀌어도 공격이 안 옮겨가는 것처럼 보인다.
            _activeField?.Stop();

            ChainField field = Context.projectiles.Get(_definition.FieldPrefab);
            if (field == null) return;

            field.Begin(_links, new ChainFieldData
            {
                TickDamage = stats.Damage,
                TickInterval = _definition.TickInterval,
                Duration = _definition.ActiveDuration,
                OwnerFaction = Context.Faction,
                Origin = Context.owner,
            });

            _activeField = field;
        }

        protected override void OnUnequipped()
        {
            _activeField?.Stop();
            _activeField = null;
        }

        /// <summary>
        /// 원래 재연결은 트리거 칸(기획서 기준 반 마디~한 마디 간격)에서만 일어나는데, 그 정도
        /// 간격으로는 움직이는 적을 쫓기엔 눈에 띄게 느리다. 사슬이 떠 있는 동안은 매 프레임
        /// 대상을 다시 골라 <see cref="ChainField.Retarget"/>로 밀어 넣는다 — 트리거 칸은
        /// "언제 새로 거는가/얼마나 자주 다시 거는가"만 정하고, 실제 추적은 여기서 한다.
        /// </summary>
        public override void Tick(float deltaTime)
        {
            if (_activeField == null || Context.targets is null) return;

            ResolveChain(Context.owner.position, Mathf.Max(1, Stats.Pierce));

            if (_links.Count == 0)
            {
                // 전부 사거리 밖으로 나가면 빈 사슬을 그릴 이유가 없다.
                _activeField.Stop();
                _activeField = null;
                return;
            }

            _activeField.Retarget(_links);
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
