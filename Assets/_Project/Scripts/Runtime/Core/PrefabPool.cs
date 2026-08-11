using System.Collections.Generic;
using System;
using UnityEngine.Pool;
using UnityEngine;

namespace FiveWG.Core
{
    /// <summary>
    /// 풀에서 재사용되는 개체. 생성 시 반납 콜백을 한 번 받아 스스로 반납한다.
    /// 자기 타입을 인자로 받는 이유는 풀이 구체 타입을 그대로 돌려받아야 하기 때문.
    /// </summary>
    public interface IPooledObject<T> where T : Component
    {
        /// <summary>풀 생성 시 1회만 호출된다.</summary>
        void SetReleaseCallback(Action<T> release);
    }

    /// <summary>
    /// 프리팹 하나를 재사용하는 풀.
    ///
    /// 담당은 딱 하나 — 인스턴스의 수명과 활성 목록. "언제 꺼내고 언제 반납하는가"는 소유자가 정한다.
    /// 스폰 주기·회수 정책 같은 걸 여기로 끌어오면 세 곳의 차이가 파라미터로 새어나와 원래대로 돌아간다.
    ///
    /// MonoBehaviour가 아니라 소유자가 필드로 갖는다(상속이 아닌 합성).
    /// EnemySpawner처럼 "풀이 아닌데 풀을 가진" 쪽이 있어서 is-a로 묶으면 안 되기 때문이다.
    /// </summary>
    public sealed class PrefabPool<T> : IDisposable where T : Component, IPooledObject<T>
    {
        private readonly List<T> _active = new();
        private readonly ObjectPool<T> _pool;
        private readonly T _prefab;
        private readonly Action<T> _onCreate;

        // 델리게이트를 한 번만 만들어 재사용한다(꺼낼 때마다 GC 할당이 생기지 않게).
        private readonly Action<T> _releaseCallback;

        private Transform _root;
        private bool _isDisposed;

        /// <summary>지금 꺼내져 있는 개체 수.</summary>
        public int ActiveCount => _active.Count;

        /// <summary>지금 꺼내져 있는 개체들. 순회 중 반납하면 목록이 바뀌므로 뒤에서부터 훑어야 한다.</summary>
        public IReadOnlyList<T> Active => _active;

        /// <param name="rootName">하이라키에서 인스턴스를 담아둘 부모 이름. 비우면 "{프리팹}Pool".</param>
        /// <param name="onCreate">인스턴스를 새로 만들 때 1회 호출. 이벤트 구독처럼 개체당 한 번만 할 일에 쓴다.</param>
        public PrefabPool(T prefab, string rootName = null, int defaultCapacity = 32, int maxSize = 256,
            Action<T> onCreate = null)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));

            _prefab = prefab;
            _onCreate = onCreate;
            _releaseCallback = Release;

            // 하이라키가 인스턴스로 더러워지지 않도록 부모 하나에 모아둔다.
            _root = new GameObject(string.IsNullOrEmpty(rootName) ? $"{prefab.name}Pool" : rootName).transform;

            _pool = new ObjectPool<T>(
                createFunc: Create,
                actionOnGet: instance => instance.gameObject.SetActive(true),
                actionOnRelease: instance => instance.gameObject.SetActive(false),
                actionOnDestroy: instance =>
                {
                    if (instance != null) UnityEngine.Object.Destroy(instance.gameObject);
                },
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        public T Get()
        {
            T instance = _pool.Get();
            _active.Add(instance);
            return instance;
        }

        public void Release(T instance)
        {
            // 스스로 반납한 개체를 일괄 회수가 또 반납하는 상황을 여기 한 곳에서 막는다.
            if (!_active.Remove(instance)) return;
            _pool.Release(instance);
        }

        public void ReleaseAll()
        {
            // 반납이 목록을 수정하므로 뒤에서부터 훑는다.
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Release(_active[i]);
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _active.Clear();
            _pool.Dispose();

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root.gameObject);
                _root = null;
            }
        }

        private T Create()
        {
            T instance = UnityEngine.Object.Instantiate(_prefab, _root);
            instance.SetReleaseCallback(_releaseCallback);
            _onCreate?.Invoke(instance);
            return instance;
        }
    }
}
