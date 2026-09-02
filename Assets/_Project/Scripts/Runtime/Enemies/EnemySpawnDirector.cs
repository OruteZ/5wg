using System;
using System.Collections.Generic;
using Alchemy.Inspector;
using BeatTemplate;
using FiveWG.Core;
using FiveWG.Pickup;
using FiveWG.Player;
using FiveWG.Progression;
using UnityEngine;

namespace FiveWG.Enemies
{
    /// <summary>
    /// <see cref="StageEnemyPlan"/>의 표를 시간축에 얹는 스포너.
    /// 평상시 추첨과 지정 웨이브가 동시에 돌고, 서로를 멈추지 않는다.
    ///
    /// 회차·웨이브·엘리트·보스는 **마디**로, 난이도 곡선은 **초**로 잰다.
    /// 곡 구조상의 위치는 BPM을 따라가야 하고, 초당 밸런스는 따라가면 안 되기 때문이다.
    /// </summary>
    public sealed class EnemySpawnDirector : EnemySpawnerBase
    {
        [Title("기획 표")]
        [SerializeField, Required("스테이지 적 편성 에셋이 없으면 아무것도 스폰되지 않는다.")]
        [LabelText("스테이지 적 편성")]
        private StageEnemyPlan _plan;

        [Title("참조 (비우면 자동 탐색)")]
        [SerializeField, LabelText("추적 대상")] private Transform _target;
        [SerializeField, LabelText("박자 클록")] private BpmClock _clock;
        [SerializeField, LabelText("경험치 오브 풀")] private ExpOrbPool _expPool;
        [SerializeField, LabelText("픽업 드랍")] private PickupDropper _dropper;

        [Title("풀")]
        [SerializeField, LabelText("종류당 기본 용량")] private int _poolDefaultCapacity = 64;
        [SerializeField, LabelText("종류당 최대 크기")] private int _poolMaxSize = 320;

        [Title("디버그")]
        [ShowInInspector, ReadOnly, LabelText("자동 스폰")] private bool _spawnEnabled;
        [ShowInInspector, ReadOnly, LabelText("경과 (분:초)")]
        private string ElapsedLabel => $"{Mathf.FloorToInt(_elapsedSec / 60f)}:{_elapsedSec % 60f:00.0}";

        [ShowInInspector, ReadOnly, LabelText("살아있는 수")] private int AliveCount => _pool?.ActiveCount ?? 0;
        [ShowInInspector, ReadOnly, LabelText("현재 회차")] private string RoundLabel => _currentRound?.Label ?? "-";
        [ShowInInspector, ReadOnly, LabelText("스폰율 (마리/초)")]
        private float SpawnRateNow => _pacing != null ? _pacing.SampleSpawnRate(_elapsedSec) : 0f;

        [ShowInInspector, ReadOnly, LabelText("기본 적 HP")]
        private float BaseHealthNow => _pacing != null ? _pacing.SampleBaseHealth(_elapsedSec) : 0f;

        [ShowInInspector, ReadOnly, LabelText("남은 엘리트")] private int EliteRemaining =>
            _plan != null && _plan.Elite != null ? Mathf.Max(0, _plan.Elite.Count - _elitesSpawned) : 0;

        [ShowInInspector, ReadOnly, LabelText("경과 마디")] private float ElapsedBarsDebug => ElapsedBars;

        [ShowInInspector, ReadOnly, LabelText("보스")]
        private string BossLabel => !_bossSpawned ? "대기" : IsBossAlive ? "교전 중" : "처치됨";

        private readonly List<Enemy> _activeBuffer = new();
        private readonly List<StageEnemyPlan.Weight> _candidates = new();

        private PrefabPoolSet<Enemy> _pool;
        private RunPacingPlan _pacing;
        private StageEnemyPlan.Round _currentRound;
        private Enemy _bossInstance;
        private bool[] _waveFired;
        private float _elapsedSec;
        private float _spawnCredit;
        private int _elitesSpawned;
        private bool _bossSpawned;

