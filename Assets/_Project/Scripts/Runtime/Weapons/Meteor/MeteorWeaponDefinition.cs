using Alchemy.Inspector;
using FiveWG.Combat;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 무작위 적 하나를 지나는 직선 위에 순서대로 떨어지는 무기의 데이터. 벨(별똥별) 하나만 쓴다.
    /// </summary>
    [CreateAssetMenu(fileName = "MeteorWeapon", menuName = "Weapons/Meteor Weapon")]
    public sealed class MeteorWeaponDefinition : WeaponDefinition
    {
        [Title("착탄")]
        [SerializeField, Required("착탄 영역 프리팹 (즉발 원)")] private DamageField _impactPrefab;
        [SerializeField, LabelText("착탄 수 (트리거 칸 수와 맞춰야 한다)")] private int _meteorCount = 6;
        [SerializeField, LabelText("착탄 선 길이")] private float _lineLength = 4f;

        public DamageField ImpactPrefab => _impactPrefab;
        public int MeteorCount => Mathf.Max(1, _meteorCount);
        public float LineLength => Mathf.Max(0.1f, _lineLength);

        public override IWeapon CreateRuntime() => new MeteorWeapon(this, CreateTiming());
    }
}
