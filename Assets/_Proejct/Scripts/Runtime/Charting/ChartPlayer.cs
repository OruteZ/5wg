using System.Collections.Generic;
using Alchemy.Inspector;
using BeatTemplate;
using UnityEngine;

namespace FiveWG.Charting
{
    /// <summary>
    /// 채보와 stem을 클록에 붙여 재생한다. 곡·메트로놈·검출된 온셋을 같은 타임라인 위에 올리는 것이
    /// 이 클래스의 전부다.
    ///
    /// <b>동기의 핵심은 예약이다.</b> 트랙별 <c>AudioSource</c>를 전부 같은 미래 시각으로
    /// <c>PlayScheduled</c>하고, 클록도 <see cref="BpmClock.PlayAt"/>로 그 시각을 비트 0으로 잡는다.
    /// <c>dspTime</c>이 곧 오디오 하드웨어 시계라 트랙이 몇 개든 서로 어긋나지 않는다.
    ///
    /// 지금 당장 틀지 않고 <see cref="_startDelay"/>만큼 미래로 예약하는 이유는, 예약 시각이 이미
    /// 지났으면 오디오 시스템이 "가능한 한 빨리"로 처리해 샘플 정확도를 잃기 때문이다.
    /// </summary>
    public sealed class ChartPlayer : MonoBehaviour
    {
        [Title("대상")]
        [SerializeField, Required("박자 소스가 필요하다.")] private BpmClock _clock;
        [SerializeField, LabelText("채보")] private ChartAsset _chart;

        [Title("재생")]
        [SerializeField, LabelText("예약 여유 (sec). 이 시간 뒤에 곡이 시작된다"), Min(0.05f)]
        private double _startDelay = 0.2;

        [Title("검증")]
        [SerializeField, LabelText("혼자 들어볼 트랙")] private InstrumentTrack _soloTrack = InstrumentTrack.Kick;

        [SerializeField, LabelText("메트로놈 음 높이 (Hz)")] private float _metronomeHz = 1200f;
        [SerializeField, LabelText("온셋 클릭 음 높이 (Hz)")] private float _onsetHz = 800f;
        [SerializeField, LabelText("클릭 볼륨"), Range(0f, 1f)] private float _clickVolume = 0.5f;

        [Title("디버그")]
        [ShowInInspector, ReadOnly, LabelText("클록 상태")]
        private string ClockDebug => _clock == null ? "클록 미연결" : $"{_clock.State} / {_clock.Bpm:0.##} BPM";

        [ShowInInspector, ReadOnly, LabelText("재생 중인 소스 수")]
        private int PlayingDebug
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _sources.Count; i++)
                {
                    if (_sources[i] != null && _sources[i].isPlaying) n++;
                }

