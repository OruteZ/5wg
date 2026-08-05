using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 진행 중 상시 표시되는 그레이박스 HUD. 체력과 목표 진행도만 그린다.
/// 값은 매 프레임 폴링한다. 갱신 빈도가 문제될 규모가 아니고, 이벤트 배선을 줄이는 쪽이 낫다.
/// </summary>
public sealed class StageHud : MonoBehaviour
{
    [Title("참조 (비우면 자동 탐색)")]
    [SerializeField, LabelText("스테이지 디렉터")] private StageDirector _director;

    [Title("위젯")]
    [SerializeField, LabelText("체력 바")] private Slider _healthBar;
    [SerializeField, LabelText("진행도 바")] private Slider _progressBar;
    [SerializeField, LabelText("체력 수치 텍스트")] private Text _healthLabel;
    [SerializeField, LabelText("경험치 바")] private Slider _experienceBar;
    [SerializeField, LabelText("레벨 텍스트")] private Text _levelLabel;

    private PlayerController _player;
    private PlayerExperience _experience;

    private void Awake()
    {
        if (_director == null) _director = FindFirstObjectByType<StageDirector>();

        if (_director == null)
        {
            Debug.LogError($"[{nameof(StageHud)}] StageDirector를 찾지 못했다. HUD가 갱신되지 않는다.", this);
        }
    }

    private void Update()
    {
        if (_director == null) return;

        // 디렉터가 Awake에서 플레이어를 찾으므로 첫 프레임 이후부터 잡힌다.
        if (_player == null) _player = _director.Player;
        if (_experience == null && _player != null) _experience = _player.GetComponent<PlayerExperience>();

        if (_healthBar != null && _player != null)
        {
            _healthBar.value = _player.HealthNormalized;
        }

        if (_healthLabel != null && _player != null)
        {
            _healthLabel.text = $"HP {Mathf.CeilToInt(_player.Health)} / {Mathf.CeilToInt(_player.MaxHealth)}";
        }

        if (_progressBar != null)
        {
            _progressBar.value = _director.Progress;
        }

        if (_experienceBar != null && _experience != null)
        {
            _experienceBar.value = _experience.Progress;
        }

        if (_levelLabel != null && _experience != null)
        {
            _levelLabel.text = $"Lv {_experience.Level}";
        }
    }
}
