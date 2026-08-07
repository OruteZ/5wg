using System.Collections.Generic;
using System.Linq;
using Alchemy.Inspector;
using BeatTemplate;
using FiveWG.Core;
using FiveWG.Weapons;
using UnityEngine;

namespace FiveWG.Player
{
    /// <summary>
    /// 보유 무기를 관리하고 비트 틱마다 발사 여부를 판정하는 조정자.
    /// "언제 쏘는지"는 각 무기의 IFireTiming이, "쏴도 되는지"는 IFireGate가 정한다.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class WeaponHandler : MonoBehaviour
    {
        [Title("비트")]
        [SerializeField, Required("박자 소스가 필요하다.")] private BpmClock _clock;

        [SerializeField, LabelText("한 프레임 최대 보정 틱 수")]
        private int _maxCatchUpTicks = 8;

        [Title("서비스 (비우면 자동 생성)")]
        [SerializeField, LabelText("투사체 풀")] private ProjectilePool _projectilePool;
        [SerializeField, LabelText("타겟 탐색")] private RegistryTargetProvider _targetProvider;
        [SerializeField, LabelText("발사구 (비우면 자기 자신)")] private Transform _muzzle;

        [Title("시작 무기")]
        [SerializeField, LabelText("시작 시 지급 (최대 6)")]
        private WeaponDefinition[] _startingWeapons;

        [Title("디버그")]
        [ShowInInspector, ReadOnly, LabelText("클록 상태")]
        private string ClockStateDebug => _clock == null ? "클록 미연결" : _clock.State.ToString();

        [ShowInInspector, ReadOnly, LabelText("보유 무기 수")]
        private int OwnedCount => Inventory?.Count ?? 0;

        [ShowInInspector, ReadOnly, LabelText("전역 발사 허용")]
        private bool AttackEnabledDebug => AttackEnabled;

        [ShowInInspector, ReadOnly, LabelText("등록된 게이트 수")]
        private int GateCount => _gates.Count;

        [ShowInInspector, ReadOnly, LabelText("마지막 서브비트 인덱스")]
        private long LastSubIndexDebug => _lastSubIndex;

        private readonly List<IFireGate> _gates = new();

        private PlayerMovement _movement;
        private Camera _camera;
        private Vector2 _aimInput;
        private long _lastSubIndex = -1;

        /// <summary>보유 무기 목록. 획득·업그레이드·UI 연동은 이쪽으로 한다.</summary>
        public WeaponInventory Inventory { get; private set; }

        /// <summary>전투 전역 on/off. 컷신·상점처럼 일시적으로 전부 멈출 때 쓴다.</summary>
        public bool AttackEnabled { get; set; } = true;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _camera = Camera.main;
            if (_muzzle == null) _muzzle = transform;

            EnsureServices();

            // 쏘는 쪽의 편은 이 핸들러를 들고 있는 주체가 정한다. 적이 무기를 들어도 그대로 동작한다.
            Faction faction = TryGetComponent(out IDamageable owner) ? owner.Faction : Faction.Player;

            Inventory = new WeaponInventory(
                new WeaponContext(transform, _projectilePool, _targetProvider, _camera, faction));

            GrantStartingWeapons();
            WarnIfCannotFire();
        }

        /// <summary>
        /// 발사가 조용히 안 되는 상황(클록 미연결·무기 미지급)은 원인을 찾기 어려우므로 시작 시 한 번 알린다.
        /// </summary>
        private void WarnIfCannotFire()
        {
            if (_clock == null)
            {
                // 씬에 클록이 하나뿐인 프로토타입 단계에서는 자동 탐색으로 연결 실수를 흡수한다.
                _clock = SceneServices.Instance.Clock;
            }

            if (_clock == null)
            {
                Debug.LogError(
                    $"[{nameof(WeaponHandler)}] BpmClock을 찾지 못했다. 박자 틱이 없어 발사가 일어나지 않는다.", this);
            }

            if (Inventory.Count == 0)
            {
                Debug.LogWarning(
                    $"[{nameof(WeaponHandler)}] 보유 무기가 없다. 시작 무기에 WeaponDefinition을 넣어야 발사된다.", this);
            }
        }

        private void EnsureServices()
        {
            if (_projectilePool == null) _projectilePool = SceneServices.Instance.Projectiles;

            // 반면 타겟 탐색은 소유자별 설정이다(겨눌 편이 주체마다 다르다). 그래서 여기 남는다.
            if (_targetProvider == null)
            {
                _targetProvider = GetComponent<RegistryTargetProvider>();
            }

            if (_targetProvider == null)
            {
                Debug.LogWarning(
                    $"[{nameof(WeaponHandler)}] {nameof(RegistryTargetProvider)}가 없어 자동으로 붙였다. 겨눌 편을 정하려면 씬에 명시하는 게 좋다.",
                    this);
                _targetProvider = gameObject.AddComponent<RegistryTargetProvider>();
            }
        }

