using Alchemy.Inspector;
using BeatTemplate;
using UnityEngine;

/// <summary>
/// 지정한 마디 수만큼 버티면 클리어시키는 임시 종료 소스.
///
/// 초가 아니라 박 기준으로 재는 이유는, 나중에 실제 곡을 물린 오케스트레이터로 갈아탈 때
/// "몇 초 뒤"가 아니라 "몇 마디 뒤"가 그대로 이식되기 때문이다.
/// 수치는 기획 확정 전까지 인스펙터에서 굴리는 값이다.
/// </summary>
public sealed class BeatTimelineEndSource : StageEndSource
{
    [Title("클록")]
    [SerializeField, LabelText("박자 클록 (비우면 자동 탐색)")] private BpmClock _clock;

    [Title("클리어 조건")]
    [SerializeField, LabelText("한 마디 박 수"), Min(1)] private int _beatsPerBar = 4;
    [SerializeField, LabelText("버텨야 하는 마디 수"), Min(1)] private int _barsToClear = 8;

    [Title("디버그")]
    [ShowInInspector, ReadOnly, LabelText("경과 박")] private int ElapsedBeatsDebug => _elapsedBeats;
    [ShowInInspector, ReadOnly, LabelText("목표 박")] private int TargetBeats => _beatsPerBar * _barsToClear;

    private int _elapsedBeats;
    private bool _isRunning;

    public override float Progress =>
        TargetBeats <= 0 ? 0f : Mathf.Clamp01((float)_elapsedBeats / TargetBeats);

    protected override void OnBound()
    {
        if (_clock == null) _clock = FindFirstObjectByType<BpmClock>();

        if (_clock == null)
        {
            Debug.LogError(
                $"[{nameof(BeatTimelineEndSource)}] BpmClock을 찾지 못했다. 클리어 조건이 영영 달성되지 않는다.", this);
        }
    }

    public override void OnStageBegin()
    {
        _elapsedBeats = 0;
        _isRunning = true;
    }

    public override void OnStageEnd() => _isRunning = false;

    private void Update()
    {
        if (!_isRunning || _clock == null) return;
        if (_clock.State != BeatState.Playing) return;

        // 디렉터가 시작 시 클록을 재앵커하므로 CurrentBeat는 스테이지 시작 기준 0부터 센다.
        _elapsedBeats = _clock.CurrentBeat;

        if (_elapsedBeats < TargetBeats) return;

        _isRunning = false;
        RequestClear();
    }
}
