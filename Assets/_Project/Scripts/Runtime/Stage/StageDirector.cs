using System;
using Alchemy.Inspector;
using BeatTemplate;
using FiveWG.Core;
using FiveWG.Enemies;
using FiveWG.Player;
using UnityEngine;

namespace FiveWG.Stage
{
    /// <summary>
    /// 스테이지의 상태만 소유하는 조정자. "언제 끝나는가"는 모른다.
    /// 종료 판단은 StageEndSource가 하고, 디렉터는 RequestClear/RequestFail 창구만 연다.
    ///
    /// 종료 시 시간을 Time.timeScale로 멈추지 않는다. 박자 클록이 dspTime 기준이라
    /// timeScale을 건드리면 비트 그리드와 어긋난다. 대신 각 축을 개별로 끈다.
    /// 예외는 플레이어가 거는 일시정지 하나다(→ <see cref="SetPaused"/>).
    /// </summary>
    public sealed class StageDirector : MonoBehaviour
    {
        [Title("참조 (비우면 씬에서 자동 탐색)")]
        [SerializeField, LabelText("박자 클록")] private BpmClock _clock;
        [SerializeField, LabelText("적 스포너")] private EnemySpawnerBase _spawner;
        [SerializeField, LabelText("플레이어")] private PlayerController _player;

        [Title("종료 소스")]
        [SerializeField, LabelText("종료를 알리는 주체 (비우면 씬 전체에서 수집)")]
        private StageEndSource[] _endSources;

        [Title("진행")]
        [SerializeField, LabelText("씬 로드 즉시 시작")] private bool _beginOnStart = true;

        [Title("디버그")]
        [ShowInInspector, ReadOnly, LabelText("상태")] private StageState StateDebug => State;
        [ShowInInspector, ReadOnly, LabelText("진행도")] private float ProgressDebug => Progress;
        [ShowInInspector, ReadOnly, LabelText("일시정지")] private bool PausedDebug => IsPaused;

        private WeaponHandler _weapon;

        // 앱 포커스·에디터 일시정지. 플레이어 일시정지와 따로 들고 있어야 한쪽이 풀릴 때 다른 쪽이 건
        // 정지까지 풀어버리지 않는다(일시정지 메뉴를 연 채 알트탭했다 돌아오면 박이 다시 흐르던 경우).
        private bool _systemPaused;

        public StageState State { get; private set; } = StageState.Ready;

        /// <summary>HUD가 체력을 읽어가는 통로. 디렉터가 플레이어를 이미 찾아뒀으므로 중복 탐색을 막는다.</summary>
        public PlayerController Player => _player;

        /// <summary>종료 소스들이 보고하는 진행도 중 가장 앞선 값.</summary>
        public float Progress { get; private set; }

        /// <summary>플레이어가 건 일시정지. 앱 포커스로 인한 정지는 포함하지 않는다.</summary>
        public bool IsPaused { get; private set; }

        public event Action<StageState> OnStateChanged;

        /// <summary>인자는 새 일시정지 상태. 값이 실제로 바뀔 때만 발행된다.</summary>
        public event Action<bool> OnPauseChanged;

        private void Awake()
        {
            ResolveReferences();

            // 디렉터가 시작을 선언하기 전까지는 아무것도 돌지 않게 막는다.
            SetCombatActive(false);
        }

        private void Start()
        {
            // 소스 결속은 Start에서 한다. 다른 컴포넌트의 Awake 초기화가 끝난 뒤여야 안전하다.
            BindEndSources();

            if (_beginOnStart) Begin();
        }

        // 클록은 dspTime 기준이라 앱이 멈춰도 박이 계속 흐른다. 오디오는 엔진이 멈췄다 이어 트는데
        // 클록만 앞서 가면 재개 순간 악기 루프와 발사 그리드가 어긋나고 밀린 틱이 몰아친다.
        // 그래서 일시정지 동안 클록도 같이 세운다. 악기 루프는 WeaponHandler가 클록 상태를 따라간다.
        private void OnApplicationPause(bool paused)
        {
            _systemPaused = paused;
            ApplyClockPause();
        }

#if UNITY_EDITOR
        private void OnEnable() => UnityEditor.EditorApplication.pauseStateChanged += HandleEditorPause;
        private void OnDisable() => UnityEditor.EditorApplication.pauseStateChanged -= HandleEditorPause;

        private void HandleEditorPause(UnityEditor.PauseState state)
        {
            _systemPaused = state == UnityEditor.PauseState.Paused;
            ApplyClockPause();
        }
#endif

