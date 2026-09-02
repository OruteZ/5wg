using UnityEngine;

namespace FiveWG.Enemies
{
    /// <summary>
    /// 스폰 시점에 계산이 끝난 적 한 마리의 수치.
    /// 값으로 넘겨서 적이 "지금 몇 분째인가"를 몰라도 되게 한다.
    /// </summary>
    public readonly struct EnemyStats
    {
        public readonly float MaxHealth;
        public readonly float MoveSpeed;
        public readonly float ContactDamage;

        /// <summary>죽을 때 떨구는 음표 개수. 종류별 분포는 픽업 쪽이 정한다.</summary>
        public readonly float NoteReward;

        public readonly float SizeMultiplier;

        /// <summary>종류를 화면에서 가르는 색. 스프라이트가 하나뿐이라 지금은 이게 유일한 단서다.</summary>
        public readonly Color Color;

        /// <summary>보상 경로가 다르다(상자).</summary>
        public readonly bool IsElite;

        /// <summary>밀려나면 밀면서 도망치는 것이 공략이 된다. 보스가 이쪽이다.</summary>
        public readonly bool ImmuneToKnockback;

        /// <summary>회수되면 도망만 다녔을 때 화면에서 사라진다. 보스가 이쪽이다.</summary>
        public readonly bool ImmuneToDespawn;

        public EnemyStats(
            float maxHealth, float moveSpeed, float contactDamage,
            float noteReward, float sizeMultiplier, bool isElite,
            Color color, bool immuneToKnockback = false, bool immuneToDespawn = false)
        {
            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            ContactDamage = contactDamage;
            NoteReward = noteReward;
            SizeMultiplier = sizeMultiplier;
            IsElite = isElite;
            Color = color;
            ImmuneToKnockback = immuneToKnockback;
            ImmuneToDespawn = immuneToDespawn;
        }
    }
}
