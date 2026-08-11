using Alchemy.Inspector;
using FiveWG.Combat;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>영역이 어디에 생기는가.</summary>
    public enum AreaPlacement
    {
        [InspectorName("발사 주체 위치")] Self,
        [InspectorName("사거리 내 최근접 적 위치")] AtNearestTarget,
    }

    /// <summary>
    /// 반경 안을 때리는 무기의 데이터. 킥·크래시·베이스·신스가 이 형태를 공유한다.
    ///
    /// 데미지·반경·지속시간·넉백은 레벨 표에서 온다. 여기 있는 것은 레벨이 올라도 변하지 않는
    /// 형태 파라미터뿐이다 — 그래서 타격 간격이 15축이 아니라 이쪽에 있다.
    /// </summary>
    [CreateAssetMenu(fileName = "AreaWeapon", menuName = "Weapons/Area Weapon")]
    public sealed class AreaWeaponDefinition : WeaponDefinition
    {
        [Title("영역")]
        [SerializeField, Required("영역 프리팹")] private DamageField _fieldPrefab;
        [SerializeField, LabelText("생기는 위치")] private AreaPlacement _placement = AreaPlacement.Self;

        [SerializeField, LabelText("발사 주체를 따라다닌다 (오라. 자기 위치 배치일 때만 적용)")]
        private bool _followOwner;

        [SerializeField, LabelText("타격 간격 (sec, 0이면 생길 때 1회만)")]
        private float _tickInterval;

        public DamageField FieldPrefab => _fieldPrefab;
        public AreaPlacement Placement => _placement;
        public bool FollowOwner => _followOwner;
        public float TickInterval => Mathf.Max(0f, _tickInterval);

        public override IWeapon CreateRuntime() => new AreaWeapon(this, CreateTiming());
    }
}
