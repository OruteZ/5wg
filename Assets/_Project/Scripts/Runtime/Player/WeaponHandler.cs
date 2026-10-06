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

        [Title("악기 루프 사운드")]
        [SerializeField, LabelText("악기 루프 음량 (0~1)")] private float _instrumentVolume = 0.5f;

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

        // 슬롯당 악기 루프 하나. 장착 중 계속 도는 마디 루프라 발사 이벤트가 아니라
        // 인벤토리의 획득·해제 이벤트에 붙는다.
        private readonly AudioSource[] _instrumentLoops = new AudioSource[WeaponInventory.Capacity];
        private BeatState _lastClockState = BeatState.Idle;

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

            Inventory.Acquired += HandleWeaponAcquired;
            Inventory.Removed += HandleWeaponRemoved;

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

            for (int i = 0; i < _instrumentLoops.Length; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.loop = true;
                source.volume = _instrumentVolume;
                _instrumentLoops[i] = source;
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
            SyncLoopsToClockState();
        }

        /// <summary>
        /// 악기 루프는 클록 상태를 그대로 따른다. 일시정지·스테이지 종료를 부르는 쪽이 각자 루프를
        /// 멈추게 하면 한 곳만 빠뜨려도 소리만 계속 도는 상태가 되므로, 클록 하나만 보고 여기서 맞춘다.
        /// 시작 무기는 클록이 돌기 전(Awake)에 지급되므로 그때는 클립만 끼워 두고, 클록이 Playing이
        /// 되는 순간(스테이지 시작·재개) 전부 다시 스케줄한다.
        /// </summary>
        private void SyncLoopsToClockState()
        {
            BeatState state = _clock != null ? _clock.State : BeatState.Idle;
            if (state == _lastClockState) return;

            BeatState previous = _lastClockState;
            _lastClockState = state;

            switch (state)
            {
                case BeatState.Playing:
                    // 재개는 클록이 멈춘 박 위치에서 이어지므로 마디 경계를 기다리지 않고 바로 잇는다.
                    RescheduleAllInstrumentLoops(resumeImmediately: previous == BeatState.Paused);
                    break;

                case BeatState.Paused:
                    // Pause가 아니라 Stop한다. 재개 때 어차피 클록 위치로 다시 예약하므로 AudioSource가
                    // 기억한 재생 위치는 쓰지 않는다 — UnPause로 이으면 예약 대기 중이던 루프가 꼬인다.
                    StopAllInstrumentLoops();
                    break;

                default:
                    StopAllInstrumentLoops();
                    break;
            }
        }

        private void StopAllInstrumentLoops()
        {
            foreach (AudioSource source in _instrumentLoops)
            {
                if (source != null) source.Stop();
            }
        }

        /// <summary>
        /// 전 슬롯을 한 시각 기준으로 다시 건다. 클립 준비(로드)를 먼저 전부 끝내고 시각은 그 뒤에 한 번만
        /// 읽는다 — 슬롯마다 따로 읽으면 앞 슬롯의 로드가 메인 스레드를 잡는 동안 dspTime이 흘러,
        /// 시작 무기가 여럿일 때 루프끼리 수십 ms씩 어긋났다.
        /// </summary>
        private void RescheduleAllInstrumentLoops(bool resumeImmediately)
        {
            if (_clock == null || _clock.State != BeatState.Playing) return;

            for (int slot = 0; slot < WeaponInventory.Capacity; slot++)
            {
                IWeapon weapon = Inventory.GetAt(slot);
                if (weapon != null) PrepareInstrumentLoop(slot, weapon);
            }

            double now = AudioSettings.dspTime;
            double elapsed = _clock.ElapsedSec;

            foreach (AudioSource source in _instrumentLoops)
            {
                if (source != null && source.clip != null) ScheduleAlignedToClock(source, now, elapsed, resumeImmediately);
            }
        }

        private void HandleWeaponAcquired(int slot, IWeapon weapon)
        {
            if (!PrepareInstrumentLoop(slot, weapon)) return;

            // 클록이 돌지 않으면(시작 무기 지급 시점·일시정지·종료) 클립만 끼워 두고 틀지 않는다 —
            // 클록이 Playing이 되면 SyncLoopsToClockState가 다시 스케줄한다.
            if (_clock.State != BeatState.Playing) return;

            ScheduleAlignedToClock(_instrumentLoops[slot], AudioSettings.dspTime, _clock.ElapsedSec, resumeImmediately: false);
        }

        private void HandleWeaponRemoved(int slot, IWeapon weapon)
        {
            AudioSource source = _instrumentLoops[slot];
            if (source == null) return;

            source.Stop();
            source.clip = null;
        }

        /// <summary>
        /// 그 악기의 클립을 슬롯에 끼우고 오디오 데이터를 미리 올린다. 틀지는 않는다.
        /// 악기 샘플은 Preload Audio Data가 꺼져 있어, 미리 올리지 않으면 첫 재생 순간에 동기 로드가
        /// 일어나고 그동안 예약 시각이 이미 지나가 버린다.
        /// </summary>
        private bool PrepareInstrumentLoop(int slot, IWeapon weapon)
        {
            AudioSource source = _instrumentLoops[slot];
            if (source == null || _clock == null) return false;

            // 곡 BPM은 항상 녹음된 4단계(80/90/100/110) 중 하나여야 한다 — 피치로 억지로
            // 맞추면 음정이 틀어진다. 클록 BPM이 그 목록 밖이면 가장 가까운 샘플을 골라도
            // 마디 길이가 안 맞아 돌수록 어긋나니, 그건 재생 속도가 아니라 클록 쪽을 고쳐야 한다.
            if (!weapon.Definition.TryGetClipForBpm(_clock.Bpm, out AudioClip clip, out int clipBpm)) return false;

            if (clipBpm != _clock.Bpm)
            {
                Debug.LogWarning(
                    $"[{nameof(WeaponHandler)}] '{weapon.Definition.DisplayName}' 클록 BPM({_clock.Bpm})에 맞는 샘플이 없어 " +
                    $"{clipBpm}bpm으로 대신 재생한다. 마디가 진행될수록 다른 악기와 어긋난다.", this);
            }

            // 백그라운드 로드가 꺼진 클립이라 여기서 끝까지 로드된다.
            if (clip.loadState != AudioDataLoadState.Loaded) clip.LoadAudioData();

            source.Stop();
            source.clip = clip;
            source.volume = _instrumentVolume;
            source.pitch = 1f;
            return true;
        }

        /// <summary>
        /// 다음 마디 경계에 틀되, 클립 처음이 아니라 클록 위치에 해당하는 지점부터 튼다.
        /// 샘플은 1마디가 아니라 8마디 프레이즈라서, 처음부터 틀면 마디 박자는 맞아도 이미 도는
        /// 다른 악기와 프레이즈가 몇 마디씩 어긋난다(3마디째에 습득한 악기는 영영 3마디 늦게 돈다).
        /// 지금 당장이 아니라 PlayScheduled로 예약하는 이유는, 이미 도는 다른 악기들과 위상이 맞아야 하기 때문이다.
        /// </summary>
        private void ScheduleAlignedToClock(AudioSource source, double now, double elapsed, bool resumeImmediately)
        {
            // PlayScheduled에 지금이나 지난 시각을 주면 오디오 스레드가 집어 가는 버퍼에서야 시작해
            // 시작 지점이 소스마다 달라진다. 바로 틀 때도 약간 앞을 예약하고 클립 위치도 그만큼 당긴다.
            const double LeadSec = 0.1;

            double secPerBar = 60.0 / _clock.Bpm * CellMath.BeatsPerBar;
            double phase = elapsed % secPerBar;

            // 재개는 클록이 멈춘 박 위치에서 바로 잇는다. 이미 마디 경계 근처면(스테이지 막 시작한 순간 등)
            // 한 마디를 통째로 기다리지 않고 바로 튼다 — 공격은 칸 0에서 바로 나가는데 소리만 한 마디 늦게
            // 시작하는 것처럼 들렸다.
            double startElapsed = resumeImmediately || phase < LeadSec
                ? elapsed + LeadSec
                : elapsed - phase + secPerBar;

            PlayFromClockPosition(source, source.clip, now + (startElapsed - elapsed), startElapsed);
        }

        private static void PlayFromClockPosition(AudioSource source, AudioClip clip, double startDsp, double clockSec)
        {
            double clipSec = (double)clip.samples / clip.frequency;
            double offsetSec = clockSec % clipSec;
            source.timeSamples = Mathf.Clamp((int)(offsetSec * clip.frequency), 0, clip.samples - 1);
            source.PlayScheduled(startDsp);
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
                if (weapon is null) continue;
                if (!AllGatesAllow(weapon)) continue;
                if (!weapon.Timing.ShouldFire(tick, weapon.Level)) continue;

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
