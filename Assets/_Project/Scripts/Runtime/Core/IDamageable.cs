using UnityEngine;

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

        /// <summary>
        /// 밀려남. 기본 구현은 무시다 — 넉백을 받을 이유가 없는 대상(플레이어 등)이
        /// 빈 메서드를 억지로 채우지 않게 하려고 기본 구현을 둔다.
        /// 넉백 무기가 대상의 구체 타입을 몰라도 되게 하는 것이 목적이므로 별도 인터페이스로 쪼개지 않았다.
        /// </summary>
        void ApplyKnockback(Vector2 impulse)
        {
        }
    }
}
