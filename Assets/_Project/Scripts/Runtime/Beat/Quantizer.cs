using System;


namespace BeatTemplate
{   
    /// <summary>
    /// 시간 실수를 박자 그리드에 맞추어서 계산하는 도구입니다.
    /// </summary>
    public static class Quantizer
    {
        // Returns (mainBeat, subBeat). leadSub shifts the grid forward (e.g., 0.25 for 16th-early input).
        public static (int main, int sub) Quantize(double bpm, double elapsedSec, int subPerBeat, double leadSub = 0.0)
        {
            double subPos = elapsedSec * (bpm / 60.0) * subPerBeat + leadSub;
            int subIndex = (int)Math.Floor(subPos + 1e-9);
            if (subIndex < 0) subIndex = 0;
            return (subIndex / subPerBeat, subIndex % subPerBeat);
        }


        // Returns nearest sub-grid time (sec) relative to beat-0, and delta (tapSec - gridSec)
        public static (double gridSec, double deltaSec) NearestSubGrid(double bpm, int subPerBeat, double tapSec)
        {
            double subSec = BeatMath.SubSeconds(bpm, subPerBeat);
            double k = Math.Round(tapSec / subSec);
            double grid = k * subSec;
            return (grid, tapSec - grid);
        }
    }
}