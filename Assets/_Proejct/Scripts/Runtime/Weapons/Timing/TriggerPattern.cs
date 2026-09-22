using System;
using Alchemy.Inspector;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 한 레벨의 고정 트리거 칸 집합. 옵시디언 무기 기획서의 "트리거" 표기를 그대로 데이터로 옮긴 것.
    ///
    /// 채보(곡 분석) 대신 무기마다 고정된 칸에서 쏜다 — 트리거는 마디 내 상대 위치(0~15)라
    /// 스테이지가 진행되며 BPM이 올라가도 패턴 자체는 그대로고 실제 간격만 짧아진다.
    /// </summary>
    [Serializable]
    public struct TriggerPattern
    {
        [LabelText("패턴 길이 (마디 수. 보통 1, 8마디 중 1마디째처럼 저빈도 무기는 더 길다)")]
        public int PatternBars;

        [LabelText("발사할 칸 (0~15, 패턴이 여러 마디면 그만큼 확장)")]
        public int[] Cells;

        public static TriggerPattern Empty => new() { PatternBars = 1, Cells = Array.Empty<int>() };
    }
}
