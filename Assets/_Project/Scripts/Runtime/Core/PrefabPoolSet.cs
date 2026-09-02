using System;
using System.Collections.Generic;

namespace FiveWG.Core
{
    /// <summary>
    /// "프리팹 여러 종류를 쓰는 한 부류"의 풀. <see cref="PrefabPool{T}"/>를 프리팹별로 하나씩 들고
    /// "어느 프리팹의 풀인가"만 고른다.
    ///
    /// 무기마다 탄이 다르고 장판도 다르므로 이 묶음이 두 부류에 똑같이 필요하다.
    /// MonoBehaviour가 아니라 소유자가 필드로 갖는다 — 씬 컴포넌트를 부류마다 하나씩 늘리지 않으려고.
    /// </summary>
    public sealed class PrefabPoolSet<T> : IDisposable where T : UnityEngine.Component, IPooledObject<T>
    {
        private readonly Dictionary<T, PrefabPool<T>> _pools = new();
        private readonly int _defaultCapacity;
        private readonly int _maxSize;
        private readonly Action<T> _onCreate;

        /// <param name="onCreate">인스턴스를 새로 만들 때 1회 호출. 프리팹별 풀에 그대로 넘긴다.</param>
        public PrefabPoolSet(int defaultCapacity = 32, int maxSize = 256, Action<T> onCreate = null)
        {
            _defaultCapacity = defaultCapacity;
            _maxSize = maxSize;
            _onCreate = onCreate;
        }

        public int PrefabCount => _pools.Count;

        public int ActiveCount
        {
            get
            {
                int total = 0;
                foreach (PrefabPool<T> pool in _pools.Values) total += pool.ActiveCount;
                return total;
            }
        }

        public T Get(T prefab)
        {
            if (prefab == null) return null;

            if (!_pools.TryGetValue(prefab, out PrefabPool<T> pool))
            {
                pool = new PrefabPool<T>(prefab, $"{prefab.name}Pool", _defaultCapacity, _maxSize, _onCreate);
                _pools.Add(prefab, pool);
            }

            return pool.Get();
        }

        /// <summary>
        /// 꺼내져 있는 개체를 전부 담는다. 복사본이라 순회 도중 반납해도 안전하다.
        /// 호출자가 리스트를 재사용하므로 할당은 생기지 않는다.
        /// </summary>
        public void CollectActive(List<T> results)
        {
            results.Clear();
            foreach (PrefabPool<T> pool in _pools.Values)
            {
                results.AddRange(pool.Active);
            }
        }

        public void ReleaseAll()
        {
            foreach (PrefabPool<T> pool in _pools.Values) pool.ReleaseAll();
        }

        public void Dispose()
        {
            foreach (PrefabPool<T> pool in _pools.Values) pool.Dispose();
            _pools.Clear();
        }
    }
}
