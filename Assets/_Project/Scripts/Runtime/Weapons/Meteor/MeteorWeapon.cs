using System.Collections.Generic;
using FiveWG.Combat;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 무작위 적을 지나는 직선 위에 착탄 지점을 미리 정해두고, 연속된 트리거 칸마다 한 지점씩 터뜨린다.
    ///
    /// 순차 다단(BurstTiming)과 달리 "어느 지점에 떨어지는가"가 발동 시점마다 달라져야 해서
    /// 무기 자신이 상태(고른 지점들)를 들고 있어야 한다 — 그래서 AreaWeapon을 재사용하지 않고
    /// 새 클래스를 만들었다. 연속된 칸에서 연달아 불렸는지로 "같은 발동의 다음 착탄인지"를 판단한다.
    /// </summary>
    public sealed class MeteorWeapon : WeaponBase
    {
        private readonly MeteorWeaponDefinition _definition;
        private readonly List<Transform> _candidates = new();
        private readonly List<Vector2> _impactPoints = new();

        private long _lastFiredSubIndex = long.MinValue;
        private bool _burstActive;
        private int _burstIndex;

        public MeteorWeapon(MeteorWeaponDefinition definition, IFireTiming timing)
            : base(definition, timing)
        {
            _definition = definition;
        }

        protected override void OnFire(in FireContext context)
        {
            if (_definition.ImpactPrefab == null || Context.projectiles == null || Context.targets is null) return;

            WeaponLevelData stats = Stats;
            long subIndex = context.tick.SubIndex;
            bool isContinuation = _burstActive && subIndex == _lastFiredSubIndex + 1;
            _lastFiredSubIndex = subIndex;

            if (isContinuation)
            {
                _burstIndex++;
            }
            else
            {
                _burstIndex = 0;
                _burstActive = TryPickLine(context.origin, stats.Range, _definition.LineLength, _definition.MeteorCount);
            }

            if (!_burstActive || _burstIndex >= _impactPoints.Count) return;

            DamageField field = Context.projectiles.Get(_definition.ImpactPrefab);
            if (field == null) return;

            field.Begin(_impactPoints[_burstIndex], new DamageFieldData
            {
                Damage = stats.Damage,
                Radius = stats.AreaRadius,
                Duration = 0f,
                TickInterval = 0f,
                Knockback = stats.Knockback,
                OwnerFaction = Context.Faction,
            });
        }

        /// <summary>
        /// 사거리 안 무작위 적 하나를 골라 그 적을 지나는 직선 위에 착탄 지점을 등간격으로 채운다.
        /// 플레이어 쪽 끝부터 순서대로 놓는다 — 착탄은 이 배열을 순서대로 소비한다.
        /// </summary>
        private bool TryPickLine(Vector2 origin, float searchRadius, float lineLength, int meteorCount)
        {
            _impactPoints.Clear();

            int count = Context.targets.GetInRange(origin, searchRadius, _candidates);
            if (count == 0) return false;

            Vector2 target = _candidates[Random.Range(0, count)].position;
            Vector2 direction = (target - origin).sqrMagnitude > 0.0001f ? (target - origin).normalized : Vector2.up;

            for (int i = 0; i < meteorCount; i++)
            {
                float t = meteorCount > 1 ? (float)i / (meteorCount - 1) - 0.5f : 0f;
                _impactPoints.Add(target + direction * (lineLength * t));
            }

            return true;
        }
    }
}
