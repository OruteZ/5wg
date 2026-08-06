
namespace FiveWG.Stage
{
    /// <summary>
    /// 스테이지의 진행 상태. Cleared/Failed는 종착 상태라 다시 Playing으로 돌아오지 않는다.
    /// 재도전은 상태 되돌리기가 아니라 씬 재로드로 처리한다.
    /// </summary>
    public enum StageState
    {
        Ready,
        Playing,
        Cleared,
        Failed
    }
}
