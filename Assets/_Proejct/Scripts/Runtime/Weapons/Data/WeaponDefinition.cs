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

        [Title("타이밍 (레벨별 고정 칸 패턴)")]
        [SerializeField, LabelText("레벨별 트리거 칸 (1레벨부터, 레벨 표와 같은 길이)")]
        private TriggerPattern[] _triggerPatterns = { TriggerPattern.Empty };

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public Sprite Icon => _icon;
        public int MaxLevel => _levels is { Length: > 0 } ? _levels.Length : 1;

        public WeaponLevelData GetLevelData(int level)
        {
            return _levels[Mathf.Clamp(level, 1, _levels.Length) - 1];
        }

#if UNITY_EDITOR
        // 표를 저장할 때 한 번만 보정한다. 예전엔 발사마다 돌았다.
        private void OnValidate()
        {
            if (_levels != null)
            {
                for (int i = 0; i < _levels.Length; i++) _levels[i] = _levels[i].Sanitized();
            }

            // 트리거 칸을 비워두면 그 레벨에서 영원히 발사가 안 된다 — 조용히 묻히기 쉬운 실수라 저장 시 바로 알린다.
            if (_triggerPatterns == null) return;
            for (int i = 0; i < _triggerPatterns.Length; i++)
            {
                if (_triggerPatterns[i].Cells is { Length: > 0 }) continue;

                Debug.LogWarning(
                    $"[{nameof(WeaponDefinition)}] '{DisplayName}' {i + 1}레벨의 트리거 칸이 비어 있다. 이 레벨에서는 발사되지 않는다.",
                    this);
            }
        }

        // 밸런싱은 인스펙터에서 15축 × 5레벨을 무기마다 따로 만지는 것보다 스프레드시트에서 10종을
        // 한 화면에 놓고 하는 쪽이 빠르다. 그 왕복만 열어준다 — 에디터 창이나 CSV 임포터는 두지 않는다.
        [Button, LabelText("레벨 표를 TSV로 복사")]
        private void CopyLevelsAsTsv()
        {
            UnityEditor.EditorGUIUtility.systemCopyBuffer = WeaponLevelData.ToTsv(_levels);
            Debug.Log($"[{DisplayName}] 레벨 표 {MaxLevel}줄을 클립보드에 복사했다.", this);
        }

        [Button, LabelText("클립보드 TSV를 레벨 표에 반영")]
        private void PasteLevelsFromTsv()
        {
            WeaponLevelData[] parsed = WeaponLevelData.FromTsv(UnityEditor.EditorGUIUtility.systemCopyBuffer);
            if (parsed is not { Length: > 0 }) return;

            UnityEditor.Undo.RecordObject(this, "Paste Weapon Levels");
            _levels = parsed;
            OnValidate();
            UnityEditor.EditorUtility.SetDirty(this);

            Debug.Log($"[{DisplayName}] 레벨 표를 {parsed.Length}줄로 덮었다.", this);
        }
#endif

        /// <summary>런타임 무기 인스턴스를 만든다. 무기 종류마다 구현한다.</summary>
        public abstract IWeapon CreateRuntime();

        protected IFireTiming CreateTiming() => new CellPatternTiming(_triggerPatterns);
    }
}
