using UnityEngine;

namespace FiveWG.Stage
{
    /// <summary>
    /// "스테이지가 언제 끝나는가"를 판단해 디렉터에 알리는 주체.
    /// 디렉터는 종료 조건을 모르고, 이 쪽은 종료 처리 절차를 모른다.
    ///
    /// 지금은 비트 타임라인(임시)과 플레이어 사망 둘뿐이지만,
    /// 나중에 음악 오케스트레이터가 들어와도 이 클래스를 상속하면 씬에서 컴포넌트만 갈아끼우면 된다.
    ///
    /// 인터페이스가 아니라 MonoBehaviour 추상 클래스인 이유는 인스펙터에서 배열로 물려야 하기 때문.
    /// </summary>
    public abstract class StageEndSource : MonoBehaviour
    {
        protected StageDirector Director { get; private set; }

        /// <summary>0~1 진행도. 진행도 개념이 없는 소스(사망 등)는 0을 반환한다.</summary>
        public virtual float Progress => 0f;

        /// <summary>디렉터가 시작 시 한 번 호출한다.</summary>
        public void Bind(StageDirector director)
        {
            Director = director;
            OnBound();
        }

        /// <summary>스테이지가 실제로 시작된 시점. 여기서부터 계측을 시작한다.</summary>
        public virtual void OnStageBegin() { }

        /// <summary>스테이지가 끝난 시점. 계측을 멈춘다. 자기가 끝낸 경우에도 호출된다.</summary>
        public virtual void OnStageEnd() { }

        protected virtual void OnBound() { }

        protected void RequestClear() => Director?.RequestClear();

        protected void RequestFail() => Director?.RequestFail();
    }
}
