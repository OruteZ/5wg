using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 무기별 고정 칸 패턴으로 발사 여부를 판단한다. <see cref="EveryBeatTiming"/>·<see cref="BurstTiming"/>이
    /// 하던 "기획서 확정 전 임시" 역할을 대체한다 — 이제 패턴 자체가 기획서 수치다.
    ///
    /// 연타(BurstTiming이 하던 것)는 별도 클래스가 필요 없다. 인접한 칸 여러 개를 트리거 집합에
    /// 넣으면 그대로 연타가 된다.
    /// </summary>
    public sealed class CellPatternTiming : IFireTiming
    {
        private readonly TriggerPattern[] _patternsByLevel;

        public CellPatternTiming(TriggerPattern[] patternsByLevel)
        {
            _patternsByLevel = patternsByLevel is { Length: > 0 }
                ? patternsByLevel
                : new[] { TriggerPattern.Empty };
        }

        public bool ShouldFire(in BeatTick tick, int level)
        {
            TriggerPattern pattern = ResolvePattern(level);
            if (pattern.Cells is not { Length: > 0 }) return false;

            int cell = CellMath.CellInPattern(tick, pattern.PatternBars);

            for (int i = 0; i < pattern.Cells.Length; i++)
            {
                if (pattern.Cells[i] == cell) return true;
            }

            return false;
        }

        private TriggerPattern ResolvePattern(int level)
        {
            int index = Mathf.Clamp(level, 1, _patternsByLevel.Length) - 1;
            return _patternsByLevel[index];
        }

        public void Reset()
        {
            // 곡 시작 기준 절대 칸 인덱스로만 판단하므로 되돌릴 상태가 없다.
        }
    }
}
