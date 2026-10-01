using Alchemy.Inspector;
using BeatTemplate;
using FiveWG.Core;
using FiveWG.Player;
using FiveWG.Weapons;
using UnityEngine;

namespace FiveWG.Stage
{
    /// <summary>
    /// 스테이지를 4구간으로 나눠 구간 경계마다 BPM을 한 단계씩 올린다(기획서 [02]·[14-15]:
    /// 80→90→100→110). 회차(구간) 경계는 원래 스폰 디렉터가 정해야 할 값인데 아직 그 개념이
    /// 없어서, 여기서는 <see cref="BeatTimelineEndSource"/>와 같은 방식으로 생존 마디 수를
    /// 4등분해 임시로 가른다. 스폰 디렉터에 회차 개념이 생기면 그걸로 교체한다.
    /// </summary>
    public sealed class StageTempoDirector : MonoBehaviour
    {
        [Title("클록")]
        [SerializeField, LabelText("박자 클록 (비우면 자동 탐색)")] private BpmClock _clock;
        [SerializeField, LabelText("악기 루프 재동기화 대상 (비우면 자동 탐색)")] private WeaponHandler _weaponHandler;

        [Title("구간 (기획서 [02] 회차별 BPM 계단)")]
        [SerializeField, LabelText("구간별 BPM (1구간부터)")] private int[] _bpmSteps = { 80, 90, 100, 110 };

        [SerializeField, LabelText("한 구간의 마디 수 (임시 — 스폰 디렉터에 회차가 생기면 대체)")]
        private int _barsPerPhase = 16;

        [Title("디버그")]
        [ShowInInspector, ReadOnly, LabelText("현재 구간 (1부터)")] private int CurrentPhaseDebug => _currentPhase + 1;
        [ShowInInspector, ReadOnly, LabelText("현재 BPM")] private int CurrentBpmDebug => _clock == null ? 0 : _clock.Bpm;

        private int _currentPhase;

        private void Awake()
        {
            if (_clock == null) _clock = SceneServices.Instance.Clock;
            if (_weaponHandler == null) _weaponHandler = FindFirstObjectByType<WeaponHandler>();

            if (_clock == null)
            {
                Debug.LogError($"[{nameof(StageTempoDirector)}] BpmClock을 찾지 못했다. 구간이 전환되지 않는다.", this);
            }

            if (_bpmSteps is { Length: > 0 })
            {
                _clock?.SetBpm(_bpmSteps[0]);
            }
        }

        private void Update()
        {
            if (_clock == null || _clock.State != BeatState.Playing) return;
            if (_bpmSteps is not { Length: > 0 }) return;

            int barsElapsed = _clock.CurrentBeat / CellMath.BeatsPerBar;
            int targetPhase = Mathf.Clamp(barsElapsed / Mathf.Max(1, _barsPerPhase), 0, _bpmSteps.Length - 1);

            if (targetPhase == _currentPhase) return;

            _currentPhase = targetPhase;
            _clock.SetBpm(_bpmSteps[_currentPhase]);
            _weaponHandler?.HandleBpmChanged();
        }
    }
}
