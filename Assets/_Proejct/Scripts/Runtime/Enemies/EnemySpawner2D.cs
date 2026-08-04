using System;
using System.Collections.Generic;
using System.Threading;
using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.Pool;

public sealed class EnemySpawner2D : MonoBehaviour
{
    [Title("대상")]
    [SerializeField, Required("적 프리팹")] private Enemy2D _enemyPrefab;
    [SerializeField, LabelText("추적 대상 (비우면 자동 탐색)")] private Transform _target;

    [Title("스폰")]
    [SerializeField, LabelText("스폰 간격 (sec)")] private float _spawnInterval = 0.8f;
    [SerializeField, LabelText("스폰 거리")] private float _spawnRadius = 10f;
    [SerializeField, LabelText("동시 최대 수")] private int _maxAlive = 40;

    [Title("풀")]
    [SerializeField, LabelText("기본 용량")] private int _poolDefaultCapacity = 32;
    [SerializeField, LabelText("최대 크기")] private int _poolMaxSize = 256;

    [Title("디버그")]
    [ShowInInspector, ReadOnly, LabelText("자동 스폰")]
    private bool _autoSpawnEnabled = true;

    [ShowInInspector, ReadOnly, LabelText("현재 살아있는 수")]
    private int AliveCount => _activeEnemies.Count;

    private readonly List<Enemy2D> _activeEnemies = new();
    private ObjectPool<Enemy2D> _pool;
    private Action<Enemy2D> _releaseCallback;
    private Transform _enemyRoot;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        _enemyRoot = new GameObject("EnemyPool").transform;

        // 델리게이트를 한 번만 만들어 재사용한다(스폰마다 GC 할당 방지).
        _releaseCallback = ReleaseEnemy;

        _pool = new ObjectPool<Enemy2D>(
            createFunc: CreateEnemy,
            actionOnGet: OnGetEnemy,
            actionOnRelease: OnReleaseEnemy,
            actionOnDestroy: OnDestroyEnemy,
            collectionCheck: true,
            defaultCapacity: _poolDefaultCapacity,
            maxSize: _poolMaxSize);
    }

    private void OnEnable()
    {
        if (_target == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) _target = player.transform;
        }

        _cts = new CancellationTokenSource();
        _ = SpawnLoopAsync(_cts.Token);
    }

    private void OnDisable()
    {
        // 루프는 토큰 취소로만 끝난다. 취소 후 Dispose 해서 CTS를 남기지 않는다.
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private void OnDestroy()
    {
        _pool?.Dispose();
        if (_enemyRoot != null) Destroy(_enemyRoot.gameObject);
    }

    private async Awaitable SpawnLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Awaitable.WaitForSecondsAsync(_spawnInterval, token);

                if (!_autoSpawnEnabled) continue;
                if (_enemyPrefab == null || _target == null) continue;
                if (_activeEnemies.Count >= _maxAlive) continue;

                SpawnOne();
            }
        }
        catch (OperationCanceledException)
        {
            // OnDisable에 의한 정상 종료.
        }
    }

    [Button, LabelText("자동 스폰 켜기 / 끄기")]
    private void ToggleAutoSpawn()
    {
        _autoSpawnEnabled = !_autoSpawnEnabled;
    }

    [Button, LabelText("적 전부 제거")]
    private void ClearAllEnemies()
    {
        if (!Application.isPlaying) return;

        // 반납이 리스트를 수정하므로 뒤에서부터 훑는다.
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            ReleaseEnemy(_activeEnemies[i]);
        }
    }

    private void SpawnOne()
    {
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        Vector2 offset = new(Mathf.Cos(angle) * _spawnRadius, Mathf.Sin(angle) * _spawnRadius);

        Enemy2D enemy = _pool.Get();
        _activeEnemies.Add(enemy);
        enemy.Spawn((Vector2)_target.position + offset, _target);
    }

    private Enemy2D CreateEnemy()
    {
        Enemy2D enemy = Instantiate(_enemyPrefab, _enemyRoot);
        enemy.SetReleaseCallback(_releaseCallback);
        return enemy;
    }

    private void ReleaseEnemy(Enemy2D enemy)
    {
        // 사망과 일괄 제거가 겹쳐도 같은 개체를 두 번 반납하지 않게 막는다.
        if (!_activeEnemies.Remove(enemy)) return;
        _pool.Release(enemy);
    }

    private static void OnGetEnemy(Enemy2D enemy) => enemy.gameObject.SetActive(true);

    private static void OnReleaseEnemy(Enemy2D enemy) => enemy.gameObject.SetActive(false);

    private static void OnDestroyEnemy(Enemy2D enemy)
    {
        if (enemy != null) Destroy(enemy.gameObject);
    }
}
