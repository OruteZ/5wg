using Alchemy.Inspector;
using UnityEngine;

namespace FiveWG.UI
{
    /// <summary>
    /// 캔버스 하나가 내는 UI 소리를 전부 맡는다. 화면 스크립트들이 같은 오브젝트에 붙어 있으므로
    /// <c>GetComponent</c>로 찾는다 — 씬마다 AudioSource를 흩어 놓지 않기 위해서다.
    ///
    /// **박자에 맞추지 않는다.** 무기 루프는 dspTime으로 곡에 붙여 두지만, 버튼 소리를 다음 정박까지
    /// 미루면 누른 느낌이 사라진다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class UiSfx : MonoBehaviour
    {
        [Title("소리")]
        [SerializeField, LabelText("버튼 누름")] private AudioClip _click;
        [SerializeField, LabelText("버튼 위에 올림")] private AudioClip _hover;
        [SerializeField, LabelText("레벨업 카드 등장")] private AudioClip _cardShow;
        [SerializeField, LabelText("카드 선택")] private AudioClip _cardPick;
        [SerializeField, LabelText("클리어")] private AudioClip _clear;
        [SerializeField, LabelText("실패")] private AudioClip _fail;

        [Title("볼륨 (0~1)")]
        [SerializeField, LabelText("기본"), Range(0f, 1f)] private float _volume = 0.7f;
        [SerializeField, LabelText("올림 소리"), Range(0f, 1f)] private float _hoverVolume = 0.3f;

        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();

            // 효과음은 2D로 낸다. 리스너와의 거리에 따라 작아지면 안 된다.
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }

        public void PlayClick() => Play(_click, _volume);

        public void PlayHover() => Play(_hover, _hoverVolume);

        public void PlayCardShow() => Play(_cardShow, _volume);

        public void PlayCardPick() => Play(_cardPick, _volume);

        /// <summary>판이 끝난 결과에 맞는 소리. 클리어와 실패가 서로 다른 소리다.</summary>
        public void PlayResult(bool cleared) => Play(cleared ? _clear : _fail, _volume);

        // 클립이 비어도 조용히 넘어간다. 소리 하나가 빠졌다고 화면이 멈출 이유가 없다.
        private void Play(AudioClip clip, float volume)
        {
            if (clip == null || _source == null) return;

            _source.PlayOneShot(clip, volume);
        }
    }
}
