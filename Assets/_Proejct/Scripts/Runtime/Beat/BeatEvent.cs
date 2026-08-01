using System;
using UnityEngine;
using UnityEngine.Events;


namespace BeatTemplate
{
    /// <summary>
    /// 비트와 서브비트가 변경될 때마다 이벤트를 발생시키는 컴포넌트
    /// </summary>
    public class BeatEvent : MonoBehaviour
    {
        [SerializeField] private BpmClock beat;
        [SerializeField] private bool emitSubBeats = true;


        public UnityEvent<int> OnBeat; // (beat)
        public UnityEvent<int, int> OnSubBeat; // (beat, sub)


        int _lastBeat = -1;
        (int beat, int sub) _lastSub = (-1, -1);
        
        #if UNITY_EDITOR
        [SerializeField] private double elapsedSec;
        [SerializeField] private double beatCnt;
        [SerializeField] private double subBeatCnt;
        #endif


        private void Update()
        {
            if (beat == null) return;
            if (beat.State is not (BeatState.Playing or BeatState.Paused)) return;


            // 둘을 따로 읽으면 Quantize가 두 번 돈다.
            (int b, int s) = beat.CurrentPosition;


            if (b != _lastBeat)
            {
                _lastBeat = b;
                OnBeat?.Invoke(b);
                
            }


            if (emitSubBeats && (b != _lastSub.beat || s != _lastSub.sub))
            {
                _lastSub = (b, s);
                OnSubBeat?.Invoke(b, s);
            }
#if UNITY_EDITOR
            beatCnt = b;
            subBeatCnt = s;
            elapsedSec = beat.ElapsedSec;
#endif
        }
    }
}