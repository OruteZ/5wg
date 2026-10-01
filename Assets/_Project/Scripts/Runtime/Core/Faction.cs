
namespace FiveWG.Core
{
    /// <summary>
    /// 피격·조준 대상을 가르는 편. "누가 누구를 때릴 수 있는가"를 계층 비교나 레이어가 아니라
    /// 이 값으로 판단한다.
    ///
    /// 계층(<c>transform.root</c>) 비교로 아군을 걸러내던 방식은 소환수·아군 NPC처럼
    /// 서로 다른 계층에 있는 같은 편이 생기는 순간 무너진다.
    /// </summary>
    public enum Faction
    {
        Player,
        Enemy,
        Neutral
    }
}
