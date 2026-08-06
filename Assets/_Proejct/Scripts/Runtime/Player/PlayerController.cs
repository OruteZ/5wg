using System;
using Alchemy.Inspector;
using FiveWG.Core;
using FiveWG.Weapons;
using UnityEngine.InputSystem;
using UnityEngine;

namespace FiveWG.Player
{
    /// <summary>
    /// 플레이어 입력을 한 곳에서 읽어 PlayerMovement/WeaponHandler에 전달하고,
    /// 체력 등 플레이어 전반의 상태를 관리하는 조정자.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(WeaponHandler))]
    public sealed class PlayerController : MonoBehaviour, IDamageable
    {
        [Title("입력")]
        [SerializeField, Required("Player 액션맵을 가진 InputActionAsset이 필요하다.")]
        private InputActionAsset _inputActions;

        // 무기가 전부 자동공격이라 Attack 액션은 쓰지 않는다.
        [SerializeField, LabelText("Move 액션 경로")] private string _moveActionPath = "Player/Move";
        [SerializeField, LabelText("Look 액션 경로")] private string _lookActionPath = "Player/Look";

        [Title("체력")]
        [SerializeField, LabelText("최대 체력")] private float _maxHealth = 100f;

        [ShowInInspector, ReadOnly, LabelText("현재 체력")]
        private float _health;

        private InputAction _moveAction;
        private InputAction _lookAction;

        private PlayerMovement _movement;
        private WeaponHandler _weapon;

        public bool IsAlive => _health > 0f;

        public Faction Faction => Faction.Player;

        /// <summary>입력 수용 여부. 스테이지 시작 전/종료 후에 StageDirector가 잠근다.</summary>
        public bool ControlEnabled { get; set; } = true;

        public float MaxHealth => _maxHealth;

        public float Health => _health;

        /// <summary>0~1 체력 비율. HUD용.</summary>
        public float HealthNormalized => _maxHealth <= 0f ? 0f : Mathf.Clamp01(_health / _maxHealth);

        public event Action OnDied;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _weapon = GetComponent<WeaponHandler>();

            _health = _maxHealth;

            _moveAction = ResolveAction(_moveActionPath);
            _lookAction = ResolveAction(_lookActionPath);

            // 사망 시 발사가 멈추는 규칙을 게이트로 넘긴다. WeaponHandler는 플레이어를 몰라도 된다.
            _weapon.AddGate(new DelegateFireGate(() => IsAlive));
        }

        private InputAction ResolveAction(string path)
        {
            // throwIfNotFound: false → 인스펙터 연결 실수를 예외 대신 로그로 잡는다.
            InputAction action = _inputActions == null
                ? null
                : _inputActions.FindAction(path, throwIfNotFound: false);

            if (action is null)
            {
                Debug.LogError($"[{nameof(PlayerController)}] '{path}' 액션을 찾지 못했다.", this);
            }

            return action;
        }

        // InputAction의 소유자는 에셋이므로 Dispose가 아니라 Enable/Disable로 수명만 맞춘다.
        private void OnEnable()
        {
            _moveAction?.Enable();
            _lookAction?.Enable();

            // 플레이어도 피격 대상이다. 적이 무기를 들게 되면 이 목록에서 찾는다.
            TargetRegistry.Register(this, Faction);
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _lookAction?.Disable();

            TargetRegistry.Unregister(this, Faction);
        }

        private void Update()
        {
            if (!IsAlive) return;

            if (!ControlEnabled)
            {
                // 입력이 잠긴 동안 마지막 입력이 남아 계속 미끄러지지 않게 매 프레임 0으로 눌러둔다.
                _movement.SetMoveInput(Vector2.zero);
                return;
            }

            _movement.SetMoveInput(_moveAction?.ReadValue<Vector2>() ?? Vector2.zero);
            _weapon.SetAimInput(_lookAction?.ReadValue<Vector2>() ?? Vector2.zero);
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return;

            _health = Mathf.Max(0f, _health - amount);
            if (_health <= 0f) Die();
        }

        private void Die()
        {
            // 발사 차단은 Awake에서 등록한 생존 게이트가 처리한다.
            _movement.SetMoveInput(Vector2.zero);
            OnDied?.Invoke();
        }
    }
}
