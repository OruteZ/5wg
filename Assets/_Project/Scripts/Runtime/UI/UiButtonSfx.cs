using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FiveWG.UI
{
    /// <summary>
    /// 버튼 하나에 누름·올림 소리를 붙인다. 버튼마다 스크립트가 각자 소리를 내면
    /// 새 버튼을 만들 때마다 같은 코드를 또 쓰게 되므로 여기 한 곳에 둔다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UiButtonSfx : MonoBehaviour, IPointerEnterHandler
    {
        private UiSfx _sfx;
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _sfx = GetComponentInParent<UiSfx>(includeInactive: true);

            if (_sfx == null)
            {
                Debug.LogError(
                    $"[{nameof(UiButtonSfx)}] 위쪽에 {nameof(UiSfx)}가 없다. 이 버튼은 소리가 나지 않는다.", this);
                return;
            }

            _button.onClick.AddListener(_sfx.PlayClick);
        }

        private void OnDestroy()
        {
            if (_button != null && _sfx != null) _button.onClick.RemoveListener(_sfx.PlayClick);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_sfx == null || !_button.interactable) return;

            _sfx.PlayHover();
        }
    }
}
