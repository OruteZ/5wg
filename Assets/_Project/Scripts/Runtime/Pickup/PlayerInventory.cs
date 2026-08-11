using System;
using Alchemy.Inspector;
using UnityEngine;

namespace FiveWG.Pickup
{
    /// <summary>
    /// 판 안에서만 유지되는 보유량 — 티켓과 리롤.
    ///
    /// 둘을 한곳에 둔 건 성질이 같아서다. 필드에서만 늘고, 다른 데서 쓰이고, 판이 끝나면 사라진다.
    /// 되돌리는 경로를 따로 두지 않는 것도 같다 — 재도전은 씬을 다시 로드한다.
    ///
    /// 쓰는 쪽(상점·레벨업 카드)은 아직 없다. 지금은 벌어서 들고 있는 데까지다.
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        [Title("리롤")]
        [SerializeField, LabelText("최대 개수"), Min(0)]
        [Tooltip("꽉 찬 뒤에 주운 리롤은 버려진다.")]
        private int _maxRerolls = 5;

        [Title("디버그")]
        [ShowInInspector, ReadOnly, LabelText("티켓")] private int TicketsDebug => Tickets;
        [ShowInInspector, ReadOnly, LabelText("리롤")] private int RerollsDebug => Rerolls;

        /// <summary>가진 티켓. 상점에서 쓴다.</summary>
        public int Tickets { get; private set; }

        /// <summary>가진 리롤. 꽉 찼을 때 더 주우면 그 몫은 버려진다.</summary>
        public int Rerolls { get; private set; }

        public int MaxRerolls => _maxRerolls;

        public event Action<int> OnTicketsChanged;

        public event Action<int> OnRerollsChanged;

        public void AddTickets(int amount)
        {
            if (amount <= 0) return;

            Tickets += amount;
            OnTicketsChanged?.Invoke(Tickets);
        }

        /// <summary>상점 구매용. 모자라면 아무것도 안 하고 false.</summary>
        public bool TryConsumeTickets(int amount)
        {
            if (amount <= 0 || Tickets < amount) return false;

            Tickets -= amount;
            OnTicketsChanged?.Invoke(Tickets);
            return true;
        }

        /// <summary>꽉 차서 안 늘었으면 false. 주웠는데 안 늘었을 때 연출을 가르려고 돌려준다.</summary>
        public bool AddReroll()
        {
            if (Rerolls >= _maxRerolls) return false;

            Rerolls++;
            OnRerollsChanged?.Invoke(Rerolls);
            return true;
        }

        /// <summary>레벨업 카드·상점의 다시 뽑기. 없으면 false.</summary>
        public bool TryConsumeReroll()
        {
            if (Rerolls <= 0) return false;

            Rerolls--;
            OnRerollsChanged?.Invoke(Rerolls);
            return true;
        }
    }
}
