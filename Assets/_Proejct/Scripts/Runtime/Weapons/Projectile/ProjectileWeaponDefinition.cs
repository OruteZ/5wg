using Alchemy.Inspector;
using UnityEngine;

/// <summary>
/// 투사체를 쏘는 무기의 데이터. 6개 무기 중 이 형태에 해당하는 것들이 이 에셋을 공유한다.
/// 다른 형태(장판·오라·근접 등)가 필요해지면 WeaponDefinition을 새로 상속한다.
/// </summary>
[CreateAssetMenu(fileName = "ProjectileWeapon", menuName = "Weapons/Projectile Weapon")]
public sealed class ProjectileWeaponDefinition : WeaponDefinition
{
    [Title("투사체")]
    [SerializeField, Required("투사체 프리팹")] private Projectile2D _projectilePrefab;
    [SerializeField, LabelText("탄퍼짐 각 (도, 전체 폭)")] private float _spreadDegrees;

    public Projectile2D ProjectilePrefab => _projectilePrefab;
    public float SpreadDegrees => _spreadDegrees;

    public override IWeapon CreateRuntime() => new ProjectileWeapon(this, CreateTiming());
}
