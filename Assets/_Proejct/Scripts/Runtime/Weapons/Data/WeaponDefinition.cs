using Alchemy.Inspector;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 무기 하나의 기획 데이터이자 런타임 인스턴스 팩토리.
    /// 인벤토리는 이 에셋 참조를 무기 식별자로 쓴다(세이브가 필요해지면 별도 문자열 ID를 추가한다).
    /// </summary>
    public abstract class WeaponDefinition : ScriptableObject
    {
        [Title("표시")]
        [SerializeField, LabelText("이름")] private string _displayName;
        [SerializeField, LabelText("아이콘")] private Sprite _icon;

        [Title("레벨 표")]
        [SerializeField, LabelText("레벨별 수치 (1레벨부터)")]
        private WeaponLevelData[] _levels = { WeaponLevelData.Default };

        [Title("타이밍 (기획서 확정 전 임시)")]
        [SerializeField, LabelText("발사 주기 (정박)")] private int _intervalBeats = 1;
        [SerializeField, LabelText("시작 오프셋 (정박)")] private int _offsetBeats;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public Sprite Icon => _icon;
        public int MaxLevel => _levels is { Length: > 0 } ? _levels.Length : 1;

        public WeaponLevelData GetLevelData(int level)
        {
            if (_levels is not { Length: > 0 }) return WeaponLevelData.Default;

            return _levels[Mathf.Clamp(level, 1, _levels.Length) - 1].Sanitized();
        }

        /// <summary>런타임 무기 인스턴스를 만든다. 무기 종류마다 구현한다.</summary>
        public abstract IWeapon CreateRuntime();

        /// <summary>
        /// 기획서 확정 전까지 쓰는 임시 타이밍 생성. 확정되면 이 메서드만 무기별 패턴으로 교체하면 된다.
        /// </summary>
        protected IFireTiming CreateTiming() => new EveryBeatTiming(_intervalBeats, _offsetBeats);
    }
}
