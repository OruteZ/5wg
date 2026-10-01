using System.Collections.Generic;
using Alchemy.Inspector;
using FiveWG.Core;
using FiveWG.Player;
using FiveWG.Progression;
using FiveWG.Weapons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FiveWG.UI
{
    /// <summary>
    /// 레벨업 카드. 레벨이 오르면 화면 중앙 하단에 최대 3장을 띄우고 하나를 고르게 한다.
    ///
    /// **아무것도 멈추지 않는다.** 카드가 떠 있어도 음악·적·발사가 계속 돈다. 시간을 멈추지 않는
    /// 이유는 음악을 끊지 않기 위해서고, 애초에 `Time.timeScale`은 쓸 수 없다 —
    /// `BpmClock`이 `AudioSettings.dspTime` 기준이라 timeScale로는 박이 멈추지 않고 화면과 어긋나기만 한다.
    ///
    /// 고르지 않으면 무한히 기다린다. 그동안 또 레벨업하면 큐에 쌓아 순서대로 소진한다.
    /// </summary>
    public sealed class LevelUpCardView : MonoBehaviour
    {
        [Title("참조 (비우면 자동 탐색)")]
        [SerializeField, LabelText("플레이어")] private PlayerController _player;

        [Title("후보 풀")]
        [SerializeField, LabelText("카드에 뜰 수 있는 무기 전부")]
        private WeaponDefinition[] _weaponPool;

        [Title("위젯")]
        [SerializeField, Required("카드 패널 루트")] private GameObject _panel;

        [SerializeField, LabelText("카드 버튼 (최대 3개)")] private Button[] _cardButtons;
        [SerializeField, LabelText("카드 제목 텍스트")] private Text[] _cardTitles;
        [SerializeField, LabelText("카드 설명 텍스트")] private Text[] _cardDetails;
        [SerializeField, LabelText("대기 중인 레벨업 수 텍스트")] private Text _queueLabel;

        [Title("소모성 보상")]
        [SerializeField, LabelText("고를 강화가 없을 때 회복량")] private float _healAmount = 25f;

        [Title("디버그")]
        [ShowInInspector, ReadOnly, LabelText("대기 중인 레벨업")]
        private int PendingDebug => _pending;

        [ShowInInspector, ReadOnly, LabelText("카드 떠 있음")]
        private bool ShowingDebug => _shown.Count > 0;

        private readonly List<LevelUpOption> _shown = new();

        private PlayerExp _exp;
        private WeaponHandler _handler;
        private int _pending;

        private void Awake()
        {
            if (_player == null) _player = SceneServices.Instance.Player;

            if (_player == null)
            {
                Debug.LogError($"[{nameof(LevelUpCardView)}] 플레이어를 찾지 못했다. 레벨업 카드가 뜨지 않는다.", this);
                return;
            }

            _exp = _player.GetComponent<PlayerExp>();
            _handler = _player.GetComponent<WeaponHandler>();

            if (_exp == null)
            {
                Debug.LogError($"[{nameof(LevelUpCardView)}] 플레이어에 {nameof(PlayerExp)}가 없다.", this);
                return;
            }

            // 풀이 비면 카드가 영영 안 뜨고 매 레벨업이 조용히 회복으로만 처리된다.
            // 첫 레벨업까지 기다리면 원인을 찾기 어려우므로 시작 시점에 알린다.
            if (_weaponPool is not { Length: > 0 })
            {
                Debug.LogError($"[{nameof(LevelUpCardView)}] 무기 풀이 비어 있다. 카드가 뜨지 않는다.", this);
            }

            // 버튼 배선은 인스펙터 UnityEvent가 아니라 코드로 건다.
            for (int i = 0; i < (_cardButtons?.Length ?? 0); i++)
            {
                if (_cardButtons[i] == null) continue;

                int index = i;
                _cardButtons[i].onClick.AddListener(() => Choose(index));
            }

            _exp.OnLeveledUp += HandleLeveledUp;
            Hide();
        }

        private void OnDestroy()
        {
            if (_exp != null) _exp.OnLeveledUp -= HandleLeveledUp;
        }

        /// <summary>인자인 레벨은 쓰지 않는다. 몇 번 남았는지만 알면 되기 때문.</summary>
        private void HandleLeveledUp(int level)
        {
            _pending++;
            if (_shown.Count == 0) ShowNext();
            else UpdateQueueLabel();
        }

        private void Update()
        {
            if (_shown.Count == 0 || Keyboard.current is null) return;

            // 숫자키는 UI 단축키라 InputActionAsset에 액션을 늘리지 않고 키보드를 직접 읽는다.
            if (Keyboard.current.digit1Key.wasPressedThisFrame) Choose(0);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) Choose(1);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame) Choose(2);
        }

        private void ShowNext()
        {
            if (_pending <= 0 || _handler == null)
            {
                Hide();
                return;
            }

            LevelUpDraft.Build(_handler.Inventory, _weaponPool, _shown);

            // 고를 것이 없는 회차(슬롯이 차고 전부 만렙)는 카드를 띄우지 않고 소모성 보상을 바로 준다.
            if (_shown.Count == 0)
            {
                _player.Heal(_healAmount);
                _pending--;

                if (_pending > 0) ShowNext();
                else Hide();
                return;
            }

            for (int i = 0; i < (_cardButtons?.Length ?? 0); i++)
            {
                bool used = i < _shown.Count;
                if (_cardButtons[i] != null) _cardButtons[i].gameObject.SetActive(used);
                if (!used) continue;

                if (_cardTitles != null && i < _cardTitles.Length && _cardTitles[i] != null)
                {
                    _cardTitles[i].text = $"{i + 1}. {_shown[i].Title}";
                }

                if (_cardDetails != null && i < _cardDetails.Length && _cardDetails[i] != null)
                {
                    _cardDetails[i].text = _shown[i].Detail;
                }
            }

            if (_panel != null) _panel.SetActive(true);

            // 직전 회차에 클릭한 버튼이 선택된 채로 남으면, 다음 카드가 떴을 때
            // Submit(Enter·Space) 한 번에 그게 먹힌다. 띄울 때마다 선택을 비워 끊는다.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            UpdateQueueLabel();
        }

        private void Choose(int index)
        {
            if (index < 0 || index >= _shown.Count) return;

            _shown[index].Apply(_handler.Inventory);
            _shown.Clear();
            _pending--;

            if (_pending > 0) ShowNext();
            else Hide();
        }

        private void Hide()
        {
            _shown.Clear();
            if (_panel != null) _panel.SetActive(false);
        }

        private void UpdateQueueLabel()
        {
            if (_queueLabel == null) return;

            // 지금 띄운 한 장을 뺀 나머지가 대기다.
            int waiting = Mathf.Max(0, _pending - 1);
            _queueLabel.gameObject.SetActive(waiting > 0);
            if (waiting > 0) _queueLabel.text = $"+{waiting}";
        }
    }
}
