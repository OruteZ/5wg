using System;
using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 프리팹별 투사체 풀. 6개 무기가 서로 다른 투사체를 쓰므로 풀을 무기가 아닌 이곳이 공유 소유한다.
/// </summary>
public sealed class ProjectilePool : MonoBehaviour
{
    [Title("풀")]
    [SerializeField, LabelText("프리팹당 기본 용량")] private int _defaultCapacity = 32;
    [SerializeField, LabelText("프리팹당 최대 크기")] private int _maxSize = 256;

    [ShowInInspector, ReadOnly, LabelText("등록된 프리팹 수")]
    private int PrefabCount => _pools?.Count ?? 0;

    private readonly Dictionary<Projectile2D, ObjectPool<Projectile2D>> _pools = new();
    private Transform _root;

    private void Awake()
    {
        // 하이라키가 투사체로 더러워지지 않도록 부모 하나에 모아둔다.
        _root = new GameObject("ProjectilePool").transform;
    }

    private void OnDestroy()
    {
        foreach (ObjectPool<Projectile2D> pool in _pools.Values)
        {
            pool.Dispose();
        }

        _pools.Clear();
        if (_root != null) Destroy(_root.gameObject);
    }

    public Projectile2D Get(Projectile2D prefab)
    {
        if (prefab == null) return null;

        if (!_pools.TryGetValue(prefab, out ObjectPool<Projectile2D> pool))
        {
            pool = CreatePool(prefab);
            _pools.Add(prefab, pool);
        }

        return pool.Get();
    }

    private ObjectPool<Projectile2D> CreatePool(Projectile2D prefab)
    {
        // 반납 델리게이트가 자기 풀을 참조해야 하므로 지역 변수를 캡처해 나중에 채운다.
        // 프리팹당 한 번만 만들어지므로 발사마다 할당이 생기지는 않는다.
        ObjectPool<Projectile2D> pool = null;
        Action<Projectile2D> release = projectile => pool.Release(projectile);

        pool = new ObjectPool<Projectile2D>(
            createFunc: () =>
            {
                Projectile2D projectile = Instantiate(prefab, _root);
                projectile.SetReleaseCallback(release);
                return projectile;
            },
            actionOnGet: projectile => projectile.gameObject.SetActive(true),
            actionOnRelease: projectile => projectile.gameObject.SetActive(false),
            actionOnDestroy: projectile =>
            {
                if (projectile != null) Destroy(projectile.gameObject);
            },
            collectionCheck: true,
            defaultCapacity: _defaultCapacity,
            maxSize: _maxSize);

        return pool;
    }
}
