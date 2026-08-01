using System;
using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 프리팹별 투사체 풀. 6개 무기가 서로 다른 투사체를 쓰므로 풀을 무기가 아닌 이곳이 공유 소유한다.
///
/// 활성 투사체의 수명도 여기서 한 번에 돌린다. 투사체마다 Update를 두면 개체 수만큼
/// 네이티브↔매니지드 전환이 생기는데, 탄막 게임은 그 개체 수가 곧 부하다.
/// </summary>
public sealed class ProjectilePool : MonoBehaviour
{
    [Title("풀")]
    [SerializeField, LabelText("프리팹당 기본 용량")] private int _defaultCapacity = 32;
    [SerializeField, LabelText("프리팹당 최대 크기")] private int _maxSize = 256;

    [ShowInInspector, ReadOnly, LabelText("등록된 프리팹 수")]
    private int PrefabCount => _pools?.Count ?? 0;

    [ShowInInspector, ReadOnly, LabelText("활성 투사체 수")]
    private int ActiveCount => _active?.Count ?? 0;

    private readonly Dictionary<Projectile2D, ObjectPool<Projectile2D>> _pools = new();
    private readonly List<Projectile2D> _active = new(128);
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
        _active.Clear();

        if (_root != null) Destroy(_root.gameObject);
    }

    private void Update()
    {
        // 일시정지 중에는 deltaTime이 0이라 수명이 그대로 멈춘다.
        float deltaTime = Time.deltaTime;

        // 수명이 다한 투사체는 이 안에서 반납되며 목록 끝의 원소가 그 자리로 당겨온다.
        // 뒤에서부터 돌면 당겨온 원소는 이미 지나온 자리라 같은 프레임에 두 번 처리되지 않는다.
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            _active[i].TickLifetime(deltaTime);
        }
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
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyProjectile,
            collectionCheck: true,
            defaultCapacity: _defaultCapacity,
            maxSize: _maxSize);

        return pool;
    }

    private void OnGet(Projectile2D projectile)
    {
        projectile.gameObject.SetActive(true);
        AddActive(projectile);
    }

    private void OnRelease(Projectile2D projectile)
    {
        RemoveActive(projectile);
        projectile.gameObject.SetActive(false);
    }

    private void OnDestroyProjectile(Projectile2D projectile)
    {
        if (projectile == null) return;

        RemoveActive(projectile);
        Destroy(projectile.gameObject);
    }

    private void AddActive(Projectile2D projectile)
    {
        if (projectile.ActiveIndex >= 0) return;

        projectile.ActiveIndex = _active.Count;
        _active.Add(projectile);
    }

    /// <summary>마지막 원소를 빈자리로 당겨오는 O(1) 제거. 활성 목록의 순서는 의미가 없다.</summary>
    private void RemoveActive(Projectile2D projectile)
    {
        int index = projectile.ActiveIndex;
        if (index < 0 || index >= _active.Count) return;

        int last = _active.Count - 1;

        if (index != last)
        {
            _active[index] = _active[last];
            _active[index].ActiveIndex = index;
        }

        _active.RemoveAt(last);
        projectile.ActiveIndex = -1;
    }
}
