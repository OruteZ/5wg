/// <summary>
/// 한 번의 서브비트 경계를 나타내는 값. WeaponHandler가 만들어 무기 타이밍에 전달한다.
/// </summary>
public readonly struct BeatTick
{
    /// <summary>0부터 시작하는 정박 인덱스.</summary>
    public readonly int Beat;

    /// <summary>정박 안에서의 서브비트 인덱스. 0이면 정박 위치.</summary>
    public readonly int Sub;

    /// <summary>정박 하나를 몇 등분하는지. BpmClock 설정값.</summary>
    public readonly int SubPerBeat;

    public readonly double Bpm;

    /// <summary>이 틱을 감지한 시점의 DSP 시각. 예약 발사를 쓸 때의 기준.</summary>
    public readonly double DspTime;

    public BeatTick(int beat, int sub, int subPerBeat, double bpm, double dspTime)
    {
        Beat = beat;
        Sub = sub;
        SubPerBeat = subPerBeat;
        Bpm = bpm;
        DspTime = dspTime;
    }

    /// <summary>정박 위치인지. 서브비트 무시하고 정박에만 반응할 때 쓴다.</summary>
    public bool IsOnBeat => Sub == 0;

    /// <summary>곡 시작부터 누적된 서브비트 인덱스. 주기·오프셋 계산의 기준값.</summary>
    public long SubIndex => (long)Beat * SubPerBeat + Sub;
}
