using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 시계 초침처럼 마디 안에서 칸마다 정해진 각도로 도는 조준. 대상을 보지 않는다 —
    /// 하이햇이 쓴다: 플레이어를 중심으로 12시(칸 0)에서 시작해 시계방향으로 한 칸당 22.5˚씩 돈다.
    /// </summary>
    public static class ClockHandAim
    {
        private const float DegreesPerCell = 360f / 16f;

        public static Vector2 DirectionFor(in BeatTick tick)
        {
            int cell = CellMath.CellInBar(tick);
            float radians = cell * DegreesPerCell * Mathf.Deg2Rad;

            // 칸 0 = 12시(위쪽, +Y) = (0,1). 시계방향으로 돌므로 각이 커질수록 +X 쪽으로 기운다.
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }
    }
}
