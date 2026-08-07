using FiveWG.Core;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 장착 시 한 번 주어지는 장기 서비스 묶음
    /// </summary>
    public readonly struct WeaponContext
    {
        public readonly Transform owner;
        public readonly ProjectilePool projectiles;
        public readonly ITargetProvider targets;

        /// <summary>쏘는 쪽의 편.</summary>
        public readonly Faction Faction;

        /// <summary>마우스 조준을 쓰는 무기가 스크린 좌표를 환산할 때 필요</summary>
        public readonly Camera camera;

        public WeaponContext(
            Transform owner, ProjectilePool projectiles, ITargetProvider targets, Camera camera,
            Faction faction = Faction.Player)
        {
            this.owner = owner;
            this.projectiles = projectiles;
            this.targets = targets;
            this.camera = camera;
            Faction = faction;
        }
    }
}
