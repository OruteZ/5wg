using System;
using UnityEngine;

/// <summary>
/// 게임 전역 일시정지 상태.
///
/// 정적 클래스인 이유: 일시정지는 Time.timeScale과 마찬가지로 본질적으로 전역이고,
/// 아무 컴포넌트나 OnEnable에서 구독할 수 있어야 해서 인스턴스 초기화 순서에 얽히면 안 된다.
/// 대신 상태를 여기서만 바꾸고, 개별 반응은 Changed 구독으로 처리한다.
///
/// timeScale 0으로 멈추는 것: FixedUpdate·물리·Time.deltaTime 기반 로직 전부.
/// 따로 처리해야 하는 것: AudioSettings.dspTime을 쓰는 BpmClock, 그리고 unscaled 시간을 쓰는 코루틴.
/// </summary>
public static class PauseState
{
    private static float _timeScaleBeforePause = 1f;

    public static bool IsPaused { get; private set; }

    /// <summary>인자는 새 일시정지 상태. 값이 실제로 바뀔 때만 발생한다.</summary>
    public static event Action<bool> Changed;

    public static void Set(bool paused)
    {
        if (IsPaused == paused) return;

        IsPaused = paused;

        if (paused)
        {
            // 슬로모션 등으로 1이 아닐 수 있으므로 복원용으로 기억해둔다.
            _timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = _timeScaleBeforePause;
        }

        Changed?.Invoke(paused);
    }

    public static void Toggle() => Set(!IsPaused);

    /// <summary>
    /// 도메인 리로드를 끈 상태(Enter Play Mode Options)에서도 정적 상태가 남지 않게 초기화한다.
    /// 이게 없으면 일시정지 상태로 플레이를 멈췄을 때 다음 실행이 멈춘 채로 시작한다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnEnterPlayMode()
    {
        IsPaused = false;
        _timeScaleBeforePause = 1f;
        Changed = null;
        Time.timeScale = 1f;
    }
}
