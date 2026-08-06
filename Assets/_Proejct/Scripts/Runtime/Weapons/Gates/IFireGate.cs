
namespace FiveWG.Weapons
{
    /// <summary>
    /// 발사 허용 조건 하나. WeaponHandler가 등록된 게이트를 전부 AND로 묶어 판단한다.
    /// 사망·컷신·침묵 디버프처럼 "쏠 수 없는 상황"이 늘어날 때 조건문 대신 게이트를 추가한다.
    /// </summary>
    public interface IFireGate
    {
        bool IsAllowed(IWeapon weapon);
    }
}
