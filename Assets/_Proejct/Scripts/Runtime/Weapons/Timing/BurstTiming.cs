using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// N정박마다 서브비트 몇 개를 연달아 때리는 임시 타이밍. 탐(3연타)과 건반(아르페지오)이 쓴다.
    ///
    /// 순차 다단은 무기가 아니라 타이밍이다. WeaponHandler가 이미 서브비트마다 틱을 흘리고 있어서
    /// "언제 쏘는가"만 바꾸면 투사체든 영역이든 그대로 연타가 된다. 무기 클래스를 새로 만들면
    /// 같은 형태가 단발용·연타용으로 두 벌씩 생긴다.
    ///
    /// <see cref="EveryBeatTiming"/>과 마찬가지로 곡 시작 기준 절대 인덱스로만 판단한다(내부 상태 없음).
    /// 채보가 들어오면 이 클래스와 EveryBeatTiming 둘 다 노트 기반 타이밍으로 교체된다.
    /// </summary>
    public sealed class BurstTiming : IFireTiming
    {
        private readonly int _intervalBeats;
        private readonly int _offsetBeats;
        private readonly int _burstCount;
        private readonly int _burstSubStep;

        /// <param name="burstCount">한 번에 몇 발을 연달아 쏘는가.</param>
        /// <param name="burstSubStep">연타 사이 간격(서브비트 수). 1이면 서브비트마다.</param>
        public BurstTiming(int intervalBeats = 1, int offsetBeats = 0, int burstCount = 3, int burstSubStep = 1)
        {
            _intervalBeats = Mathf.Max(1, intervalBeats);
            _offsetBeats = Mathf.Max(0, offsetBeats);
            _burstCount = Mathf.Max(1, burstCount);
            _burstSubStep = Mathf.Max(1, burstSubStep);
        }

        public bool ShouldFire(in BeatTick tick)
        {
            if (tick.Beat < _offsetBeats) return false;

            // 연타는 정박에서 시작한다. 시작 정박이 아닌 박에 걸린 틱은 전부 거른다.
            int beatsSinceStart = tick.Beat - _offsetBeats;
            if (beatsSinceStart % _intervalBeats != 0) return false;

            // 연타는 시작 정박 안에서 끝난다. 클록의 SubPerBeat가 허용하는 칸보다 많이 요구하면
            // 넘치는 발은 그냥 나가지 않는다 — 다음 정박으로 넘기면 그 박의 연주와 겹치기 때문이다.
            if (tick.Sub % _burstSubStep != 0) return false;

            return tick.Sub / _burstSubStep < _burstCount;
        }

        public void Reset()
        {
            // 절대 비트 인덱스로만 판단하므로 되돌릴 상태가 없다.
        }

#if UNITY_EDITOR
        /// <summary>
        /// 연타가 시작 정박 안에서만, 주기에 맞는 정박에서만 나가는지 확인한다.
        /// 조용히 어긋나면 "가끔 안 쏘는 무기"로 보여서 원인을 찾기 어렵다.
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void SelfCheck()
        {
            // 2정박마다 3연타. 서브비트는 정박당 4칸.
            var timing = new BurstTiming(intervalBeats: 2, offsetBeats: 0, burstCount: 3, burstSubStep: 1);

            (int beat, int sub, bool expected)[] cases =
            {
                (0, 0, true), (0, 1, true), (0, 2, true),
                (0, 3, false),            // 연타 수를 넘어선 칸
                (1, 0, false),            // 주기에 안 맞는 정박
                (2, 0, true), (2, 2, true), (2, 3, false),
            };

            foreach ((int beat, int sub, bool expected) in cases)
            {
                var tick = new BeatTick(beat, sub, subPerBeat: 4, bpm: 120d, dspTime: 0d);
                if (timing.ShouldFire(tick) == expected) continue;

                Debug.LogError(
                    $"[{nameof(BurstTiming)}] {beat}박 {sub}서브에서 발사 여부가 {expected}여야 하는데 아니다.");
            }
        }
#endif
    }
}