        private void GrantStartingWeapons()
        {
            if (_startingWeapons is null) return;

            foreach (WeaponDefinition definition in _startingWeapons)
            {
                if (definition == null) continue;

                if (!Inventory.TryAcquire(definition, out _))
                {
                    Debug.LogWarning(
                        $"[{nameof(WeaponHandler)}] '{definition.DisplayName}' 지급 실패. 슬롯이 {WeaponInventory.Capacity}칸을 넘었다.",
                        this);
                }
            }
        }

        /// <summary>PlayerController가 매 프레임 전달하는 Look 입력을 받는다. 타겟이 없을 때의 조준 폴백.</summary>
        public void SetAimInput(Vector2 aimInput) => _aimInput = aimInput;

        public void AddGate(IFireGate gate)
        {
            if (gate is null || _gates.Contains(gate)) return;
            _gates.Add(gate);
        }

        public bool RemoveGate(IFireGate gate) => _gates.Remove(gate);

        private void Update()
        {
            Inventory.Tick(Time.deltaTime);
            PumpBeatTicks();
        }

        /// <summary>
        /// 클록을 폴링해 지난 프레임 이후 넘어간 서브비트를 전부 틱으로 흘린다.
        /// 프레임이 밀려도 발사가 통째로 누락되지 않게 하되, 과도한 몰아치기는 한도로 막는다.
        /// </summary>
        private void PumpBeatTicks()
        {
            if (_clock == null || _clock.State != BeatState.Playing)
            {
                // 정지·일시정지 중에는 앵커를 버려 재개 시 밀린 틱이 한꺼번에 터지지 않게 한다.
                _lastSubIndex = -1;
                return;
            }

            int subPerBeat = Mathf.Max(1, _clock.SubPerBeat);
            long current = (long)_clock.CurrentBeat * subPerBeat + _clock.CurrentSubBeat;

            // 첫 프레임이거나 곡이 되감긴 경우 현재 위치에 다시 앵커한다.
            if (_lastSubIndex < 0 || current < _lastSubIndex)
            {
                _lastSubIndex = current - 1;
            }

            if (current - _lastSubIndex > _maxCatchUpTicks)
            {
                _lastSubIndex = current - _maxCatchUpTicks;
            }

            for (long index = _lastSubIndex + 1; index <= current; index++)
            {
                DispatchTick(index, subPerBeat);
            }

            _lastSubIndex = current;
        }

        private void DispatchTick(long subIndex, int subPerBeat)
        {
            if (!AttackEnabled) return;

            BeatTick tick = new(
                beat: (int)(subIndex / subPerBeat),
                sub: (int)(subIndex % subPerBeat),
                subPerBeat: subPerBeat,
                bpm: _clock.Bpm,
                dspTime: AudioSettings.dspTime);

            // 조준 방향은 여기서 정하지 않는다. 무기마다 조준 방식이 다르므로 원본 단서만 넘긴다.
            FireContext context = new(_muzzle.position, _aimInput, _movement.FacingDirection, tick);

            for (int slot = 0; slot < WeaponInventory.Capacity; slot++)
            {
                IWeapon weapon = Inventory.GetAt(slot);
                if (weapon is null || !weapon.IsReady) continue;
                if (!AllGatesAllow(weapon)) continue;
                if (!weapon.Timing.ShouldFire(tick)) continue;

                weapon.Fire(context);
            }
        }

        private bool AllGatesAllow(IWeapon weapon)
        {
            for (int i = 0; i < _gates.Count; i++)
            {
                if (!_gates[i].IsAllowed(weapon)) return false;
            }

            return true;
        }

        [Button, LabelText("보유 무기 전부 레벨업")]
        private void DebugUpgradeAll()
        {
            if (!Application.isPlaying) return;

            for (int slot = 0; slot < WeaponInventory.Capacity; slot++)
            {
                Inventory.TryUpgradeAt(slot);
            }

            // 인스펙터의 ShowInInspector 값은 UI를 만들 때 한 번만 읽혀서 눌러도 화면이 그대로다.
            // 콘솔은 항상 갱신되므로 결과를 여기로 뱉는다.
            Debug.Log(string.Join("\n", Enumerable.Range(0, WeaponInventory.Capacity).Select(i =>
                Inventory.GetAt(i) is { } w
                    ? $"{i}: {w.Definition.DisplayName} Lv {w.Level}/{w.Definition.MaxLevel}"
                    : $"{i}: -")), this);
        }
    }
}
