using System.Collections.Generic;
using Alchemy.Inspector;
using FiveWG.Core;
using UnityEngine;

namespace FiveWG.Pickup
{
    /// <summary>
    /// 적이 죽은 자리에서 드랍 표를 뽑는 창구.
    ///
    /// 스포너가 아는 픽업 쪽 타입은 이것 하나뿐이라, 확률 규칙이 아무리 늘어도 스포너는 그대로다.
    /// 뽑기와 풀을 나눈 것도 같은 이유다. 풀은 무엇이 왜 나왔는지 몰라야 한다.
    /// </summary>
    public sealed class PickupDropper : MonoBehaviour
    {
        [Title("데이터")]
        [SerializeField, Required("드랍 표")] private PickupDropTable _table;

        [Title("대상 (비우면 자동으로 찾는다)")]
        [SerializeField, LabelText("픽업 풀")] private PickupPool _pool;

        // 적이 죽을 때마다 새로 만들지 않으려고 돌려 쓴다. 후반에는 초당 수십 번 불린다.
        private readonly List<PickupDefinition> _rolled = new();

        private void Start()
        {
            if (_pool == null) _pool = SceneServices.Instance.Pickups;

            if (_table == null)
            {
                Debug.LogError($"[{nameof(PickupDropper)}] 드랍 표가 비어 있다. 픽업이 나오지 않는다.", this);
            }
        }

        /// <summary>적이 죽은 자리에서 한 번 뽑고, 나온 것을 전부 떨어뜨린다.</summary>
        public void RollDrops(Vector2 position)
        {
            if (_table == null || _pool == null) return;

            _table.Roll(_rolled);

            foreach (PickupDefinition definition in _rolled)
            {
                _pool.Drop(position, definition);
            }
        }
    }
}