                return n;
            }
        }

        private readonly List<AudioSource> _sources = new();

        // 트랙 인덱스 → 그 트랙의 stem을 트는 소스. 볼륨 조절이 트랙 단위로 필요해서 따로 둔다.
        private readonly Dictionary<InstrumentTrack, AudioSource> _stemSources = new();

        private void Awake()
        {
            if (_clock == null) _clock = GetComponent<BpmClock>();

            if (_clock == null)
            {
                Debug.LogError($"[{nameof(ChartPlayer)}] {nameof(BpmClock)}을 찾지 못했다. 재생해도 박이 흐르지 않는다.", this);
            }

            if (_chart == null)
            {
                Debug.LogWarning($"[{nameof(ChartPlayer)}] 채보가 비어 있다. 재생할 것이 없다.", this);
            }
        }

        /// <summary>
        /// 트랙 하나만 검증한다. stem은 가운데, 메트로놈은 왼쪽, 검출된 온셋은 오른쪽으로 나온다.
        ///
        /// 좌우로 가르는 이유는 귀가 절대 시각보다 <b>두 소리의 어긋남</b>에 훨씬 예민하기 때문이다.
        /// 몇 ms만 밀려도 플랜징처럼 들려서, 수치로는 놓치는 어긋남이 바로 잡힌다.
        /// </summary>
        [Button, LabelText("고른 트랙만 검증 재생")]
        public void PlaySolo()
        {
            if (!TryBegin(out double start)) return;

            if (!_chart.TryGetTrack(_soloTrack, out ChartTrack track))
            {
                Debug.LogWarning($"[{nameof(ChartPlayer)}] 채보에 '{_soloTrack}' 트랙이 없다.", this);
                return;
            }

            double length = LengthSeconds(track);

            Schedule(track.Stem, start, pan: 0f, volume: 1f, key: track.Instrument);
            Schedule(BuildClicks("Metronome", BeatIndices(length), length, _metronomeHz), start, -1f, _clickVolume);
            Schedule(BuildClicks("Onsets", track.Notes, length, _onsetHz), start, 1f, _clickVolume);

            _clock.PlayAt(start);
        }

        /// <summary>
        /// 전 트랙을 동시에 튼다. <b>stem끼리 어긋나지 않았는지</b> 보는 용도다 —
        /// 트랙마다 따로 자르면 한 악기만 앞서 들리는데, 그건 하나씩 들어서는 안 잡힌다.
        /// </summary>
        [Button, LabelText("전 트랙 동시 재생")]
        public void PlayAll()
        {
            if (!TryBegin(out double start)) return;

            foreach (ChartTrack track in _chart.Tracks)
            {
                Schedule(track.Stem, start, pan: 0f, volume: 1f, key: track.Instrument);
            }

            _clock.PlayAt(start);
        }

        [Button, LabelText("정지")]
        public void Stop()
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                if (_sources[i] != null) Destroy(_sources[i].gameObject);
            }

            _sources.Clear();
            _stemSources.Clear();

            if (_clock != null) _clock.Stop();
        }

        /// <summary>
        /// 트랙 볼륨. 플레이어가 아직 고르지 않은 악기는 <b>0으로 두되 재생은 시킨다</b> —
        /// 안 트는 것과 소리는 같지만, 나중에 그 악기를 얻었을 때 곡 중간부터 페이드인할 수 있다.
        /// 무엇을 고를지 아는 쪽(무기 시스템)이 호출한다. 이 브랜치에는 그 호출자가 없다.
        /// </summary>
        public void SetTrackVolume(InstrumentTrack instrument, float volume)
        {
            if (_stemSources.TryGetValue(instrument, out AudioSource source) && source != null)
            {
                source.volume = Mathf.Clamp01(volume);
            }
        }

        private bool TryBegin(out double start)
        {
            start = 0.0;
            if (_clock == null || _chart == null) return false;

            Stop();
            start = AudioSettings.dspTime + _startDelay;
            return true;
        }

        private void Schedule(AudioClip clip, double start, float pan, float volume,
            InstrumentTrack? key = null)
        {
            if (clip == null) return;

            var go = new GameObject(clip.name);
            go.transform.SetParent(transform, false);

            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.panStereo = pan;
            source.volume = volume;
            source.spatialBlend = 0f;   // 2D. 위치에 따라 볼륨이 변하면 검증이 안 된다
            source.playOnAwake = false;
            source.PlayScheduled(start);

            _sources.Add(source);
            if (key.HasValue) _stemSources[key.Value] = source;
        }

        /// <summary>stem이 있으면 그 길이, 없으면 마지막 노트까지. 클릭 트랙을 얼마나 만들지 정한다.</summary>
        private double LengthSeconds(in ChartTrack track)
        {
            double byNotes = track.Notes is { Length: > 0 }
                ? _chart.TimeOf(track.Notes[^1]) + 1.0
                : 0.0;

            double byStem = track.Stem != null ? track.Stem.length : 0.0;
            return Mathf.Max(1f, (float)Mathf.Max((float)byNotes, (float)byStem));
        }

        /// <summary>정박 위치의 서브박 인덱스들. 메트로놈은 정박에만 친다.</summary>
        private IEnumerable<int> BeatIndices(double length)
        {
            double perSub = _chart.SecondsPerSub;
            int total = Mathf.CeilToInt((float)(length / perSub));

            for (int i = 0; i < total; i += ChartAsset.SubPerBeat) yield return i;
        }

        /// <summary>
        /// 지정한 서브박 위치마다 짧은 클릭이 들어간 클립을 즉석에서 만든다.
        ///
        /// 클릭마다 <c>AudioSource</c>를 하나씩 예약하지 않는 이유는, 한 곡에 수백 개가 되면
        /// 소스 수가 터지고 예약 자체가 부담이 되기 때문이다. 클립 하나로 구우면 샘플 단위로 정확하다.
        /// </summary>
        private AudioClip BuildClicks(string name, IEnumerable<int> subIndices, double length, float frequency)
        {
            if (subIndices is null) return null;

            int rate = AudioSettings.outputSampleRate;
            int total = Mathf.Max(1, Mathf.CeilToInt((float)(length * rate)));
            var samples = new float[total];

            int clickLength = rate / 40;   // 25ms
            double perSub = _chart.SecondsPerSub;

            foreach (int index in subIndices)
            {
                int start = (int)(index * perSub * rate);
                if (start < 0 || start >= total) continue;

                for (int i = 0; i < clickLength && start + i < total; i++)
                {
                    float t = i / (float)rate;
                    // 감쇠하는 사인. 어택이 뚜렷해야 어긋남이 귀에 잡힌다.
                    samples[start + i] += Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-60f * t);
                }
            }

            AudioClip clip = AudioClip.Create(name, total, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
