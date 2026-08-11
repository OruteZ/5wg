using FiveWG.Combat;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 반경 안을 때리는 무기. <see cref="DamageField"/>를 하나 뿌리는 것이 전부다.
    ///
    /// 악기 넷이 이 형태를 공유한다 — 킥(자기 중심 충격파), 크래시(넓은 원형 폭발),
    /// 베이스(주변 지속 오라), 신스(바닥에 남는 장판). 차이는 배치 위치와 지속시간뿐이라
    /// "즉발"과 "지속"을 따로 만들지 않았다.
    /// </summary>
    public sealed class AreaWeapon : WeaponBase
    {
        private readonly AreaWeaponDefinition _definition;

        public AreaWeapon(AreaWeaponDefinition definition, IFireTiming timing)
            : base(definition, timing)
        {
            _definition = definition;
        }

        protected override void OnFire(in FireContext context)
        {
            if (_definition.FieldPrefab == null || Context.projectiles == null) return;

            WeaponLevelData stats = Stats;

            if (!TryResolvePosition(stats, context, out Vector2 position)) return;

            DamageField field = Context.projectiles.Get(_definition.FieldPrefab);
            if (field == null) return;

            field.Begin(position, new DamageFieldData
            {
                Damage = stats.Damage,
                Radius = stats.AreaRadius,
                Duration = stats.Duration,
                TickInterval = _definition.TickInterval,
                Knockback = stats.Knockback,
                OwnerFaction = Context.Faction,

                // 추종은 자기 중심 배치에서만 뜻이 있다. 바닥에 떨어뜨린 장판이 따라오면 장판이 아니다.
                Follow = _definition.Placement == AreaPlacement.Self && _definition.FollowOwner
                    ? Context.owner
                    : null,
            });
        }

        /// <summary>
        /// 대상 위치에 떨어뜨리는 무기는 겨눌 적이 없으면 발사 자체를 거른다.
        /// 빈 땅에 폭발을 띄우면 연출만 나가고 아무 일도 안 일어난다.
        /// </summary>
        private bool TryResolvePosition(in WeaponLevelData stats, in FireContext context, out Vector2 position)
        {
            if (_definition.Placement == AreaPlacement.Self)
            {
                position = context.origin;
                return true;
            }

            if (Context.targets is not null &&
                Context.targets.TryGetNearest(context.origin, stats.Range, out Transform target))
            {
                position = target.position;
                return true;
            }

            position = default;
            return false;
        }
    }
}
