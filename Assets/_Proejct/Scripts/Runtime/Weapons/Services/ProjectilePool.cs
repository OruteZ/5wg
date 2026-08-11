using Alchemy.Inspector;
using FiveWG.Combat;
using FiveWG.Core;
using FiveWG.Stage;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 무기가 씬에 뿌리는 개체들의 풀. 투사체와 영역(장판·충격파) 두 부류를 함께 소유한다.
    ///
    /// 무기마다 탄과 장판이 다르므로 프리팹별로 풀을 하나씩 두고, 이 클래스는 부류와 프리팹만 고른다.
    /// 부류가 늘 때마다 씬 컴포넌트를 하나씩 늘리지 않으려고 한 곳에 모았다.
    ///
    /// 스테이지가 끝나면 전부 회수한다 — 적이 사라진 뒤 탄과 장판만 남아 떠다니지 않게.
    /// (이름은 투사체만 담당하던 시절 그대로다. 씬·프리팹이 GUID로 참조하고 있어 따로 바꾼다.)
    /// </summary>
    public sealed class ProjectilePool : MonoBehaviour
    {
        [Title("풀")]
        [SerializeField, LabelText("프리팹당 기본 용량")] private int _defaultCapacity = 32;
        [SerializeField, LabelText("프리팹당 최대 크기")] private int _maxSize = 256;

        [Title("스테이지")]
        [SerializeField, LabelText("디렉터 (비우면 자동 탐색). 종료 시 전부 회수")]
        private StageDirector _director;

        [ShowInInspector, ReadOnly, LabelText("등록된 프리팹 수 (탄 / 영역)")]
        private string PrefabCount => $"{_projectiles?.PrefabCount ?? 0} / {_fields?.PrefabCount ?? 0}";

        [ShowInInspector, ReadOnly, LabelText("살아 있는 개체 수 (탄 / 영역)")]
        private string ActiveCount => $"{_projectiles?.ActiveCount ?? 0} / {_fields?.ActiveCount ?? 0}";

        private PrefabPoolSet<Projectile> _projectiles;
        private PrefabPoolSet<DamageField> _fields;
        private bool _isSubscribed;

        private void Awake()
        {
            // WeaponHandler가 Awake에서 무기를 지급하며 바로 꺼내 갈 수 있으므로 Start보다 앞서 만든다.
            _projectiles = new PrefabPoolSet<Projectile>(_defaultCapacity, _maxSize);
            _fields = new PrefabPoolSet<DamageField>(_defaultCapacity, _maxSize);
        }

        private void Start()
        {
            // WeaponHandler가 런타임에 붙일 수도 있어 인스펙터 연결에 기대지 않는다.
            if (_director == null) _director = SceneServices.Instance.Director;

            if (_director != null)
            {
                _director.OnStateChanged += HandleStageStateChanged;
                _isSubscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (_isSubscribed && _director != null) _director.OnStateChanged -= HandleStageStateChanged;

            _projectiles?.Dispose();
            _fields?.Dispose();
        }

        public Projectile Get(Projectile prefab) => _projectiles?.Get(prefab);

        public DamageField Get(DamageField prefab) => _fields?.Get(prefab);

        [Button, LabelText("전부 회수")]
        public void ReleaseAll()
        {
            if (!Application.isPlaying) return;

            _projectiles?.ReleaseAll();
            _fields?.ReleaseAll();
        }

        private void HandleStageStateChanged(StageState state)
        {
            if (state is StageState.Cleared or StageState.Failed) ReleaseAll();
        }
    }
}
