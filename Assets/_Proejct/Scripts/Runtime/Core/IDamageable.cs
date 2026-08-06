
namespace FiveWG.Core
{
    /// <summary>
    /// 피격 대상. 투사체가 적의 구체 타입을 모르게 분리하기 위한 인터페이스.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>어느 편인가. 조준·피격 판정이 이 값으로 아군을 걸러낸다.</summary>
        Faction Faction { get; }

        void TakeDamage(float amount);
    }
}