        /// <summary>엘리트 처치. 보상인 상자 시스템이 아직 없어 받는 쪽이 없다.</summary>
        public event Action<Vector2> OnEliteDefeated;

        /// <summary>보스 처치. 승리 판정은 스테이지 쪽 일이라 알리기만 한다.</summary>
        public event Action<Vector2> OnBossDefeated;

        public bool IsBossAlive { get; private set; }

        /// <summary>
        /// 경과 시간을 지금 BPM으로 환산한 마디. 클록의 박 인덱스를 직접 읽지 않는 것은
        /// <c>BpmClock.ElapsedSec</c>가 정지 상태에서 0을 돌려주고, 시간 건너뛰기가
        /// 클록을 되감을 수 없기 때문이다.
        /// </summary>
        private float ElapsedBars
        {
            get
            {
                if (_clock == null || _pacing == null || _pacing.BeatsPerBar <= 0) return 0f;
                return _elapsedSec * (_clock.Bpm / 60f) / _pacing.BeatsPerBar;
            }
        }

        private void Awake()
        {
            if (_plan == null)
            {
                Debug.LogError(
                    $"[{nameof(EnemySpawnDirector)}] 스테이지 적 편성이 비어 있다. 적이 하나도 나오지 않는다.", this);
                return;
            }

            _pacing = _plan.Pacing;
            if (_pacing == null)
            {
                Debug.LogError(
                    $"[{nameof(EnemySpawnDirector)}] \"{_plan.name}\"에 페이싱이 연결되지 않았다. " +
                    "곡선·밀도 상한이 없어 적이 나오지 않는다.", this);
                return;
            }

            _pool = new PrefabPoolSet<Enemy>(
                _poolDefaultCapacity, _poolMaxSize,
                onCreate: enemy => enemy.OnDiedWithReward += (position, reward) => HandleEnemyDied(enemy, position, reward));

            _waveFired = new bool[_plan.Waves?.Length ?? 0];
        }

        private void OnEnable()
        {
            if (_target == null)
            {
                PlayerController player = SceneServices.Instance.Player;
                if (player != null) _target = player.transform;
            }

            if (_clock == null) _clock = SceneServices.Instance.Clock;
            if (_expPool == null) _expPool = SceneServices.Instance.ExpOrbs;
            if (_dropper == null) _dropper = SceneServices.Instance.Dropper;

            if (_target == null)
            {
                Debug.LogError(
                    $"[{nameof(EnemySpawnDirector)}] 추적 대상을 찾지 못했다. 스폰 위치를 정할 수 없어 적이 나오지 않는다.",
                    this);
            }

            if (_clock == null)
            {
                Debug.LogError(
                    $"[{nameof(EnemySpawnDirector)}] BpmClock을 찾지 못했다. 경과 마디가 0에 멈춰 " +
                    "회차·웨이브·엘리트·보스가 하나도 나오지 않는다.", this);
            }

            WarnIfEliteMissing();
            WarnIfBossMissing();
        }

        private void OnDestroy() => _pool?.Dispose();

        // 시간축은 건드리지 않는다. 상점처럼 전투만 잠깐 멈췄다 재개할 때 표가 처음으로 돌아가지 않게.
        public override void SetSpawning(bool enabled) => _spawnEnabled = enabled;

        [Button, LabelText("적 전부 제거")]
        public override void ClearAll()
        {
            if (!Application.isPlaying) return;

            _pool?.ReleaseAll();

            // 일괄 회수는 사망 이벤트를 타지 않으므로 보스 상태를 여기서 같이 내린다.
            _bossInstance = null;
            IsBossAlive = false;
        }

