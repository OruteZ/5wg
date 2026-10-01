namespace FiveWG.Enemies
{
    /// <summary>적의 최대 체력을 어떻게 정하는가.</summary>
    public enum EnemyHealthMode
    {
        /// <summary>시간 곡선의 기본 HP에 배수를 곱한다.</summary>
        CurveMultiplier = 0,

        /// <summary>
        /// 곡선을 무시하고 적어둔 값을 그대로 쓴다. 보스처럼 등장 시각이 하나뿐이고 기획이
        /// 목표 처치 시간에서 HP를 직접 뽑은 경우다 — 배수로 옮기면 "133배" 같은 숫자가 된다.
        /// </summary>
        Absolute = 1,
    }
}
