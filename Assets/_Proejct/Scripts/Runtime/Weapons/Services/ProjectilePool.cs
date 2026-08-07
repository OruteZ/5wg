using System.Collections.Generic;
using Alchemy.Inspector;
using FiveWG.Combat;
using FiveWG.Core;
using FiveWG.Stage;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 프리팹별 투사체 풀. 6개 무기가 서로 다른 투사체를 쓰므로 풀을 무기가 아닌 이곳이 공유 소유한다.
    ///
    /// 프리팹마다 PrefabPool을 하나씩 두고, 이 클래스는 "어느 프리팹의 풀인가"만 고른다.
    /// 스테이지가 끝나면 날아가던 탄을 전부 회수한다 — 적이 사라진 뒤 탄만 남아 떠다니지 않게.
    /// </summary>
    public sealed class ProjectilePool : MonoBehaviour
    {
        [Title("풀")]
        [SerializeField, LabelText("프리팹당 기본 용량")] private int _defaultCapacity = 32;
        [SerializeField, LabelText("프리팹당 최대 크기")] private int _maxSize = 256;

        [Title("스테이지")]
        [SerializeField, LabelText("디렉터 (비우면 자동 탐색). 종료 시 투사체 회수")]
        private StageDirector _director;

        [ShowInInspector, ReadOnly, LabelText("등록된 프리팹 수")]
        private int PrefabCount => _pools.Count;

        [ShowInInspector, ReadOnly, LabelText("날아다니는 탄 수")]
        private int ActiveCount
        {
            get
            {
                int total = 0;
                foreach (PrefabPool<Projectile> pool in _pools.Values) total += pool.ActiveCount;
                return total;
            }
        }

        private readonly Dictionary<Projectile, PrefabPool<Projectile>> _pools = new();
        private bool _isSubscribed;

        private void Start()
        {
            // WeaponHandler가 런타임에 붙일 수도 있어 인스펙터 연결에 기대지 않는다.
            if (_director == null) _director = SceneServices.Instance.Director;

            if (_director != null)
            {
                _director.OnStateChanged += HandleStageStateChanged;
                _isSubscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (_isSubscribed && _director != null) _director.OnStateChanged -= HandleStageStateChanged;

            foreach (PrefabPool<Projectile> pool in _pools.Values)
            {
                pool.Dispose();
            }

            _pools.Clear();
        }

        public Projectile Get(Projectile prefab)
        {
            if (prefab == null) return null;

            if (!_pools.TryGetValue(prefab, out PrefabPool<Projectile> pool))
            {
                pool = new PrefabPool<Projectile>(prefab, $"{prefab.name}Pool", _defaultCapacity, _maxSize);
                _pools.Add(prefab, pool);
            }

            return pool.Get();
        }

        [Button, LabelText("투사체 전부 회수")]
        public void ReleaseAll()
        {
            if (!Application.isPlaying) return;

            foreach (PrefabPool<Projectile> pool in _pools.Values)
            {
                pool.ReleaseAll();
            }
        }

        private void HandleStageStateChanged(StageState state)
        {
            if (state is StageState.Cleared or StageState.Failed) ReleaseAll();
        }
    }
}