        /// <summary>시간축을 앞으로 감는다. 지나친 웨이브·엘리트는 나가지 않은 것으로 친다.</summary>
        [Button, LabelText("시간 건너뛰기 (분)")]
        private void SkipToMinute(float minute)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"[{nameof(EnemySpawnDirector)}] 플레이 중에만 쓸 수 있다.", this);
                return;
            }

            _elapsedSec = Mathf.Max(0f, minute * 60f);
            _spawnCredit = 0f;

            float bars = ElapsedBars;
            _currentRound = _plan != null ? _plan.GetRound(bars) : null;

            StageEnemyPlan.Wave[] waves = _plan?.Waves;
            if (waves != null && _waveFired != null && _waveFired.Length == waves.Length)
            {
                for (int i = 0; i < waves.Length; i++)
                {
                    _waveFired[i] = waves[i] != null && waves[i].Bar <= bars;
                }
            }

            StageEnemyPlan.EliteEntry elite = _plan?.Elite;
            if (elite != null)
            {
                _elitesSpawned = 0;
                while (_elitesSpawned < elite.Count && elite.GetSpawnBar(_elitesSpawned) <= bars)
                {
                    _elitesSpawned++;
                }
            }

            // 살아 있는 보스를 건드리면 두 마리가 되어 승리 조건이 꼬인다.
            StageEnemyPlan.BossEntry boss = _plan?.Boss;
            if (boss != null && !IsBossAlive)
            {
                _bossSpawned = boss.Enabled && boss.SpawnBar <= bars;
            }

            Debug.Log(
                $"[{nameof(EnemySpawnDirector)}] {ElapsedLabel}({bars:0.#}마디)로 건너뛰었다. " +
                $"회차 \"{RoundLabel}\", 기본 HP {BaseHealthNow:0.#}, 스폰율 {SpawnRateNow:0.##}/초, " +
                $"남은 엘리트 {EliteRemaining}마리.", this);
        }

        private void Update()
        {
            if (_plan == null || _pool == null) return;

            // 스폰이 꺼져도 돈다. 스테이지가 끝난 뒤 남은 적이 그 자리에서 굳지 않게.
            _pool.CollectActive(_activeBuffer);
            ApplyBeatPulse();
            RecycleStrayEnemies();

            if (!_spawnEnabled || _target == null) return;

            _elapsedSec += Time.deltaTime;
            _currentRound = _plan.GetRound(ElapsedBars);

            TickWaves();
            TickElites();
            TickBoss();
            TickNormalSpawn();
        }

        // 클록을 한 번만 읽어 나눠준다. 적마다 읽으면 프레임당 수백 번이 된다.
        private void ApplyBeatPulse()
        {
            if (_clock == null) return;

            // 제곱해서 정박 직후에 몰아준다. 선형이면 박이 아니라 그냥 늘 커졌다 작아지는 것으로 보인다.
            float decay = 1f - Mathf.Clamp01(_clock.BeatProgress);
            float pulse = decay * decay;

            foreach (Enemy enemy in _activeBuffer)
            {
                enemy.ApplyBeatPulse(pulse);
            }
        }

        private void RecycleStrayEnemies()
        {
            if (_target == null) return;

            Vector2 center = _target.position;
            float despawnSqr = _pacing.DespawnRadius * _pacing.DespawnRadius;

            foreach (Enemy enemy in _activeBuffer)
            {
                if (enemy.ImmuneToDespawn) continue;

                Vector2 offset = enemy.Position - center;
                if (offset.sqrMagnitude < despawnSqr) continue;

                // 멀어진 쪽의 반대편으로. 한 방향으로 계속 달리면 뒤에 처진 적이 앞으로 돌아온다.
                float angle = Mathf.Atan2(-offset.y, -offset.x);
                enemy.RecycleTo(center + PointOnSpawnCircle(angle));
            }
        }

        private void TickWaves()
        {
            StageEnemyPlan.Wave[] waves = _plan.Waves;
            if (waves == null) return;

            float bars = ElapsedBars;

            for (int i = 0; i < waves.Length; i++)
            {
                if (_waveFired[i]) continue;

                StageEnemyPlan.Wave wave = waves[i];
                if (wave == null || bars < wave.Bar) continue;

                _waveFired[i] = true;
                SpawnWave(wave);
            }
        }

        // 웨이브는 평상시 상한을 넘겨도 되고 하드 상한만 지킨다.
        private void SpawnWave(StageEnemyPlan.Wave wave)
        {
            int total = wave.TotalCount;
            if (total <= 0) return;

            int room = _pacing.HardCap - _pool.ActiveCount;
            if (room <= 0)
            {
                Debug.LogWarning(
                    $"[{nameof(EnemySpawnDirector)}] 웨이브 \"{wave.Label}\"가 하드 상한({_pacing.HardCap})에 막혀 나오지 못했다.",
                    this);
                return;
            }

            float baseAngle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float arc = _pacing.OneDirectionArcDegrees * Mathf.Deg2Rad;

            int spawned = 0;
            foreach (StageEnemyPlan.Wave.Entry entry in wave.Entries)
            {
                if (entry.Definition == null) continue;

                for (int i = 0; i < entry.Count && spawned < room; i++, spawned++)
                {
                    float angle = wave.Formation switch
                    {
                        EnemyFormation.Circle => baseAngle + Mathf.PI * 2f * spawned / total,
                        EnemyFormation.OneDirection => baseAngle + UnityEngine.Random.Range(-arc * 0.5f, arc * 0.5f),
                        _ => UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                    };

                    SpawnAt(entry.Definition, angle);
                }
            }

            if (spawned < total)
            {
                Debug.LogWarning(
                    $"[{nameof(EnemySpawnDirector)}] 웨이브 \"{wave.Label}\" {total}마리 중 {spawned}마리만 나왔다. " +
                    $"하드 상한({_pacing.HardCap})에 걸렸다.", this);
            }
        }

        // 상한을 보지 않는다. 한 판 3마리뿐이라 하나가 막히면 상자 보상이 3분의 1 사라진다.
        private void TickElites()
        {
            StageEnemyPlan.EliteEntry elite = _plan.Elite;
            if (elite == null || !elite.Enabled || _elitesSpawned >= elite.Count) return;
            if (ElapsedBars < elite.GetSpawnBar(_elitesSpawned)) return;

            _elitesSpawned++;

            if (elite.Definition == null)
            {
                Debug.LogError(
                    $"[{nameof(EnemySpawnDirector)}] 엘리트 정의가 비어 있어 {_elitesSpawned}번째 엘리트가 나오지 않았다.",
                    this);
                return;
            }

            SpawnAt(elite.Definition, UnityEngine.Random.Range(0f, Mathf.PI * 2f));
        }

        // 상한을 보지 않는다. 보스가 막혀 안 나오면 그 판은 이길 수 없는 판이 된다.
        private void TickBoss()
        {
            StageEnemyPlan.BossEntry boss = _plan.Boss;
            if (boss == null || !boss.Enabled || _bossSpawned) return;
            if (ElapsedBars < boss.SpawnBar) return;

            _bossSpawned = true;

            if (boss.Definition == null)
            {
                Debug.LogError(
                    $"[{nameof(EnemySpawnDirector)}] 보스 정의가 비어 있어 보스가 나오지 않았다. 승리 조건이 없다.", this);
                return;
            }

            _bossInstance = SpawnAt(boss.Definition, UnityEngine.Random.Range(0f, Mathf.PI * 2f));
            IsBossAlive = _bossInstance != null;

            Debug.Log($"[{nameof(EnemySpawnDirector)}] 보스 \"{boss.Definition.DisplayName}\" 등장 ({ElapsedLabel}).", this);
        }

        private void TickNormalSpawn()
        {
            int alive = _pool.ActiveCount;
            if (alive >= _pacing.HardCap) return;

            // 스폰율이 목표 적 수에서 역산된 값이라, 천장을 같이 보면 처치가 밀릴 때 저절로 쉬어간다.
            int ceiling = Mathf.Min(_pacing.SoftCap, Mathf.RoundToInt(_pacing.SampleTargetAlive(_elapsedSec)));
            if (alive >= ceiling) return;

            _spawnCredit += _pacing.SampleSpawnRate(_elapsedSec) * Time.deltaTime;

            // 소수점 아래를 이월한다. 버리면 스폰율이 프레임레이트에 끌려간다.
            while (_spawnCredit >= 1f && alive < ceiling)
            {
                _spawnCredit -= 1f;

                EnemyDefinition definition = PickWeighted();
                if (definition == null) break;

                SpawnAt(definition, UnityEngine.Random.Range(0f, Mathf.PI * 2f));
                alive++;
            }
        }

        /// <summary>
        /// 지금 회차의 가중치 표에서 하나 뽑는다. 가중치와 등장 시점을 둘 다 본다 —
        /// 앰프는 2곡(4분) 표에 있지만 등장 시점이 6분이라 그 사이에는 나오면 안 된다.
        /// </summary>
        private EnemyDefinition PickWeighted()
        {
            if (_currentRound?.Weights == null) return null;

            _candidates.Clear();
            float total = 0f;

            foreach (StageEnemyPlan.Weight weight in _currentRound.Weights)
            {
                if (weight.Definition == null || weight.Value <= 0f) continue;
                if (_elapsedSec < weight.Definition.UnlockTimeSec) continue;

                _candidates.Add(weight);
                total += weight.Value;
            }

            if (total <= 0f) return null;

            float roll = UnityEngine.Random.value * total;
            float sum = 0f;

            foreach (StageEnemyPlan.Weight weight in _candidates)
            {
                sum += weight.Value;
                if (roll < sum) return weight.Definition;
            }

            // 부동소수 오차로 마지막 칸을 넘길 수 있다.
            return _candidates[^1].Definition;
        }

        private Enemy SpawnAt(EnemyDefinition definition, float angle)
        {
            if (definition.Prefab == null)
            {
                Debug.LogError(
                    $"[{nameof(EnemySpawnDirector)}] \"{definition.DisplayName}\"에 프리팹이 없어 스폰하지 못했다.", this);
                return null;
            }

            EnemyStats stats = definition.Resolve(_pacing.SampleBaseHealth(_elapsedSec), _pacing.BaseMoveSpeed);

            Enemy enemy = _pool.Get(definition.Prefab);
            enemy.Spawn((Vector2)_target.position + PointOnSpawnCircle(angle), _target, stats);
            return enemy;
        }

        private Vector2 PointOnSpawnCircle(float angle)
        {
            float radius = _pacing.SpawnRadius + UnityEngine.Random.Range(0f, _pacing.SpawnRadiusJitter);
            return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }

        // 무엇이 얼마나 떨어지는지는 오브 풀과 드랍 표가 정한다.
        private void HandleEnemyDied(Enemy enemy, Vector2 position, float reward)
        {
            if (_expPool != null) _expPool.Drop(position, reward);
            if (_dropper != null) _dropper.RollDrops(position);

            if (enemy.IsElite) OnEliteDefeated?.Invoke(position);

            // 보스 프리팹을 일반 적과 공유해도 그 판의 보스 한 마리만 걸리도록 인스턴스로 비교한다.
            if (enemy != _bossInstance) return;

            _bossInstance = null;
            IsBossAlive = false;
            OnBossDefeated?.Invoke(position);
        }

        // 엘리트가 없으면 상자 보상이 통째로 사라진다. 3분이 돼서야 알게 되면 늦다.
        private void WarnIfEliteMissing()
        {
            StageEnemyPlan.EliteEntry elite = _plan?.Elite;
            if (elite == null || !elite.Enabled || elite.Definition != null) return;

            Debug.LogError(
                $"[{nameof(EnemySpawnDirector)}] \"{_plan.name}\"에 엘리트 정의가 없다. " +
                "엘리트가 나오지 않아 상자 보상도 없다.", this);
        }

        // 12분이 돼서야 알게 되면 늦다.
        private void WarnIfBossMissing()
        {
            StageEnemyPlan.BossEntry boss = _plan?.Boss;
            if (boss == null || !boss.Enabled || boss.Definition != null) return;

            Debug.LogError(
                $"[{nameof(EnemySpawnDirector)}] \"{_plan.name}\"에 보스 정의가 없다. " +
                "12분에 보스가 나오지 않아 승리 조건이 성립하지 않는다.", this);
        }
    }
}
