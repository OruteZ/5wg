using System.Collections.Generic;
using Alchemy.Inspector;
using FiveWG.Core;
using FiveWG.Player;
using FiveWG.Stage;
using UnityEngine;

namespace FiveWG.Pickup
{
    /// <summary>
    /// 픽업의 주인. 종류마다 풀을 하나씩 두고 드랍 요청을 받아 꺼내 놓는다.
    ///
    /// 픽업은 스스로 사라지지 않으니 개수 상한이 유일한 정리 수단이다.
    /// 상한에 닿으면 가장 오래된 것부터 지운다. 방금 죽인 적의 드랍이 없어지는 것보다
    /// 한참 전에 지나친 게 없어지는 쪽이 덜 억울하다.
    ///
    /// 스테이지를 구독하는 방향은 Pickup → Stage 한 방향이다. 디렉터는 픽업을 모른다.
    /// </summary>
    public sealed class PickupPool : MonoBehaviour
    {
        [Title("참조 (비우면 자동으로 찾는다)")]
        [SerializeField, LabelText("수집자")] private PickupCollector _collector;

        [SerializeField, LabelText("디렉터")]
        [Tooltip("판이 끝날 때 남은 픽업을 치우려고 구독한다.")]
        private StageDirector _director;

        [Title("풀 (픽업 종류마다 하나씩 만든다)")]
        [SerializeField, LabelText("기본 개수")] private int _defaultCapacity = 16;
        [SerializeField, LabelText("최대 개수")] private int _maxSize = 128;

        [SerializeField, LabelText("동시 상한"), Min(1)]
        [Tooltip("필드에 한 번에 떠 있을 수 있는 픽업 수. 픽업은 저절로 사라지지 않아서 이게 유일한 정리 수단이다.\n" +
                 "넘으면 가장 먼저 떨어진 것부터 지운다.")]
        private int _maxAlive = 200;

        [ShowInInspector, ReadOnly, LabelText("필드에 있는 수")]
        private int AliveCount => _alive.Count;

        private readonly Dictionary<PickupDefinition, PrefabPool<Pickup>> _pools = new();

        // 풀이 종류별로 갈라져 있어서 "전체에서 가장 오래된 것"을 풀에 물어볼 수가 없다.
        // 그래서 떨어뜨린 순서대로 여기 따로 담아 둔다. 목록은 Pickup.OnDespawned가 맞춰 준다.
        private readonly List<Pickup> _alive = new();

        private Transform _targetTransform;
        private bool _isSubscribed;

        private void Start()
        {
            if (_director == null) _director = SceneServices.Instance.Director;

            PlayerController player = SceneServices.Instance.Player;
            if (player != null)
            {
                _targetTransform = player.transform;
                if (_collector == null) _collector = player.GetComponent<PickupCollector>();
            }

            if (_collector == null)
            {
                Debug.LogWarning(
                    $"[{nameof(PickupPool)}] {nameof(PickupCollector)}를 찾지 못했다. 픽업을 주울 수 없다.", this);
            }

            if (_director != null)
            {
                _director.OnStateChanged += HandleStageStateChanged;
                _isSubscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (_isSubscribed && _director != null) _director.OnStateChanged -= HandleStageStateChanged;

            foreach (PrefabPool<Pickup> pool in _pools.Values)
            {
                pool.Dispose();
            }

            _pools.Clear();
            _alive.Clear();
        }

        /// <summary>픽업 하나를 그 자리에 떨어뜨린다. 무엇을 떨어뜨릴지는 부르는 쪽이 정한다.</summary>
        public void Drop(Vector2 position, PickupDefinition definition)
        {
            if (definition == null || definition.Prefab == null || _collector == null) return;

            if (_alive.Count >= _maxAlive) _alive[0].Despawn();

            Pickup pickup = GetPool(definition).Get();
            _alive.Add(pickup);
            pickup.Spawn(position, definition, _targetTransform, _collector);
        }

        [Button, LabelText("픽업 전부 치우기")]
        public void ReleaseAll()
        {
            if (!Application.isPlaying) return;

            // Despawn이 목록을 건드리므로 뒤에서부터 훑는다.
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                _alive[i].Despawn();
            }
        }

        private PrefabPool<Pickup> GetPool(PickupDefinition definition)
        {
            if (_pools.TryGetValue(definition, out PrefabPool<Pickup> pool)) return pool;

            pool = new PrefabPool<Pickup>(
                definition.Prefab, $"{definition.name}Pool", _defaultCapacity, _maxSize,
                onCreate: pickup => pickup.OnDespawned += HandleDespawned);

            _pools.Add(definition, pool);
            return pool;
        }

        private void HandleDespawned(Pickup pickup) => _alive.Remove(pickup);

        private void HandleStageStateChanged(StageState state)
        {
            // 판이 끝난 뒤에 남은 픽업을 주워 자원이 더 들어가는 걸 막는다.
            if (state is StageState.Cleared or StageState.Failed) ReleaseAll();
        }
    }
}
