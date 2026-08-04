using UnityEngine;

/// <summary>
/// N정박마다 발사하는 임시 타이밍. 기획서가 나오기 전까지 파이프라인을 돌려보기 위한 자리채움이다.
/// 곡 시작 기준 절대 비트 인덱스로 판단하므로 내부 상태가 없다.
/// </summary>
public sealed class EveryBeatTiming : IFireTiming
{
    private readonly int _intervalBeats;
    private readonly int _offsetBeats;

    public EveryBeatTiming(int intervalBeats = 1, int offsetBeats = 0)
    {
        _intervalBeats = Mathf.Max(1, intervalBeats);
        _offsetBeats = Mathf.Max(0, offsetBeats);
    }

    public bool ShouldFire(in BeatTick tick)
    {
        if (!tick.IsOnBeat) return false;
        if (tick.Beat < _offsetBeats) return false;

        return (tick.Beat - _offsetBeats) % _intervalBeats == 0;
    }

    public void Reset()
    {
        // 절대 비트 인덱스로만 판단하므로 되돌릴 상태가 없다.
    }
}
