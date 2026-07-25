using System;
using System.Threading;
using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.Pool;

namespace FiveWG.Prototype
{
    /// <summary>
    /// 플레이어 주변 링 위에 적을 주기적으로 스폰한다. 대기는 Unity 6의 Awaitable + CancellationToken으로 처리한다.
    /// </summary>
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

        [ShowInInspector, ReadOnly, LabelText("현재 살아있는 수")]
        private int AliveCount => _pool?.CountActive ?? 0;

        private ObjectPool<Enemy2D> _pool;
        private Action<Enemy2D> _releaseCallback;
        private Transform _enemyRoot;
        private CancellationTokenSource _cts;

        private void Awake()
        {
            _enemyRoot = new GameObject("EnemyPool").transform;
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
                PlayerMovement2D player = FindFirstObjectByType<PlayerMovement2D>();
                if (player != null) _target = player.transform;
            }

            _cts = new CancellationTokenSource();
            _ = SpawnLoopAsync(_cts.Token);
        }

        private void OnDisable()
        {
            // 루프는 토큰 취소로만 끝난다. 취소 후 반드시 Dispose 해서 CTS를 남기지 않는다.
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
            if (_enemyRoot != null) Destroy(_enemyRoot.gameObject);
        }

        /// <summary>
        /// 데드락 불가: 블로킹 대기(.Result/.Wait)가 없고 락도 없으며, 모든 대기가 취소 토큰을 받는다.
        /// </summary>
        private async Awaitable SpawnLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Awaitable.WaitForSecondsAsync(_spawnInterval, token);

                    if (_enemyPrefab == null || _target == null) continue;
                    if (_pool.CountActive >= _maxAlive) continue;

                    SpawnOne();
                }
            }
            catch (OperationCanceledException)
            {
                // OnDisable에 의한 정상 종료.
            }
        }

        [Button, LabelText("지금 한 마리 스폰")]
        private void SpawnOne()
        {
            if (!Application.isPlaying || _pool is null || _enemyPrefab == null || _target == null) return;

            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector2 offset = new(Mathf.Cos(angle) * _spawnRadius, Mathf.Sin(angle) * _spawnRadius);

            Enemy2D enemy = _pool.Get();
            enemy.Spawn((Vector2)_target.position + offset, _target);
        }

        private Enemy2D CreateEnemy()
        {
            Enemy2D enemy = Instantiate(_enemyPrefab, _enemyRoot);
            enemy.SetReleaseCallback(_releaseCallback);
            return enemy;
        }

        private void ReleaseEnemy(Enemy2D enemy) => _pool.Release(enemy);

        private static void OnGetEnemy(Enemy2D enemy) => enemy.gameObject.SetActive(true);

        private static void OnReleaseEnemy(Enemy2D enemy) => enemy.gameObject.SetActive(false);

        private static void OnDestroyEnemy(Enemy2D enemy)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }
    }
}
