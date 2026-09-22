using Alchemy.Inspector;
using FiveWG.Core;
using FiveWG.Enemies;
using UnityEngine;

namespace FiveWG.Stage
{
    /// <summary>
    /// 보스 처치를 클리어로, 보스 등장 후 제한 시간 초과를 실패로 옮기는 종료 소스.
    /// 보스가 언제 나오는지는 적 편성 에셋이 정하고, 여기서는 그 결과만 본다.
    /// </summary>
    public sealed class BossEndSource : StageEndSource
    {
        [Title("대상")]
        [SerializeField, LabelText("적 스폰 디렉터 (비우면 자동 탐색)")] private EnemySpawnDirector _spawner;

        [Title("제한 시간")]
        [SerializeField, LabelText("보스 제한 시간 (sec)"), Min(1f)] private float _timeLimitSec = 120f;

        [Title("디버그")]
        [ShowInInspector, ReadOnly, LabelText("보스전 경과 (sec)")] private float BossElapsedDebug => _bossElapsedSec;

        private float _bossElapsedSec;
        private bool _bossStarted;
        private bool _isRunning;
        private bool _isSubscribed;

        /// <summary>
        /// 보스 전에는 보스 등장까지, 보스전에서는 제한 시간의 진행도. 판 전체를 한 줄로 그리지 않는 것은
        /// 보스 등장 마디가 BPM을 따라 움직이는 반면 제한 시간은 초라서 둘을 한 축에 합칠 수 없기 때문이다.
        /// </summary>
        public override float Progress
        {
            get
            {
                if (_bossStarted) return Mathf.Clamp01(_bossElapsedSec / _timeLimitSec);
                if (_spawner == null || _spawner.BossSpawnBar <= 0f) return 0f;
                return Mathf.Clamp01(_spawner.ElapsedBars / _spawner.BossSpawnBar);
            }
        }

        protected override void OnBound()
        {
            if (_spawner == null) _spawner = SceneServices.Instance.Spawner as EnemySpawnDirector;
            if (_spawner == null)
            {
                Debug.LogError(
                    $"[{nameof(BossEndSource)}] {nameof(EnemySpawnDirector)}를 찾지 못했다. 보스를 잡아도 클리어되지 않는다.", this);
                return;
            }

            _spawner.OnBossDefeated += HandleBossDefeated;
            _isSubscribed = true;
        }

        private void OnDestroy()
        {
            if (_isSubscribed && _spawner != null) _spawner.OnBossDefeated -= HandleBossDefeated;
        }

        public override void OnStageBegin()
        {
            _bossElapsedSec = 0f;
            _bossStarted = false;
            _isRunning = true;
        }

        public override void OnStageEnd() => _isRunning = false;

        // 디렉터와 같은 초 단위로 잰다. 제한 시간은 박에 맞출 값이 아니라 보스전 밸런스다.
        private void Update()
        {
            if (!_isRunning || _spawner == null) return;
            if (!_bossStarted && !_spawner.IsBossAlive) return;

            _bossStarted = true;
            _bossElapsedSec += Time.deltaTime;
            if (_bossElapsedSec < _timeLimitSec) return;

            _isRunning = false;
            RequestFail();
        }

        private void HandleBossDefeated(Vector2 position) => RequestClear();
    }
}
