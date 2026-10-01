
namespace FiveWG.Weapons
{
    /// <summary>
    /// 무기가 "이 틱에 발사할지"만 판단하는 규칙.
    /// 발사 내용(무엇을 어떻게 쏘는지)은 IWeapon.Fire가, 발사 가능 여부는 IFireGate가 담당한다.
    /// </summary>
    public interface IFireTiming
    {
        /// <summary>
        /// level이 필요한 이유: 트리거 칸 자체가 레벨마다 바뀐다(노트 추가). 레벨을 모르면
        /// "지금 몇 레벨의 패턴을 볼지"를 타이밍 쪽에 별도로 동기화해야 해서 조용히 어긋나기 쉽다.
        /// </summary>
        bool ShouldFire(in BeatTick tick, int level);

        /// <summary>장착·부활·곡 재시작 시 호출. 카운터를 쓰는 구현체가 위상을 되돌린다.</summary>
        void Reset();
    }
}
