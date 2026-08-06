using System.Threading;
using System;
using Alchemy.Inspector;
using FiveWG.Core;
using FiveWG.Player;
using FiveWG.Progression;
using UnityEngine;

namespace FiveWG.Enemies
{
    public sealed class EnemySpawner2D : MonoBehaviour
    {
        [Title("대상")]
        [SerializeField, Required("적 프리팹")] private Enemy2D _enemyPrefab;
        [SerializeField, LabelText("추적 대상 (비우면 자동 탐색)")] private Transform _target;

        [Title("보상")]
        [SerializeField, LabelText("경험치 오브 풀 (비우면 자동 탐색)")] private ExpOrbPool _expPool;

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
        private int AliveCount => _pool?.ActiveCount ?? 0;

        private PrefabPool<Enemy2D> _pool;
        private CancellationTokenSource _cts;

        private void Awake()
        {
            if (_enemyPrefab == null)
            {
                Debug.LogError($"[{nameof(EnemySpawner2D)}] 적 프리팹이 비어 있다. 스폰이 일어나지 않는다.", this);
                return;
            }

            // 적을 만드는 곳이 여기뿐이라 사망 구독도 생성 시 한 번만 건다. 개체는 풀과 함께 파괴된다.
            _pool = new PrefabPool<Enemy2D>(
                _enemyPrefab, "EnemyPool", _poolDefaultCapacity, _poolMaxSize,
                onCreate: enemy => enemy.OnDiedWithReward += HandleEnemyDied);
        }

        private void OnEnable()
        {
            if (_target == null)
            {
                PlayerController player = SceneServices.Instance.Player;
                if (player != null) _target = player.transform;
            }

            if (_expPool == null) _expPool = SceneServices.Instance.ExpOrbs;

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

        private void OnDestroy() => _pool?.Dispose();

        private async Awaitable SpawnLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Awaitable.WaitForSecondsAsync(_spawnInterval, token);

                    if (!_autoSpawnEnabled) continue;
                    if (_pool == null || _target == null) continue;
                    if (_pool.ActiveCount >= _maxAlive) continue;

                    SpawnOne();
                }
            }
            catch (OperationCanceledException)
            {
                // OnDisable에 의한 정상 종료.
            }
        }

        /// <summary>스폰 on/off. StageDirector가 스테이지 시작·종료에 맞춰 호출한다.</summary>
        public void SetSpawning(bool enabled) => _autoSpawnEnabled = enabled;

        [Button, LabelText("자동 스폰 켜기 / 끄기")]
        private void ToggleAutoSpawn()
        {
            _autoSpawnEnabled = !_autoSpawnEnabled;
        }

        [Button, LabelText("적 전부 제거")]
        public void ClearAll()
        {
            if (!Application.isPlaying) return;
            _pool?.ReleaseAll();
        }

        private void SpawnOne()
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector2 offset = new(Mathf.Cos(angle) * _spawnRadius, Mathf.Sin(angle) * _spawnRadius);

            Enemy2D enemy = _pool.Get();
            enemy.Spawn((Vector2)_target.position + offset, _target);
        }

        /// <summary>적이 죽은 자리에 경험치를 떨어뜨린다. 스포너는 오브의 동작을 모른다.</summary>
        private void HandleEnemyDied(Vector2 position, float reward)
        {
            if (_expPool != null) _expPool.Drop(position, reward);
        }
    }
}
