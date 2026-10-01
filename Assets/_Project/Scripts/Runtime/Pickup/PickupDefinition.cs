using Alchemy.Inspector;
using UnityEngine;

namespace FiveWG.Pickup
{
    /// <summary>
    /// 픽업 한 종류의 기획 데이터. 무엇이 떨어지고 주우면 얼마가 들어오는가.
    ///
    /// 드랍 확률은 여기 없고 PickupDropTable에 있다. 확률은 종류 하나만 보고 정할 수 있는 값이
    /// 아니라 같은 판정을 나눠 쓰는 종류들 사이에서 정해지기 때문이다(일반 8% / 고액 0.5%).
    /// </summary>
    [CreateAssetMenu(fileName = "Pickup", menuName = "5WG/Pickup/Pickup Definition")]
    public sealed class PickupDefinition : ScriptableObject
    {
        [Title("표시")]
        [SerializeField, LabelText("이름")]
        [Tooltip("UI에 나올 이름. 비우면 에셋 이름을 그대로 쓴다.")]
        private string _displayName;

        [SerializeField, LabelText("아이콘")] private Sprite _icon;

        [Title("효과")]
        [SerializeField, LabelText("종류")] private PickupEffect _effect = PickupEffect.Ticket;

        [SerializeField, LabelText("값"), Min(0f)]
        [Tooltip("티켓이면 개수, 물병이면 회복량.\n자석과 리롤은 이 값을 쓰지 않아 0이어도 된다.")]
        private float _amount = 1f;

        [Title("필드")]
        [SerializeField, Required("필드에 떨어질 픽업 프리팹")] private Pickup _prefab;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public Sprite Icon => _icon;
        public PickupEffect Effect => _effect;

        /// <summary>티켓이면 개수, 회복이면 회복량. 자석·리롤은 쓰지 않아 0이어도 된다.</summary>
        public float Amount => _amount;

        public Pickup Prefab => _prefab;
    }
}
