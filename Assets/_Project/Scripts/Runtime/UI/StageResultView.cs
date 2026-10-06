using System.Text;
using Alchemy.Inspector;
using FiveWG.Core;
using FiveWG.Enemies;
using FiveWG.Pickup;
using FiveWG.Player;
using FiveWG.Progression;
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
        [SerializeField, LabelText("집계 항목 이름")] private Text _statNames;
        [SerializeField, LabelText("집계 항목 값")] private Text _statValues;

        [Title("문구")]
        [SerializeField, LabelText("클리어 문구")] private string _clearText = "STAGE CLEAR";
        [SerializeField, LabelText("실패 문구")] private string _failText = "YOU DIED";

        // 결과창은 판이 끝날 때 한 번만 그리므로 할당을 아끼지 않는다.
        private readonly StringBuilder _names = new();
        private readonly StringBuilder _values = new();

        private UiSfx _sfx;

        private void Awake()
        {
            _sfx = GetComponent<UiSfx>();

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

            FillStats();

            if (_sfx != null) _sfx.PlayResult(state == StageState.Cleared);

            if (_panel != null) _panel.SetActive(true);
        }

        /// <summary>
        /// 집계는 각자가 이미 들고 있는 값을 모아서 보여주기만 한다.
        /// 결과창용 집계기를 따로 두면 같은 수를 두 곳에서 세게 된다.
        /// </summary>
        private void FillStats()
        {
            if (_statNames == null && _statValues == null) return;

            _names.Clear();
            _values.Clear();

            if (SceneServices.Instance.Spawner is EnemySpawnDirector spawner)
            {
                Row("처치", $"{spawner.DefeatedCount:N0}마리");
                Row("엘리트", $"{spawner.EliteDefeatedCount}마리");
                Row("생존", $"{Mathf.FloorToInt(spawner.ElapsedSec / 60f)}:{Mathf.FloorToInt(spawner.ElapsedSec % 60f):00}");

                // 보스가 없는 판에서 "미처치"라고 적으면 못 잡은 것처럼 읽힌다.
                if (spawner.BossSpawnBar > 0f) Row("보스", spawner.BossDefeated ? "처치" : "미처치");
            }

            PlayerExp exp = SceneServices.Instance.PlayerExp;
            if (exp != null)
            {
                Row("레벨", $"Lv {exp.Level}");
                Row("레벨업", $"{exp.Level - 1}회");
            }

            PlayerController player = SceneServices.Instance.Player;
            PlayerInventory inventory = player != null ? player.GetComponent<PlayerInventory>() : null;
            if (inventory != null) Row("티켓", $"{inventory.Tickets:N0}장");

            if (_statNames != null) _statNames.text = _names.ToString();
            if (_statValues != null) _statValues.text = _values.ToString();
        }

        private void Row(string name, string value)
        {
            if (_names.Length > 0)
            {
                _names.Append('\n');
                _values.Append('\n');
            }

            _names.Append(name);
            _values.Append(value);
        }
    }
}
