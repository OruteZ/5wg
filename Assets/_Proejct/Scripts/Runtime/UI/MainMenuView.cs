using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 메뉴의 버튼 배선. 씬 이름은 GameFlow만 알고 있다.
/// </summary>
public sealed class MainMenuView : MonoBehaviour
{
    [Title("위젯")]
    [SerializeField, LabelText("시작 버튼")] private Button _startButton;
    [SerializeField, LabelText("종료 버튼")] private Button _quitButton;

    private void Awake()
    {
        if (_startButton != null) _startButton.onClick.AddListener(GameFlow.LoadStage);
        if (_quitButton != null) _quitButton.onClick.AddListener(GameFlow.Quit);
    }
}
