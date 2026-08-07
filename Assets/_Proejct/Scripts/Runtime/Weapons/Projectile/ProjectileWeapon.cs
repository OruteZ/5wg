using FiveWG.Combat;
using FiveWG.Core;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 투사체를 쏘는 무기. 조준은 WeaponBase의 기본 순서(최근접 적 → 마우스 → 스틱 → 이동 방향)를 그대로 쓴다.
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

            for (int i = 0; i < count; i++)
            {
                Projectile projectile = Context.projectiles.Get(_definition.ProjectilePrefab);
                if (projectile == null) return;

                projectile.Launch(
                    context.origin,
                    AimHelper.Spread(direction, i, count, _definition.SpreadDegrees),
                    stats.Damage,
                    stats.ProjectileSpeed,
                    Context.owner,
                    Context.Faction);
            }
        }
    }
}
