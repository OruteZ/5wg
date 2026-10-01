using Alchemy.Inspector;
using FiveWG.Core;
using FiveWG.Player;
using FiveWG.Progression;
using FiveWG.Stage;
using UnityEngine.UI;
using UnityEngine;

namespace FiveWG.UI
{
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
        [SerializeField, LabelText("경험치 바")] private Slider _expBar;
        [SerializeField, LabelText("레벨 텍스트")] private Text _levelLabel;

        private PlayerController _player;
        private PlayerExp _exp;

        // 마지막으로 화면에 쓴 값. 같으면 문자열을 다시 만들지 않는다.
        private int _shownHealth = int.MinValue;
        private int _shownLevel = int.MinValue;

        private void Awake()
        {
            if (_director == null) _director = SceneServices.Instance.Director;

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
            if (_exp == null && _player != null) _exp = _player.GetComponent<PlayerExp>();

            if (_healthBar != null && _player != null)
            {
                _healthBar.value = _player.HealthNormalized;
            }

            // 텍스트는 값이 실제로 바뀔 때만 다시 만든다. 매 프레임 문자열을 만들면
            // 눈에 보이는 변화 없이 GC만 쌓인다. 슬라이더는 값 대입뿐이라 그냥 매 프레임 넣는다.
            if (_healthLabel != null && _player != null)
            {
                int health = Mathf.CeilToInt(_player.Health);
                if (health != _shownHealth)
                {
                    _shownHealth = health;
                    _healthLabel.text = $"HP {health} / {Mathf.CeilToInt(_player.MaxHealth)}";
                }
            }

            if (_progressBar != null)
            {
                _progressBar.value = _director.Progress;
            }

            if (_expBar != null && _exp != null)
            {
                _expBar.value = _exp.Progress;
            }

            if (_levelLabel != null && _exp != null && _exp.Level != _shownLevel)
            {
                _shownLevel = _exp.Level;
                _levelLabel.text = $"Lv {_shownLevel}";
            }
        }
    }
}