        // 멈춘 채 플레이 모드를 나가거나 씬이 내려가면 다음 씬·다음 실행이 정지한 채로 시작한다.
        private void OnDestroy()
        {
            if (IsPaused) Time.timeScale = 1f;
        }

        private void ApplyClockPause()
        {
            if (_clock == null || State != StageState.Playing) return;

            if (IsPaused || _systemPaused) _clock.Pause();
            else _clock.Play();
        }

        /// <summary>
        /// 플레이어가 거는 일시정지. 진행 중일 때만 걸리고, 판이 끝나면 저절로 풀린다.
        ///
        /// 여기서만 <c>Time.timeScale</c>을 쓴다. 적 이동(물리)·투사체 수명·스폰 시간축·보스 제한 시간이
        /// 전부 deltaTime이라 축별 스위치로는 화면이 멈추지 않기 때문이다. 대신 timeScale로 안 서는
        /// 박자 클록은 직접 세운다 — 그래야 재개했을 때 비트 그리드가 화면과 같은 지점에서 이어진다.
        /// 악기 루프는 WeaponHandler가 클록 상태를 따라 멈춘다.
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (paused == IsPaused) return;
            if (paused && State != StageState.Playing) return;

            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            ApplyClockPause();

            OnPauseChanged?.Invoke(paused);
        }

        private void ResolveReferences()
        {
            if (_clock == null) _clock = SceneServices.Instance.Clock;
            if (_spawner == null) _spawner = SceneServices.Instance.Spawner;
            if (_player == null) _player = SceneServices.Instance.Player;

            if (_player != null) _weapon = _player.GetComponent<WeaponHandler>();

            if (_clock == null)
            {
                Debug.LogError($"[{nameof(StageDirector)}] BpmClock을 찾지 못했다. 박자가 시작되지 않는다.", this);
            }

            if (_player == null)
            {
                Debug.LogError($"[{nameof(StageDirector)}] PlayerController를 찾지 못했다.", this);
            }
        }

        private void BindEndSources()
        {
            if (_endSources == null || _endSources.Length == 0)
            {
                _endSources = FindObjectsByType<StageEndSource>(FindObjectsSortMode.None);
            }

            if (_endSources.Length == 0)
            {
                Debug.LogWarning(
                    $"[{nameof(StageDirector)}] 종료 소스가 하나도 없다. 스테이지가 끝나지 않는다.", this);
            }

            foreach (StageEndSource source in _endSources)
            {
                if (source == null) continue;
                source.Bind(this);
            }
        }

        [Button, LabelText("스테이지 시작")]
        public void Begin()
        {
            if (State != StageState.Ready) return;

            ChangeState(StageState.Playing);

            // 스테이지 시작 시점을 비트 0으로 다시 앵커한다. 결과 화면에서 되돌아와도 같은 그리드로 시작한다.
            if (_clock != null)
            {
                _clock.Stop();
                _clock.Play();
            }

            SetCombatActive(true);

            foreach (StageEndSource source in _endSources)
            {
                if (source != null) source.OnStageBegin();
            }
        }

        [Button, LabelText("클리어 요청")]
        public void RequestClear() => End(StageState.Cleared);

        [Button, LabelText("실패 요청")]
        public void RequestFail() => End(StageState.Failed);

        private void End(StageState result)
        {
            // 같은 프레임에 두 소스가 동시에 끝을 알려도 먼저 온 쪽만 반영한다.
            if (State != StageState.Playing) return;

            // 결과 화면이 멈춘 시간 위에 뜨지 않게 먼저 푼다. 클록은 아래에서 어차피 멈춘다.
            SetPaused(false);

            ChangeState(result);

            if (_clock != null) _clock.Stop();

            SetCombatActive(false);

            // 클리어는 보스를 잡은 즉시라 남은 적을 두는 것이 기획이다. 실패만 판을 비운다.
            if (_spawner != null && result == StageState.Failed) _spawner.ClearAll();

            foreach (StageEndSource source in _endSources)
            {
                if (source != null) source.OnStageEnd();
            }
        }

        private void SetCombatActive(bool active)
        {
            if (_spawner != null) _spawner.SetSpawning(active);
            if (_weapon != null) _weapon.AttackEnabled = active;
            if (_player != null) _player.ControlEnabled = active;
        }

        private void ChangeState(StageState state)
        {
            State = state;
            OnStateChanged?.Invoke(state);
        }

        private void Update()
        {
            if (State != StageState.Playing) return;

            float progress = 0f;
            foreach (StageEndSource source in _endSources)
            {
                if (source == null) continue;
                progress = Mathf.Max(progress, source.Progress);
            }

            Progress = progress;
        }
    }
}
