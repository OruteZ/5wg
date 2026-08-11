using FiveWG.Combat;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 투사체를 쏘는 무기. 조준은 WeaponBase의 기본 순서(최근접 적 → 마우스 → 스틱 → 이동 방향)를 그대로 쓴다.
    ///
    /// 악기 넷이 이 형태를 공유한다 — 스네어(직선 관통), 하이헷(다발 산탄),
    /// 기타(유도), 어쿠스틱(넓은 관통 파동). 차이는 전부 레벨 표의 수치와 탄퍼짐 각으로만 난다.
    /// </summary>
    public sealed class ProjectileWeapon : WeaponBase
    {
        private readonly ProjectileWeaponDefinition _definition;

        public ProjectileWeapon(ProjectileWeaponDefinition definition, IFireTiming timing)
            : base(definition, timing)
        {
            _definition = definition;
        }

        protected override void OnFire(in FireContext context)
        {
            if (_definition.ProjectilePrefab == null || Context.projectiles == null) return;

            WeaponLevelData stats = Stats;
            Vector2 direction = ResolveAimDirection(context);
            int count = stats.ProjectileCount;

            ProjectileSpawnData data = new()
            {
                Damage = stats.Damage,
                Speed = stats.ProjectileSpeed,
                Pierce = stats.Pierce,
                TurnRateDegrees = stats.Homing,
                HomingTarget = ResolveHomingTarget(stats, context),
                Scale = stats.ProjectileScale,
                Knockback = stats.Knockback,
                Owner = Context.owner,
                OwnerFaction = Context.Faction,
            };

            for (int i = 0; i < count; i++)
            {
                Projectile projectile = Context.projectiles.Get(_definition.ProjectilePrefab);
                if (projectile == null) return;

                projectile.Launch(
                    context.origin,
                    AimHelper.Spread(direction, i, count, _definition.SpreadDegrees),
                    data);
            }
        }

        /// <summary>유도를 안 쓰는 무기는 대상 탐색 자체를 건너뛴다.</summary>
        private Transform ResolveHomingTarget(in WeaponLevelData stats, in FireContext context)
        {
            if (stats.Homing <= 0f || Context.targets is null) return null;

            return Context.targets.TryGetNearest(context.origin, stats.Range, out Transform target)
                ? target
                : null;
        }
    }
}
