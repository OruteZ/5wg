using Alchemy.Inspector;
using FiveWG.Core;
using FiveWG.Stage;
using UnityEngine;

namespace FiveWG.Progression
{
    /// <summary>
    /// 경험치 오브의 소유자. 드랍 요청을 받아 풀에서 꺼내 놓고, 스테이지가 끝나면 전부 회수한다.
    ///
    /// 수집 대상(플레이어)을 여기서 한 번만 찾아 오브에 넘긴다. 오브마다 탐색하지 않게.
    /// 스테이지를 구독하는 방향은 Progression → Stage 한 방향이다. 디렉터는 경험치를 모른다.
    /// </summary>
    public sealed class ExpOrbPool : MonoBehaviour
    {
        [Title("대상")]
        [SerializeField, Required("경험치 오브 프리팹")] private ExpOrb _orbPrefab;
        [SerializeField, LabelText("수집자 (비우면 자동 탐색)")] private PlayerExp _collector;

        [Title("스테이지")]
        [SerializeField, LabelText("디렉터 (비우면 자동 탐색). 종료 시 오브 회수")]
        private StageDirector _director;

        [Title("풀")]
        [SerializeField, LabelText("기본 용량")] private int _defaultCapacity = 64;
        [SerializeField, LabelText("최대 크기")] private int _maxSize = 512;

        [ShowInInspector, ReadOnly, LabelText("떠 있는 오브 수")]
        private int ActiveCount => _pool?.ActiveCount ?? 0;

        private PrefabPool<ExpOrb> _pool;
        private bool _isSubscribed;

        private void Awake()
        {
            if (_orbPrefab == null)
            {
                Debug.LogError($"[{nameof(ExpOrbPool)}] 오브 프리팹이 비어 있다. 경험치가 드랍되지 않는다.", this);
                return;
            }

            _pool = new PrefabPool<ExpOrb>(_orbPrefab, "ExpOrbPool", _defaultCapacity, _maxSize);
        }

        private void Start()
        {
            if (_collector == null) _collector = SceneServices.Instance.PlayerExp;
            if (_director == null) _director = SceneServices.Instance.Director;

            if (_collector == null)
            {
                Debug.LogWarning(
                    $"[{nameof(ExpOrbPool)}] PlayerExp를 찾지 못했다. 오브가 수집되지 않는다.", this);
            }

            if (_director != null)
            {
                _director.OnStateChanged += HandleStageStateChanged;
                _isSubscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (_isSubscribed && _director != null) _director.OnStateChanged -= HandleStageStateChanged;

            _pool?.Dispose();
        }

        /// <summary>지정 위치에 경험치 오브를 떨어뜨린다.</summary>
        public void Drop(Vector2 position, float value)
        {
            if (value <= 0f || _pool == null || _collector == null) return;

            ExpOrb orb = _pool.Get();
            orb.Spawn(position, value, _collector.transform, _collector);
        }

        /// <summary>필드에 떠 있는 오브를 전부 끌어온다. 자석 아이템이 여기로 들어온다.</summary>
        [Button, LabelText("오브 전부 끌어오기")]
        public void MagnetizeAll()
        {
            if (_pool == null) return;

            foreach (ExpOrb orb in _pool.Active)
            {
                orb.ForceMagnetize();
            }
        }

        [Button, LabelText("오브 전부 회수")]
        public void ReleaseAll()
        {
            if (!Application.isPlaying) return;
            _pool?.ReleaseAll();
        }

        private void HandleStageStateChanged(StageState state)
        {
            // 종료 후에도 남은 오브가 끌려와 경험치가 더 들어가는 걸 막는다.
            if (state is StageState.Cleared or StageState.Failed) ReleaseAll();
        }
    }
}
