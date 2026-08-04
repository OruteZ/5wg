using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;

/// <summary>
/// Physics2D 오버랩으로 적을 찾는 기본 구현. 적 쪽에 등록 코드를 넣지 않아도 바로 동작한다.
/// 적 수가 많아져 오버랩 비용이 문제가 되면 이 인터페이스를 유지한 채 등록 기반 레지스트리로 교체한다.
/// </summary>
public sealed class PhysicsTargetProvider : MonoBehaviour, ITargetProvider
{
    [Title("탐색")]
    [SerializeField, LabelText("적 레이어")] private LayerMask _targetLayers = ~0;
    [SerializeField, LabelText("한 번에 볼 최대 수")] private int _maxResults = 64;

    private readonly List<Collider2D> _buffer = new();
    private ContactFilter2D _filter;

    private void Awake()
    {
        _buffer.Capacity = Mathf.Max(8, _maxResults);

        _filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _targetLayers,
            useTriggers = true,
        };
    }

    public bool TryGetNearest(Vector2 origin, float maxRange, out Transform target)
    {
        target = null;
        if (maxRange <= 0f) return false;

        int count = Physics2D.OverlapCircle(origin, maxRange, _filter, _buffer);
        float bestSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Transform candidate = _buffer[i].transform;
            if (!IsTargetable(_buffer[i])) continue;

            float sqr = ((Vector2)candidate.position - origin).sqrMagnitude;
            if (sqr >= bestSqr) continue;

            bestSqr = sqr;
            target = candidate;
        }

        return target != null;
    }

    public int GetInRange(Vector2 origin, float maxRange, List<Transform> results)
    {
        results.Clear();
        if (maxRange <= 0f) return 0;

        int count = Physics2D.OverlapCircle(origin, maxRange, _filter, _buffer);
        for (int i = 0; i < count && results.Count < _maxResults; i++)
        {
            Transform candidate = _buffer[i].transform;
            if (!IsTargetable(_buffer[i])) continue;

            results.Add(candidate);
        }

        return results.Count;
    }

    /// <summary>
    /// 레이어 마스크를 지정하지 않았을 때(기본 ~0) 자기 자신이나 아군 투사체를 조준하는 사고를 막는다.
    /// 때릴 수 있는 대상만 타겟으로 인정하므로 IDamageable이 아닌 것은 전부 걸러진다.
    /// </summary>
    private bool IsTargetable(Collider2D candidate)
    {
        if (candidate.transform.root == transform.root) return false;

        return candidate.TryGetComponent<IDamageable>(out _);
    }
}
