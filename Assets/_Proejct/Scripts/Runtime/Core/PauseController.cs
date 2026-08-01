using Alchemy.Inspector;
using BeatTemplate;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 일시정지 입력을 받고, timeScale로는 멈출 수 없는 것들을 대신 멈춘다.
///
/// PauseState가 전역 플래그와 timeScale을 담당하고, 이 컴포넌트는 씬별 배선(박자 클록 등)만 맡는다.
/// 그래서 BpmClock은 게임 쪽 일시정지 개념을 몰라도 된다.
/// </summary>
public sealed class PauseController : MonoBehaviour
{
    [Title("입력")]
    [SerializeField, LabelText("입력 액션 에셋")] private InputActionAsset _inputActions;

    // 기본값은 기존 에셋에 이미 있는 액션이다. */{Cancel} 바인딩이라 키보드 Esc와 게임패드 모두 잡힌다.
    [SerializeField, LabelText("일시정지 액션 경로")] private string _pauseActionPath = "UI/Cancel";

    [Title("대상 (비우면 자동 탐색)")]
    [SerializeField, LabelText("박자 클록")] private BpmClock _clock;

    [Title("디버그")]
    [ShowInInspector, ReadOnly, LabelText("일시정지 중")]
    private bool IsPausedDebug => PauseState.IsPaused;

    private InputAction _pauseAction;

    private void Awake()
    {
        if (_clock == null) _clock = FindFirstObjectByType<BpmClock>();

        if (_inputActions != null)
        {
            _pauseAction = _inputActions.FindAction(_pauseActionPath, throwIfNotFound: false);
        }

        if (_pauseAction is null)
        {
            Debug.LogWarning(
                $"[{nameof(PauseController)}] '{_pauseActionPath}' 액션을 찾지 못했다. " +
                "입력으로는 일시정지할 수 없고 PauseState.Set 호출만 동작한다.", this);
        }
    }

    private void OnEnable()
    {
        PauseState.Changed += OnPauseChanged;

        if (_pauseAction is null) return;

        _pauseAction.Enable();
        _pauseAction.performed += OnPausePerformed;
    }

    private void OnDisable()
    {
        PauseState.Changed -= OnPauseChanged;

        if (_pauseAction is null) return;

        _pauseAction.performed -= OnPausePerformed;
        _pauseAction.Disable();
    }

    private void OnDestroy()
    {
        // 씬을 벗어날 때 멈춘 채로 남으면 다음 씬이 통째로 정지한다.
        if (PauseState.IsPaused) PauseState.Set(false);
    }

    private void OnPausePerformed(InputAction.CallbackContext _) => PauseState.Toggle();

    private void OnPauseChanged(bool isPaused)
    {
        if (_clock == null) return;

        // dspTime 기반이라 timeScale로는 멈추지 않는다. 명시적으로 세워야 한다.
        if (isPaused) _clock.Pause();
        else _clock.Play();
    }

    [Button, LabelText("일시정지 토글")]
    private void DebugToggle()
    {
        if (Application.isPlaying) PauseState.Toggle();
    }
}
