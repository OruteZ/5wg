using System;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 슬롯 6칸짜리 보유 무기 목록. 획득·업그레이드·제거만 담당하고 발사에는 관여하지 않는다.
    /// UI는 이벤트로 붙이면 되므로 매 프레임 폴링할 필요가 없다.
    /// </summary>
    public sealed class WeaponInventory
    {
        public const int Capacity = 6;

        private readonly IWeapon[] _slots = new IWeapon[Capacity];
        private readonly WeaponContext _context;

        public WeaponInventory(in WeaponContext context)
        {
            _context = context;
        }

        public int Count { get; private set; }
        public bool IsFull => Count >= Capacity;

        /// <summary>(슬롯, 무기)</summary>
        public event Action<int, IWeapon> Acquired;

        public event Action<int, IWeapon> Removed;
        public event Action<int, IWeapon> Upgraded;

        public IWeapon GetAt(int slot) =>
            slot is >= 0 and < Capacity ? _slots[slot] : null;

        public bool TryGetSlot(WeaponDefinition definition, out int slot)
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (_slots[i] is null || _slots[i].Definition != definition) continue;

                slot = i;
                return true;
            }

            slot = -1;
            return false;
        }

        public bool Has(WeaponDefinition definition) => TryGetSlot(definition, out _);

        /// <summary>
        /// 빈 슬롯에 무기를 넣는다. 이미 보유 중이면 새로 넣지 않고 레벨업으로 흡수한다.
        /// 슬롯이 꽉 찼고 보유하지도 않은 무기면 실패한다.
        /// </summary>
        public bool TryAcquire(WeaponDefinition definition, out int slot)
        {
            slot = -1;
            if (definition == null) return false;

            if (TryGetSlot(definition, out slot))
            {
                return TryUpgradeAt(slot);
            }

            for (int i = 0; i < Capacity; i++)
            {
                if (_slots[i] is not null) continue;

                IWeapon weapon = definition.CreateRuntime();
                if (weapon is null) return false;

                _slots[i] = weapon;
                Count++;
                weapon.Equip(_context);

                slot = i;
                Acquired?.Invoke(i, weapon);
                return true;
            }

            return false;
        }

        public bool TryUpgrade(WeaponDefinition definition) =>
            TryGetSlot(definition, out int slot) && TryUpgradeAt(slot);

        public bool TryUpgradeAt(int slot)
        {
            IWeapon weapon = GetAt(slot);
            if (weapon is null) return false;
            if (weapon.Level >= weapon.Definition.MaxLevel) return false;

            weapon.SetLevel(weapon.Level + 1);
            Upgraded?.Invoke(slot, weapon);
            return true;
        }

        public bool TryRemove(int slot)
        {
            IWeapon weapon = GetAt(slot);
            if (weapon is null) return false;

            _slots[slot] = null;
            Count--;
            weapon.Unequip();

            Removed?.Invoke(slot, weapon);
            return true;
        }

        /// <summary>비트와 무관한 무기 내부 시간을 갱신한다. WeaponHandler가 매 프레임 호출한다.</summary>
        public void Tick(float deltaTime)
        {
            for (int i = 0; i < Capacity; i++)
            {
                _slots[i]?.Tick(deltaTime);
            }
        }

        /// <summary>곡 재시작·부활처럼 모든 무기의 위상을 되돌려야 할 때 호출한다.</summary>
        public void ResetTimings()
        {
            for (int i = 0; i < Capacity; i++)
            {
                _slots[i]?.Timing.Reset();
            }
        }
    }
}
