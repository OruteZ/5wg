using UnityEngine;

/// <summary>
/// 장착 시 한 번 주어지는 장기 서비스 묶음. 발사마다 바뀌는 값은 FireContext에 들어간다.
/// </summary>
public readonly struct WeaponContext
{
    public readonly Transform Owner;
    public readonly ProjectilePool Projectiles;
    public readonly ITargetProvider Targets;

    /// <summary>마우스 조준을 쓰는 무기가 스크린 좌표를 환산할 때 필요하다.</summary>
    public readonly Camera Camera;

    public WeaponContext(Transform owner, ProjectilePool projectiles, ITargetProvider targets, Camera camera)
    {
        Owner = owner;
        Projectiles = projectiles;
        Targets = targets;
        Camera = camera;
    }
}
