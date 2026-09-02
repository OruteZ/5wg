using Alchemy.Inspector;
using UnityEngine;

namespace FiveWG.Enemies
{
    /// <summary>
    /// 적 한 종류의 기획 수치.
    ///
    /// HP와 속도를 절대값이 아니라 **배수**로 둔다. 시간 곡선은 <see cref="RunPacingPlan"/>에
    /// 하나만 있고 종류별 차이는 거기 곱하는 값 하나로 낸다 — 종류마다 곡선을 들면 7종을 각각
    /// 다시 튜닝해야 한다.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy", menuName = "5WG/Enemies/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Title("정체")]
        [SerializeField, LabelText("표시 이름")]
        [Tooltip("기획서에서 이름이 정해지지 않은 적은 \"[이름 미정]\"으로 둔다. 비워두지 않는다.")]
        private string _displayName = "[이름 미정]";

        [SerializeField, Required("프리팹이 없으면 이 적은 스폰되지 않는다."), LabelText("프리팹")]
        private Enemy _prefab;

        [SerializeField, LabelText("성격 메모")]
        [Tooltip("기획서의 '성격' 열. 게임에는 안 나오고 밸런싱할 때만 본다.")]
        private string _note;

        // 종류별 스프라이트가 나오기 전까지 화면에서 종류를 가르는 유일한 단서다.
        [SerializeField, LabelText("색 (스프라이트 없는 동안의 임시 구분)")]
        private Color _color = Color.white;

        [Title("스탯")]
        [SerializeField, LabelText("HP 결정 방식")] private EnemyHealthMode _healthMode = EnemyHealthMode.CurveMultiplier;

        [SerializeField, LabelText("HP 배수 (기본 HP 곡선에 곱한다)"), Min(0f)]
        private float _healthMultiplier = 1f;

        [SerializeField, LabelText("HP 절대값 (곡선 무시)"), Min(0f)]
        [Tooltip("HP 결정 방식이 Absolute일 때만 쓰인다.")]
        private float _absoluteHealth;

        [SerializeField, LabelText("이동 속도 배수 (기본 속도에 곱한다)"), Min(0f)]
        private float _moveSpeedMultiplier = 1f;

        [SerializeField, LabelText("접촉 데미지"), Min(0f)]
        private float _contactDamage = 10f;

        [SerializeField, LabelText("드랍 음표 수"), Min(0f)]
        [Tooltip("음표 종류별 분포는 아직 구현되지 않았다. 지금은 이 개수가 그대로 경험치 값이 된다.")]
        private float _noteReward = 1f;

        [SerializeField, LabelText("크기 배수"), Min(0.01f)]
        [Tooltip("콜라이더가 스프라이트 반폭에 맞춰져 있어 겉보기와 피격 판정이 같이 커진다.")]
        private float _sizeMultiplier = 1f;

        [Title("면역")]
        [SerializeField, LabelText("넉백 면역")] private bool _immuneToKnockback;
        [SerializeField, LabelText("디스폰 면역")] private bool _immuneToDespawn;

        [Title("분류")]
        [SerializeField, LabelText("엘리트")]
        [Tooltip("켜면 처치 시 상자 보상 경로를 탄다. 수치는 다른 적과 똑같이 이 에셋에 적는다.")]
        private bool _isElite;

        [Title("등장")]
        [SerializeField, LabelText("등장 시점 (sec)"), Min(0f)]
        [Tooltip("이 시각 전에는 평상시 추첨에서 빠진다. 지정 웨이브는 이 값을 보지 않는다.")]
        private float _unlockTimeSec;

        public string DisplayName => _displayName;
        public Enemy Prefab => _prefab;
        public float HealthMultiplier => _healthMultiplier;
        public float MoveSpeedMultiplier => _moveSpeedMultiplier;
        public float UnlockTimeSec => _unlockTimeSec;

        [ShowInInspector, ReadOnly, LabelText("등장 시점 (분:초)")]
        private string UnlockTimeLabel => $"{Mathf.FloorToInt(_unlockTimeSec / 60f)}:{_unlockTimeSec % 60f:00}";

        /// <summary>시간 곡선의 기본값과 기본 속도를 받아 이 종류의 최종 수치를 만든다.</summary>
        public EnemyStats Resolve(float baseHealth, float baseMoveSpeed)
        {
            // 절대값은 곡선을 타지 않는다. 목표 처치 시간에서 직접 뽑은 값이다.
            float health = _healthMode == EnemyHealthMode.Absolute ? _absoluteHealth : baseHealth * _healthMultiplier;

            return new EnemyStats(
                health,
                baseMoveSpeed * _moveSpeedMultiplier,
                _contactDamage,
                _noteReward,
                _sizeMultiplier,
                _isElite,
                _color,
                _immuneToKnockback,
                _immuneToDespawn);
        }

        // 스폰 즉시 죽으면 화면에서는 "그 적이 안 나온다"로만 보인다.
        private void OnValidate()
        {
            if (_healthMode != EnemyHealthMode.Absolute || _absoluteHealth > 0f) return;

            Debug.LogWarning(
                $"[{nameof(EnemyDefinition)}] \"{name}\"이(가) HP 절대값 모드인데 값이 0이다. 스폰 즉시 죽는다.", this);
        }
    }
}
