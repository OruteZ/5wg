using System;
using Alchemy.Inspector;
using UnityEngine;

namespace BeatTemplate
{
    public enum BeatState
    {
        Idle, Playing, Paused, Stopped
    }

    /// <summary>
    /// AudioSettings.dspTime를 앵커로 "지금 몇 박/서브박인지"를 계산하는 순수 클록.
    /// 박 넘어감 감지/이벤트/틱 사운드는 외부(BeatEvent 등)에서 CurrentBeat를 폴링해 처리한다.
    /// </summary>
    public class BpmClock : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private int bpm = 120;
        [SerializeField, Min(1)] private int subPerBeat = 4;

        // 끄면 Play() 버튼이나 외부 호출 전까지 Idle로 남는다.
        [SerializeField] private bool playOnStart = true;

        // runtime
        private double _dspBeat0;              // 비트 0 앵커(DSP 시각)
        private BeatState _beatState = BeatState.Idle;

        // 일시정지 누적 시간 / 이번 정지 시작 DSP 시각
        private double _pausedAccumSec = 0.0;
        private double _pauseDspStart  = 0.0;

        public double ElapsedSec
        {
            get
            {
                if (_beatState is not (BeatState.Playing or BeatState.Paused)) return 0.0;

                // Paused일 땐 멈춘 순간의 DSP로 고정해 정지 중에도 값이 안 늘어나게 함
                double nowDsp = (_beatState == BeatState.Paused)
                    ? _pauseDspStart
                    : AudioSettings.dspTime;

                // 앵커로부터 경과시간에서 누적 정지 시간을 빼 연속성 유지
                double elapsed = nowDsp - _dspBeat0 - _pausedAccumSec;
                return Math.Max(0.0, elapsed);
            }
        }

        // 외부에서 박 그리드를 재구성하려면 bpm/subPerBeat가 필요하다(예: WeaponHandler의 BeatTick).
        public int Bpm => bpm;
        public int SubPerBeat => subPerBeat;

        public int CurrentBeat
        {
            get
            {
                (int beat, int _) = Quantizer.Quantize(bpm, ElapsedSec, subPerBeat, 0.0);
                return beat;
            }
        }

        public int CurrentSubBeat
        {
            get
            {
                (int _, int sb) = Quantizer.Quantize(bpm, ElapsedSec, subPerBeat, 0.0);
                return sb;
            }
        }

        // 시각 메트로놈(진자/링/플래시)용 0~1 진행도
        public float BeatProgress => (float)(ElapsedSec / (60.0 / bpm)) - CurrentBeat;

        public BeatState State => _beatState;

        // Awake가 아니라 Start에서 앵커를 잡아 다른 컴포넌트의 초기화가 끝난 뒤 비트0이 시작되게 한다.
        private void Start()
        {
            if (playOnStart) Play();
        }

        [Button]
        public void Play()
        {
            switch (_beatState)
            {
                case BeatState.Idle:
                case BeatState.Stopped:
                    // 새로 시작: 지금을 비트0으로 앵커
                    _dspBeat0       = AudioSettings.dspTime;
                    _pausedAccumSec = 0.0;
                    _pauseDspStart  = 0.0;
                    _beatState      = BeatState.Playing;
                    return;

                case BeatState.Paused:
                    // 재개: 정지했던 만큼 누적에 더해 앵커를 유지
                    _pausedAccumSec += AudioSettings.dspTime - _pauseDspStart;
                    _pauseDspStart   = 0.0;
                    _beatState       = BeatState.Playing;
                    return;

                default:
                    return;
            }
        }

        [Button]
        public void Pause()
        {
            if (_beatState == BeatState.Playing)
            {
                _pauseDspStart = AudioSettings.dspTime; // 정지 시작 시각 기록
                _beatState     = BeatState.Paused;
            }
        }

        [Button]
        public void Stop()
        {
            if (_beatState is BeatState.Playing or BeatState.Paused)
                _beatState = BeatState.Stopped;
        }
    }
}