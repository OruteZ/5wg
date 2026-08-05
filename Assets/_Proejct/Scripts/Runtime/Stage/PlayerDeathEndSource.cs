using Alchemy.Inspector;
using UnityEngine;

/// <summary>
/// 플레이어 사망을 스테이지 실패로 옮기는 종료 소스.
/// 사망 처리 자체는 PlayerController가 하고, 여기서는 그 신호를 디렉터 창구로 넘기기만 한다.
/// </summary>
public sealed class PlayerDeathEndSource : StageEndSource
{
    [Title("대상")]
    [SerializeField, LabelText("플레이어 (비우면 자동 탐색)")] private PlayerController _player;

    private bool _isSubscribed;

    protected override void OnBound()
    {
        if (_player == null) _player = FindFirstObjectByType<PlayerController>();

        if (_player == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerDeathEndSource)}] PlayerController를 찾지 못했다. 사망해도 스테이지가 끝나지 않는다.", this);
            return;
        }

        _player.OnDied += HandlePlayerDied;
        _isSubscribed = true;
    }

    private void OnDestroy()
    {
        if (_isSubscribed && _player != null) _player.OnDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied() => RequestFail();
}
