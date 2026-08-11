using Alchemy.Inspector;
using FiveWG.Core;
using FiveWG.Player;
using FiveWG.Progression;
using UnityEngine;

namespace FiveWG.Pickup
{
    /// <summary>
    /// 플레이어가 주운 픽업을 받는 곳. 종류별 분기가 있는 유일한 자리다.
    /// 효과가 늘어도 여기 switch 하나만 늘어난다.
    /// </summary>
    [RequireComponent(typeof(PlayerInventory))]
    public sealed class PickupCollector : MonoBehaviour, IPickupReceiver
    {
        [Title("참조 (비우면 자동으로 찾는다)")]
        [SerializeField, LabelText("플레이어")] private PlayerController _player;
        [SerializeField, LabelText("보유량")] private PlayerInventory _inventory;

        [SerializeField, LabelText("경험치 오브 풀")]
        [Tooltip("자석을 주웠을 때 오브를 끌어오라고 부른다.")]
        private ExpOrbPool _expOrbs;

        private void Awake()
        {
            if (_player == null) _player = GetComponent<PlayerController>();
            if (_inventory == null) _inventory = GetComponent<PlayerInventory>();
        }

        private void Start()
        {
            if (_expOrbs == null) _expOrbs = SceneServices.Instance.ExpOrbs;
        }

        public void Receive(PickupDefinition definition)
        {
            if (definition == null) return;

            switch (definition.Effect)
            {
                case PickupEffect.Ticket:
                    _inventory.AddTickets(Mathf.RoundToInt(definition.Amount));
                    break;

                case PickupEffect.Heal:
                    if (_player != null) _player.Heal(definition.Amount);
                    break;

                // 오브를 끌어오는 건 경험치 쪽이 이미 한다. 여기서는 켜기만 한다.
                case PickupEffect.Magnet:
                    if (_expOrbs != null) _expOrbs.MagnetizeAll();
                    break;

                case PickupEffect.Reroll:
                    _inventory.AddReroll();
                    break;
            }
        }
    }
}
