using Alchemy.Inspector;
using FiveWG.Core;
using FiveWG.Stage;
using UnityEngine.UI;
using UnityEngine;

namespace FiveWG.UI
{
    /// <summary>
    /// 스테이지 종료 시 뜨는 결과 오버레이. 디렉터의 상태 변경만 듣는다.
    /// 버튼 배선은 인스펙터의 UnityEvent가 아니라 코드로 건다. 씬 파일에 로직이 숨지 않게.
    /// </summary>
    public sealed class StageResultView : MonoBehaviour
    {
        [Title("참조 (비우면 자동 탐색)")]
        [SerializeField, LabelText("스테이지 디렉터")] private StageDirector _director;

        [Title("위젯")]
        [SerializeField, Required("결과 패널 루트")] private GameObject _panel;
        [SerializeField, LabelText("결과 텍스트")] private Text _resultLabel;
        [SerializeField, LabelText("재도전 버튼")] private Button _retryButton;
        [SerializeField, LabelText("메뉴 버튼")] private Button _menuButton;

        [Title("문구")]
        [SerializeField, LabelText("클리어 문구")] private string _clearText = "STAGE CLEAR";
        [SerializeField, LabelText("실패 문구")] private string _failText = "YOU DIED";

        private void Awake()
        {
            if (_director == null) _director = SceneServices.Instance.Director;

            if (_panel != null) _panel.SetActive(false);

            if (_retryButton != null) _retryButton.onClick.AddListener(GameFlow.RestartCurrentScene);
            if (_menuButton != null) _menuButton.onClick.AddListener(GameFlow.LoadMainMenu);

            if (_director == null)
            {
                Debug.LogError($"[{nameof(StageResultView)}] StageDirector를 찾지 못했다. 결과 화면이 뜨지 않는다.", this);
                return;
            }

            _director.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (_director != null) _director.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(StageState state)
        {
            if (state is not (StageState.Cleared or StageState.Failed)) return;

            if (_resultLabel != null)
            {
                _resultLabel.text = state == StageState.Cleared ? _clearText : _failText;
            }

            if (_panel != null) _panel.SetActive(true);
        }
    }
}
