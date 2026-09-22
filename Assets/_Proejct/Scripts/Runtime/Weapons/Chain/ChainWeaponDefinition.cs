using Alchemy.Inspector;
using FiveWG.Combat;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 연결된 대상 전원을 물고 틱 데미지를 주는 무기의 데이터. 신스(체인 빔) 하나만 이 형태를 쓴다.
    /// </summary>
    [CreateAssetMenu(fileName = "ChainWeapon", menuName = "Weapons/Chain Weapon")]
    public sealed class ChainWeaponDefinition : WeaponDefinition
    {
        [Title("사슬")]
        [SerializeField, Required("사슬 영역 프리팹")] private ChainField _fieldPrefab;
        [SerializeField, LabelText("첫 연결 사거리")] private float _firstLinkRange = 7f;
        [SerializeField, LabelText("다음 연결 거리 (링크 사이)")] private float _linkDistance = 3f;
        [SerializeField, LabelText("틱 간격 (sec)")] private float _tickInterval = 0.1f;

        [SerializeField, LabelText("유지 시간 (sec, 다음 재계산 칸까지의 근사값)")]
        private float _activeDuration = 2f;

        public ChainField FieldPrefab => _fieldPrefab;
        public float FirstLinkRange => Mathf.Max(0f, _firstLinkRange);
        public float LinkDistance => Mathf.Max(0f, _linkDistance);
        public float TickInterval => Mathf.Max(0.01f, _tickInterval);
        public float ActiveDuration => Mathf.Max(0.1f, _activeDuration);

        public override IWeapon CreateRuntime() => new ChainWeapon(this, CreateTiming());
    }
}
