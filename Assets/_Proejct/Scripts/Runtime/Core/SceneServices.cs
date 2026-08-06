using Alchemy.Inspector;
using BeatTemplate;
using FiveWG.Enemies;
using FiveWG.Player;
using FiveWG.Progression;
using FiveWG.Stage;
using UnityEngine;

namespace FiveWG.Core
{
    /// <summary>
    /// 씬에 하나뿐인 것들을 찾는 창구.
    ///
    /// 예전에는 컴포넌트마다 "인스펙터가 비어 있으면 <c>FindFirstObjectByType</c>"을 각자 들고 있었다.
    /// 9개 파일 14곳으로 늘어나면서 두 가지가 문제가 됐다.
    /// - 배선 실수가 조용히 성공한다. 실제로 리네임으로 참조가 끊겼는데 폴백이 받아내 눈치채기 어려웠다.
    /// - 각 컴포넌트가 "Awake냐 Start냐"를 알아서 지켜야 했다.
    ///
    /// 이제 탐색은 여기 한 곳에서만 일어난다. 각 참조는 처음 요청될 때 채워지므로
    /// 컴포넌트끼리의 Awake 순서에 기대지 않는다.
    ///
    /// 씬에 이 컴포넌트를 두면 인스펙터로 명시 배선할 수 있고, 없으면 요청 시점에 알아서 찾는다.
    /// </summary>
    public sealed class SceneServices : MonoBehaviour
    {
        [Title("명시 배선 (비우면 요청 시 자동 탐색)")]
        [SerializeField, LabelText("박자 클록")] private BpmClock _clock;
        [SerializeField, LabelText("플레이어")] private PlayerController _player;
        [SerializeField, LabelText("플레이어 경험치")] private PlayerExp _playerExp;
        [SerializeField, LabelText("적 스포너")] private EnemySpawner2D _spawner;
        [SerializeField, LabelText("경험치 오브 풀")] private ExpOrbPool _expOrbs;
        [SerializeField, LabelText("스테이지 디렉터")] private StageDirector _director;

        private static SceneServices _instance;

        /// <summary>씬의 서비스 창구. 씬에 컴포넌트가 없으면 임시 창구를 만들어 쓴다.</summary>
        public static SceneServices Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = FindFirstObjectByType<SceneServices>();
                if (_instance == null)
                {
                    // 씬에 두지 않아도 동작하게 한다. 프로토타입 씬을 매번 손보지 않으려는 편의다.
                    _instance = new GameObject(nameof(SceneServices)).AddComponent<SceneServices>();
                }

                return _instance;
            }
        }

        public BpmClock Clock => Resolve(ref _clock);
        public PlayerController Player => Resolve(ref _player);
        public PlayerExp PlayerExp => Resolve(ref _playerExp);
        public EnemySpawner2D Spawner => Resolve(ref _spawner);
        public ExpOrbPool ExpOrbs => Resolve(ref _expOrbs);
        public StageDirector Director => Resolve(ref _director);

        private void Awake()
        {
            // 씬에 두 개 이상 두면 어느 쪽이 이기는지 알 수 없다. 먼저 깬 쪽만 남긴다.
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[{nameof(SceneServices)}] 씬에 둘 이상 있다. 이 인스턴스는 무시된다.", this);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private static T Resolve<T>(ref T field) where T : Component
        {
            if (field == null) field = FindFirstObjectByType<T>();
            return field;
        }
    }
}
