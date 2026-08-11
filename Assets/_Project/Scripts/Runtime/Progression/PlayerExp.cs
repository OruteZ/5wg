using System;
using Alchemy.Inspector;
using UnityEngine;

namespace FiveWG.Progression
{
    /// <summary>
    /// 경험치 누적과 레벨 계산만 담당한다.
    /// 레벨업 시 무엇을 보여주고 무엇을 강화할지는 이 컴포넌트가 모른다 — 이벤트만 쏜다.
    /// (레벨업 UI·업그레이드 선택은 다음 브랜치)
    /// </summary>
    public sealed class PlayerExp : MonoBehaviour
    {
        [Title("레벨 곡선")]
        [SerializeField, LabelText("1→2레벨 요구 경험치"), Min(1f)] private float _baseRequirement = 5f;

        // 레벨당 요구량 증가분. 선형이다.
        [SerializeField, LabelText("레벨당 증가량"), Min(0f)] private float _requirementStep = 3f;

        [SerializeField, LabelText("최대 레벨 (0이면 무제한)"), Min(0)] private int _maxLevel = 0;

        [Title("디버그")]
        [ShowInInspector, ReadOnly, LabelText("레벨")] private int LevelDebug => Level;
        [ShowInInspector, ReadOnly, LabelText("현재 레벨 경험치")] private float CurrentDebug => CurrentExp;
        [ShowInInspector, ReadOnly, LabelText("다음 레벨까지")] private float RequiredDebug => RequiredExp;
        [ShowInInspector, ReadOnly, LabelText("누적 획득")] private float TotalDebug => TotalExp;

        public int Level { get; private set; } = 1;

        /// <summary>현재 레벨에서 모은 경험치. 레벨업하면 요구량만큼 빠진다.</summary>
        public float CurrentExp { get; private set; }

        /// <summary>지금 레벨에서 다음 레벨로 가는 데 필요한 총량.</summary>
        public float RequiredExp { get; private set; }

        /// <summary>스테이지 내내 모은 총합. 리셋되지 않아 결과 집계에 쓸 수 있다.</summary>
        public float TotalExp { get; private set; }

        /// <summary>0~1 진행도. HUD가 그대로 그린다.</summary>
        public float Progress => RequiredExp <= 0f ? 1f : Mathf.Clamp01(CurrentExp / RequiredExp);

        public bool IsMaxLevel => _maxLevel > 0 && Level >= _maxLevel;

        /// <summary>레벨이 오른 뒤 발행된다. 인자는 오른 뒤의 레벨.</summary>
        public event Action<int> OnLeveledUp;

        /// <summary>경험치가 변할 때마다 발행된다. (현재, 요구량)</summary>
        public event Action<float, float> OnExpChanged;

        private void Awake() => RequiredExp = RequirementFor(Level);

        /// <summary>
        /// 레벨 L에서 L+1로 가는 데 필요한 양. `5 + 3 × (레벨 - 1)` 선형이다(`progression.md`).
        ///
        /// 지수 곡선(VS식)을 쓰지 않는 이유는 **선택이 실시간이기 때문**이다 — 초반 1~2분에
        /// 카드가 10번 넘게 뜨면 화면 아래를 계속 가린다. 40회 기준 누적은 2,540이다.
        /// </summary>
        private float RequirementFor(int level) => _baseRequirement + _requirementStep * (level - 1);

        public void AddExp(float amount)
        {
            if (amount <= 0f) return;

            TotalExp += amount;

            if (IsMaxLevel)
            {
                // 최대 레벨에서는 바를 채운 채로 둔다. 누적만 계속 쌓는다.
                OnExpChanged?.Invoke(CurrentExp, RequiredExp);
                return;
            }

            CurrentExp += amount;

            // 한 번에 여러 레벨이 오를 수 있다(보스 처치·후반 오브 폭식).
            while (!IsMaxLevel && CurrentExp >= RequiredExp)
            {
                CurrentExp -= RequiredExp;
                Level++;
                RequiredExp = RequirementFor(Level);

                OnLeveledUp?.Invoke(Level);
            }

            if (IsMaxLevel) CurrentExp = RequiredExp;

            OnExpChanged?.Invoke(CurrentExp, RequiredExp);
        }

        [Button, LabelText("경험치 10 지급")]
        private void DebugGrant()
        {
            if (Application.isPlaying) AddExp(10f);
        }
    }
}
