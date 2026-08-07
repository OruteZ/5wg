using System;
using Alchemy.Inspector;
using UnityEngine;

namespace FiveWG.Charting
{
    /// <summary>
    /// 악기 하나의 트랙. stem 음원과 그 악기가 연주되는 시점을 같이 든다.
    /// </summary>
    [Serializable]
    public struct ChartTrack
    {
        [LabelText("악기")] public InstrumentTrack Instrument;

        // stem은 저장소에 커밋되지 않는다(→ docs/charting.md). 새로 받은 저장소에서는 비어 있다.
        [LabelText("stem 음원 (WAV)")] public AudioClip Stem;

        /// <summary>
        /// 곡 시작부터 누적된 서브박 인덱스. 오름차순이고 중복이 없다.
        ///
        /// (마디, 박, 서브박) 삼중항이 아닌 이유는 그 셋이 서로 어긋난 상태를 표현할 수 있어서다.
        /// 누적 인덱스 하나면 나눗셈으로 언제든 셋이 나오고, <c>BeatTick.SubIndex</c>가 이미 같은 형태다.
        /// </summary>
        [LabelText("노트 (누적 서브박 인덱스)")] public int[] Notes;
    }

    /// <summary>
    /// 곡 하나의 채보. 에셋 하나가 곡 하나이므로 곡 식별자를 따로 두지 않는다 —
    /// 트랙 열거형은 곡 안에서만 유일하면 되고, "다른 곡의 같은 악기"는 다른 에셋이라 충돌하지 않는다.
    ///
    /// **곡 전체를 담은 클립이 없다.** 플레이어가 고른 악기만 들려야 하므로 믹스본을 틀 수 없고,
    /// 재생은 트랙별 stem을 동시에 출발시키는 것이다(→ <see cref="ChartPlayer"/>).
    /// </summary>
    [CreateAssetMenu(fileName = "Chart", menuName = "Charting/Chart")]
    public sealed class ChartAsset : ScriptableObject
    {
        /// <summary>
        /// 정박 하나를 몇 등분하는가. 16분 고정이다.
        ///
        /// 필드로 두지 않는 이유는 <c>BpmClock</c>의 설정과 어긋날 수 있어서다. 하이헷이 16분으로
        /// 달리는 게 락 밴드 편성의 기본이고, 그보다 잘게 쪼갠 연주는 자동공격에서 발사로 구분되지도 않는다.
        /// 해상도를 올리면 검출 노이즈가 그대로 노트가 된다.
        /// </summary>
        public const int SubPerBeat = 4;

        [Title("곡")]
        // 사람이 넣는다. 자동 검출하지 않는다 — 틀리면 채보 전체가 무너지는데 틀렸다는 걸 알아채기 어렵다.
        // 대신 검출 도구가 "이 BPM으로 그리드를 그렸을 때 온셋이 평균 몇 ms 어긋나는지"를 채점한다.
        [SerializeField, LabelText("BPM"), Min(1f)] private double _bpm = 120.0;

        [Title("트랙")]
        [SerializeField, LabelText("악기별 트랙")] private ChartTrack[] _tracks = Array.Empty<ChartTrack>();

        [Title("디버그")]
        [ShowInInspector, ReadOnly, LabelText("서브박 하나 (ms)")]
        private double SubMillisDebug => SecondsPerSub * 1000.0;

        [ShowInInspector, ReadOnly, LabelText("트랙 수 / 총 노트 수")]
        private string CountDebug
        {
            get
            {
                int notes = 0;
                for (int i = 0; i < (_tracks?.Length ?? 0); i++) notes += _tracks[i].Notes?.Length ?? 0;
                return $"{_tracks?.Length ?? 0} / {notes}";
            }
        }

        public double Bpm => _bpm;

        public ChartTrack[] Tracks => _tracks;

        /// <summary>서브박 하나의 길이(초). 노트 인덱스를 시각으로 바꿀 때 쓴다.</summary>
        public double SecondsPerSub => 60.0 / _bpm / SubPerBeat;

        /// <summary>노트 인덱스 → 곡 시작 기준 초. 0초가 비트 0이므로 오프셋이 없다.</summary>
        public double TimeOf(int noteSubIndex) => noteSubIndex * SecondsPerSub;

        /// <summary>해당 악기의 트랙. 없으면 false.</summary>
        public bool TryGetTrack(InstrumentTrack instrument, out ChartTrack track)
        {
            for (int i = 0; i < (_tracks?.Length ?? 0); i++)
            {
                if (_tracks[i].Instrument != instrument) continue;

                track = _tracks[i];
                return true;
            }

            track = default;
            return false;
        }

        /// <summary>검출기가 뽑은 노트를 트랙에 넣는다. 에디터 도구가 쓴다.</summary>
        public void SetNotes(int trackIndex, int[] notes)
        {
            if (_tracks is null || trackIndex < 0 || trackIndex >= _tracks.Length) return;
            _tracks[trackIndex].Notes = notes;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_tracks == null) return;

            // 노트가 오름차순·중복 없음이라는 전제를 검출기와 재생 양쪽이 깔고 있다.
            // 손으로 고치다 깨뜨리는 게 흔해서 저장할 때 바로잡는다.
            for (int i = 0; i < _tracks.Length; i++)
            {
                int[] notes = _tracks[i].Notes;
                if (notes is not { Length: > 1 }) continue;

                Array.Sort(notes);

                int write = 1;
                for (int r = 1; r < notes.Length; r++)
                {
                    if (notes[r] == notes[write - 1]) continue;
                    notes[write++] = notes[r];
                }

                if (write != notes.Length) Array.Resize(ref notes, write);
                _tracks[i].Notes = notes;
            }
        }
#endif
    }
}
