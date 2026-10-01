namespace FiveWG.Enemies
{
    /// <summary>지정 웨이브의 배치. 셋의 차이는 "도망칠 곳이 있는가"다.</summary>
    public enum EnemyFormation
    {
        /// <summary>플레이어 주변 원주에 무작위. 평상시 스폰과 같다.</summary>
        Random = 0,

        /// <summary>한 방향에 모아서. 반대편으로 도망칠 수 있다.</summary>
        OneDirection = 1,

        /// <summary>둘러싸도록 균등 배치. 어느 쪽으로 가도 뚫어야 한다.</summary>
        Circle = 2,
    }
}
