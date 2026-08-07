using UnityEngine;

namespace FiveWG.Charting
{
    /// <summary>
    /// 채보 안에서 트랙이 어느 악기인지. 목록은 `.claude/docs/weapons.md`의 "악기 10종"과 1:1이다.
    ///
    /// 문자열이 아니라 열거형인 이유는 오타가 조용히 통과하지 않게 하기 위해서다.
    /// <c>WeaponDefinition</c> 참조로 두지 않은 것은 채보가 무기를 알게 만들지 않으려는 것 —
    /// 나중에 무기 쪽이 "나는 킥이다"라고 밝히고 이 열거형으로 만난다.
    ///
    /// 한 곡에 10종이 다 있을 필요는 없다. stem 분리가 4~6트랙까지만 깔끔하게 나오는 것이
    /// 보통이고, 곡마다 트랙 수가 다른 것을 정상으로 둔다.
    /// </summary>
    public enum InstrumentTrack
    {
        [InspectorName("킥")] Kick,
        [InspectorName("스네어")] Snare,
        [InspectorName("하이헷")] HiHat,
        [InspectorName("크래시")] Crash,
        [InspectorName("탐")] Tom,
        [InspectorName("베이스")] Bass,
        [InspectorName("기타 (일렉)")] ElectricGuitar,
        [InspectorName("어쿠스틱")] AcousticGuitar,
        [InspectorName("건반")] Keys,
        [InspectorName("신스")] Synth,
    }
}
