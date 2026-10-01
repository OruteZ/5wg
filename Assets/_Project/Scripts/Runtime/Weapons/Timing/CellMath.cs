using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 절대 비트를 마디 내 "칸"(16분음표, 0~15)으로 접는 계산.
    /// 무기 기획서(옵시디언 [05-06])의 칸 표기와 여기 결과가 1:1로 맞아야 한다 — 어긋나면
    /// 트리거 타이밍과 조준 각도(시계 초침류)가 조용히 한 칸씩 밀린다.
    /// </summary>
    public static class CellMath
    {
        /// <summary>4/4 박자 가정. 다른 박자의 곡이 들어오면 이 상수부터 바뀐다.</summary>
        public const int BeatsPerBar = 4;

        /// <summary>패턴 없이 한 마디(16칸) 안에서의 위치. 시계 초침류의 각도 계산이 쓴다.</summary>
        public static int CellInBar(in BeatTick tick) => CellInPattern(tick, patternBars: 1);

        /// <summary>
        /// patternBars마디짜리 반복 패턴 안에서의 절대 칸 위치.
        /// 곡 시작(비트 0) 기준 누적 칸이므로 스테이지 재시작마다 위상이 같다.
        /// </summary>
        public static int CellInPattern(in BeatTick tick, int patternBars)
        {
            int cellsPerBar = BeatsPerBar * Mathf.Max(1, tick.SubPerBeat);
            long totalCells = (long)cellsPerBar * Mathf.Max(1, patternBars);

            // C#의 %는 음수를 음수로 돌려주므로, 절대 인덱스가 음수일 일은 없지만
            // 방어적으로 한 번 더 감싼다(0 미만이 나오면 다음 줄에서 총 길이를 더해 양수로 되돌린다).
            long cell = tick.SubIndex % totalCells;
            return (int)(cell < 0 ? cell + totalCells : cell);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 칸 계산이 한 칸이라도 밀리면 트리거·조준이 전부 어긋나므로 알려진 값으로 왕복 확인한다.
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void SelfCheck()
        {
            // subPerBeat 4 = 4/4에서 16분음표. 1마디 = 4박 × 4서브 = 16칸.
            (int beat, int sub, int expectedCellInBar)[] cases =
            {
                (0, 0, 0), (0, 1, 1), (1, 0, 4), (3, 3, 15), (4, 0, 0), // 다음 마디로 넘어가면 다시 0
            };

            foreach ((int beat, int sub, int expected) in cases)
            {
                var tick = new BeatTick(beat, sub, subPerBeat: 4, bpm: 120d, dspTime: 0d);
                int actual = CellInBar(tick);
                if (actual == expected) continue;

                Debug.LogError(
                    $"[{nameof(CellMath)}] beat={beat} sub={sub}의 칸이 {expected}여야 하는데 {actual}이다.");
            }

            // 2마디 패턴(칸 0~31)에서 두 번째 마디로 넘어간 위치도 확인한다.
            var secondBarTick = new BeatTick(beat: 4, sub: 2, subPerBeat: 4, bpm: 120d, dspTime: 0d);
            int secondBarCell = CellInPattern(secondBarTick, patternBars: 2);
            if (secondBarCell != 18)
            {
                Debug.LogError(
                    $"[{nameof(CellMath)}] 2마디 패턴에서 4박 2서브는 칸 18이어야 하는데 {secondBarCell}이다.");
            }
        }
#endif
    }
}
