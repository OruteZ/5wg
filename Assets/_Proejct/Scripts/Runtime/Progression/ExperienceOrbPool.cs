using System;
using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 경험치 오브의 소유자. 드랍 요청을 받아 풀에서 꺼내 놓고, 스테이지가 끝나면 전부 회수한다.
///
/// 수집 대상(플레이어)을 여기서 한 번만 찾아 오브에 넘긴다. 오브마다 탐색하지 않게.
/// 스테이지를 구독하는 방향은 Progression → Stage 한 방향이다. 디렉터는 경험치를 모른다.
/// </summary>
public sealed class ExperienceOrbPool : MonoBehaviour
{
    [Title("대상")]
    [SerializeField, Required("경험치 오브 프리팹")] private ExperienceOrb _orbPrefab;
    [SerializeField, LabelText("수집자 (비우면 자동 탐색)")] private PlayerExperience _collector;

    [Title("스테이지")]
    [SerializeField, LabelText("디렉터 (비우면 자동 탐색). 종료 시 오브 회수")]
    private StageDirector _director;

    [Title("풀")]
    [SerializeField, LabelText("기본 용량")] private int _defaultCapacity = 64;
    [SerializeField, LabelText("최대 크기")] private int _maxSize = 512;

    [ShowInInspector, ReadOnly, LabelText("떠 있는 오브 수")]
    private int ActiveCount => _activeOrbs.Count;

    private readonly List<ExperienceOrb> _activeOrbs = new();
    private ObjectPool<ExperienceOrb> _pool;
    private Action<ExperienceOrb> _releaseCallback;
    private Transform _root;
    private bool _isSubscribed;

    private void Awake()
    {
        _root = new GameObject("ExperienceOrbPool").transform;

        // 델리게이트를 한 번만 만들어 재사용한다(드랍마다 GC 할당 방지).
        _releaseCallback = ReleaseOrb;

        _pool = new ObjectPool<ExperienceOrb>(
            createFunc: CreateOrb,
            actionOnGet: orb => orb.gameObject.SetActive(true),
            actionOnRelease: orb => orb.gameObject.SetActive(false),
            actionOnDestroy: orb =>
            {
                if (orb != null) Destroy(orb.gameObject);
            },
            collectionCheck: true,
            defaultCapacity: _defaultCapacity,
            maxSize: _maxSize);
    }

    private void Start()
    {
        if (_collector == null) _collector = FindFirstObjectByType<PlayerExperience>();
        if (_director == null) _director = FindFirstObjectByType<StageDirector>();

        if (_collector == null)
        {
            Debug.LogWarning(
                $"[{nameof(ExperienceOrbPool)}] PlayerExperience를 찾지 못했다. 오브가 수집되지 않는다.", this);
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
        if (_root != null) Destroy(_root.gameObject);
    }

    /// <summary>지정 위치에 경험치 오브를 떨어뜨린다.</summary>
    public void Drop(Vector2 position, float value)
    {
        if (value <= 0f || _orbPrefab == null || _collector == null) return;

        ExperienceOrb orb = _pool.Get();
        _activeOrbs.Add(orb);
        orb.Spawn(position, value, _collector.transform, _collector);
    }

    [Button, LabelText("오브 전부 회수")]
    public void ReleaseAll()
    {
        if (!Application.isPlaying) return;

        // 반납이 리스트를 수정하므로 뒤에서부터 훑는다.
        for (int i = _activeOrbs.Count - 1; i >= 0; i--)
        {
            ReleaseOrb(_activeOrbs[i]);
        }
    }

    private void HandleStageStateChanged(StageState state)
    {
        // 종료 후에도 남은 오브가 끌려와 경험치가 더 들어가는 걸 막는다.
        if (state is StageState.Cleared or StageState.Failed) ReleaseAll();
    }

    private ExperienceOrb CreateOrb()
    {
        ExperienceOrb orb = Instantiate(_orbPrefab, _root);
        orb.SetReleaseCallback(_releaseCallback);
        return orb;
    }

    private void ReleaseOrb(ExperienceOrb orb)
    {
        // 수집과 일괄 회수가 겹쳐도 같은 개체를 두 번 반납하지 않게 막는다.
        if (!_activeOrbs.Remove(orb)) return;
        _pool.Release(orb);
    }
}
