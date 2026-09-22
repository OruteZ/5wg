using Alchemy.Inspector;
using FiveWG.Combat;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>어느 방향으로 쏘는가.</summary>
    public enum ProjectileAimMode
    {
        [InspectorName("자동 조준 (최근접 적 → 이동 방향)")] Auto,

        [InspectorName("시계 초침 (칸마다 정해진 각도로 회전, 대상 안 봄)")] ClockHand,
    }

    /// <summary>
    /// 투사체를 쏘는 무기의 데이터. 형태(구체적 모양·조준 방식)가 같은 무기들이 이 에셋을 공유한다.
    /// 다른 형태(장판·오라·근접 등)가 필요해지면 WeaponDefinition을 새로 상속한다.
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileWeapon", menuName = "Weapons/Projectile Weapon")]
    public sealed class ProjectileWeaponDefinition : WeaponDefinition
    {
        [Title("투사체")]
        [SerializeField, Required("투사체 프리팹")] private Projectile _projectilePrefab;
        [SerializeField, LabelText("탄퍼짐 각 (도, 전체 폭)")] private float _spreadDegrees;
        [SerializeField, LabelText("조준 방식")] private ProjectileAimMode _aimMode = ProjectileAimMode.Auto;

        [Title("수명 (프리팹 기본값을 무기별로 덮어쓸 때만 채운다)")]
        [SerializeField, LabelText("수명 강제 (sec, 0이면 프리팹 기본값)")] private float _lifetimeOverride;

        [Title("감쇠 (오르간 오브처럼 날아가며 약해지는 탄에만 쓴다)")]
        [SerializeField, LabelText("수명 끝 데미지 배율 (1이면 감쇠 없음)")] private float _damageDecayFloor = 1f;
        [SerializeField, LabelText("수명 끝 크기 배율 (1이면 감쇠 없음)")] private float _scaleDecayFloor = 1f;

        public Projectile ProjectilePrefab => _projectilePrefab;
        public float SpreadDegrees => _spreadDegrees;
        public ProjectileAimMode AimMode => _aimMode;
        public float LifetimeOverride => Mathf.Max(0f, _lifetimeOverride);
        public float DamageDecayFloor => Mathf.Clamp01(_damageDecayFloor);
        public float ScaleDecayFloor => Mathf.Clamp(_scaleDecayFloor, 0.01f, 1f);

        public override IWeapon CreateRuntime() => new ProjectileWeapon(this, CreateTiming());
    }
}
