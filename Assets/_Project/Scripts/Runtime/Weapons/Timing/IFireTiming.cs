
namespace FiveWG.Weapons
{
    /// <summary>
    /// 무기가 "이 틱에 발사할지"만 판단하는 규칙.
    /// 발사 내용(무엇을 어떻게 쏘는지)은 IWeapon.Fire가, 발사 가능 여부는 IFireGate가 담당한다.
    /// 기획서 확정 후 무기별 구현체를 여기에 추가한다.
    /// </summary>
    public interface IFireTiming
    {
        bool ShouldFire(in BeatTick tick);

        /// <summary>장착·부활·곡 재시작 시 호출. 카운터를 쓰는 구현체가 위상을 되돌린다.</summary>
        void Reset();
    }
}
