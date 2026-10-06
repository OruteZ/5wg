using Alchemy.Inspector;
using FiveWG.Core;
using FiveWG.Stage;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FiveWG.UI
{
    /// <summary>
    /// ESC로 여닫는 일시정지 메뉴. 무엇을 멈추는지는 디렉터가 알고, 여기는 입력과 패널만 맡는다.
    /// 버튼 배선은 인스펙터의 UnityEvent가 아니라 코드로 건다. 씬 파일에 로직이 숨지 않게.
    /// </summary>
    public sealed class PauseMenuView : MonoBehaviour
    {
        [Title("참조 (비우면 자동 탐색)")]
        [SerializeField, LabelText("스테이지 디렉터")] private StageDirector _director;

        [Title("위젯")]
        [SerializeField, Required("일시정지 패널 루트")] private GameObject _panel;
        [SerializeField, LabelText("계속하기 버튼")] private Button _resumeButton;
        [SerializeField, LabelText("메인 메뉴 버튼")] private Button _menuButton;

        private void Awake()
        {
            if (_director == null) _director = SceneServices.Instance.Director;

            if (_panel != null) _panel.SetActive(false);

            if (_resumeButton != null) _resumeButton.onClick.AddListener(Resume);
            if (_menuButton != null) _menuButton.onClick.AddListener(GameFlow.LoadMainMenu);

            if (_director == null)
            {
                Debug.LogError($"[{nameof(PauseMenuView)}] StageDirector를 찾지 못했다. ESC를 눌러도 멈추지 않는다.", this);
                return;
            }

            _director.OnPauseChanged += HandlePauseChanged;
        }

        private void OnDestroy()
        {
            if (_director != null) _director.OnPauseChanged -= HandlePauseChanged;
        }

        private void Update()
        {
            if (_director == null || Keyboard.current is null) return;

            // ESC는 UI 단축키라 InputActionAsset에 액션을 늘리지 않고 키보드를 직접 읽는다(카드 숫자키와 같다).
            // 판이 끝난 뒤의 ESC는 디렉터가 무시한다.
            if (Keyboard.current.escapeKey.wasPressedThisFrame) _director.SetPaused(!_director.IsPaused);
        }

        private void Resume()
        {
            if (_director != null) _director.SetPaused(false);
        }

        private void HandlePauseChanged(bool paused)
        {
            if (_panel != null) _panel.SetActive(paused);

            // 직전에 누른 버튼이 선택된 채 남으면 다음에 열었을 때 Space 한 번에 그게 눌린다.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
