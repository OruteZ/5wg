using FiveWG.Core;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 장착 시 한 번 주어지는 장기 서비스 묶음. 발사마다 바뀌는 값은 FireContext에 들어간다.
    /// </summary>
    public readonly struct WeaponContext
    {
        public readonly Transform Owner;
        public readonly ProjectilePool Projectiles;
        public readonly ITargetProvider Targets;

        /// <summary>쏘는 쪽의 편. 발사된 탄이 아군을 때리지 않게 하는 근거가 된다.</summary>
        public readonly Faction Faction;

        /// <summary>마우스 조준을 쓰는 무기가 스크린 좌표를 환산할 때 필요하다.</summary>
        public readonly Camera Camera;

        public WeaponContext(
            Transform owner, ProjectilePool projectiles, ITargetProvider targets, Camera camera,
            Faction faction = Faction.Player)
        {
            Owner = owner;
            Projectiles = projectiles;
            Targets = targets;
            Camera = camera;
            Faction = faction;
        }
    }
}
