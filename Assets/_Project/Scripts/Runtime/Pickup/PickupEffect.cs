namespace FiveWG.Pickup
{
    /// <summary>주웠을 때 일어나는 일. 수치는 PickupDefinition이 들고 여기는 종류만 가른다.</summary>
    public enum PickupEffect
    {
        /// <summary>판 안에서만 쓰는 돈. 일반과 고액은 효과가 같고 개수만 다르다.</summary>
        Ticket = 0,

        /// <summary>체력 회복.</summary>
        Heal = 1,

        /// <summary>화면의 경험치 오브를 전부 끌어온다. 끌어오는 일은 경험치 쪽이 한다.</summary>
        Magnet = 2,

        /// <summary>모아 두는 자원. 레벨업 카드와 상점에서 쓴다.</summary>
        Reroll = 3,
    }
}
