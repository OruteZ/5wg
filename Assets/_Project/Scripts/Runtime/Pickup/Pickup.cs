using System;
using Alchemy.Inspector;
using FiveWG.Core;
using UnityEngine;

namespace FiveWG.Pickup
{
    /// <summary>
    /// 필드에 떨어진 티켓·아이템. 플레이어가 직접 닿아야 먹힌다.
    ///
    /// 경험치 오브와 다른 점이다. 오브는 가까이 가면 끌려오지만 픽업은 그 자리에 가만히 있는다.
    /// 콜라이더 대신 거리로 보는 이유는 오브와 같다 — 판정이 원 하나뿐이라 물리 엔진에 얹을 게 없다.
    ///
    /// 시간이 지나도 사라지지 않는다. 안 주운 픽업은 판이 끝날 때까지 남는다.
    /// </summary>
    public sealed class Pickup : MonoBehaviour, IPooledObject<Pickup>
    {
        [Title("습득")]
        [SerializeField, LabelText("습득 거리")]
        [Tooltip("플레이어와 이만큼 가까워지면 주운 것으로 친다.\n" +
                 "플레이어 반지름 + 픽업 반지름으로 잡는다. 닿았는데 안 먹으면 이 값을 올린다.")]
        private float _contactRadius = 0.5f;

        [Title("스폰")]
        [SerializeField, LabelText("흩어짐")]
        [Tooltip("떨어질 때 이 반경 안에서 무작위로 어긋난다.")]
        private float _scatter = 0.35f;

        [ShowInInspector, ReadOnly, LabelText("종류")]
        private string DefinitionDebug => _definition == null ? "-" : _definition.DisplayName;

        private Action<Pickup> _release;
        private Transform _target;
        private PickupCollector _receiver;
        private PickupDefinition _definition;
        private bool _isDespawned;

        /// <summary>필드에서 내려가기 직전에 발행된다. 풀이 이걸로 개수 목록을 맞춘다.</summary>
        public event Action<Pickup> OnDespawned;

        /// <summary>풀 생성 시 1회만 호출한다.</summary>
        public void SetReleaseCallback(Action<Pickup> release) => _release = release;

        /// <summary>풀에서 꺼낼 때마다 호출한다. 재사용되므로 상태를 전부 되돌린다.</summary>
        public void Spawn(Vector2 position, PickupDefinition definition, Transform target, PickupCollector receiver)
        {
            // 한자리에서 여럿이 죽어도 픽업이 한 점에 겹치지 않게 조금씩 흩는다.
            transform.position = position + UnityEngine.Random.insideUnitCircle * _scatter;

            _definition = definition;
            _target = target;
            _receiver = receiver;
            _isDespawned = false;
        }

        /// <summary>줍히지 않은 채로 필드에서 내린다. 개수 상한과 스테이지 종료가 쓴다.</summary>
        public void Despawn()
        {
            if (_isDespawned) return;
            _isDespawned = true;

            OnDespawned?.Invoke(this);
            _release?.Invoke(this);
        }

        // 거리 자체는 쓸 데가 없어서 제곱끼리 비교한다. 프레임마다 제곱근을 뽑지 않으려고.
        private void Update()
        {
            if (_isDespawned || _target == null) return;

            Vector2 toTarget = (Vector2)_target.position - (Vector2)transform.position;
            if (toTarget.sqrMagnitude <= _contactRadius * _contactRadius) Collect();
        }

        private void Collect()
        {
            if (_isDespawned) return;

            // 넘기는 게 먼저다. 내려간 뒤에는 다음 스폰이 _definition을 덮어쓸 수 있다.
            if (_definition != null) _receiver?.Receive(_definition);

            Despawn();
        }
    }
}
