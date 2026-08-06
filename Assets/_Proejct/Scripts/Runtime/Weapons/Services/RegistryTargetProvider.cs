using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;

/// <summary>
/// <see cref="DamageableRegistry"/>를 훑어 조준 대상을 고른다.
///
/// 물리 쿼리를 쓰지 않는다. 자동공격이라 발사가 서브비트마다 일어나고 무기가 6개까지 늘어나므로,
/// 발사 한 번에 오버랩 한 번이던 기존 방식은 쿼리 횟수가 무기 수만큼 배로 늘어난다.
/// 대상 수가 수십 규모인 지금은 목록을 직접 도는 쪽이 싸고 예측 가능하다.
/// </summary>
public sealed class RegistryTargetProvider : MonoBehaviour, ITargetProvider
{
    [Title("탐색")]
    [SerializeField, LabelText("한 번에 볼 최대 수")] private int _maxResults = 64;

    [ShowInInspector, ReadOnly, LabelText("등록된 대상 수")]
    private int RegisteredCount => DamageableRegistry.Count;

    public bool TryGetNearest(Vector2 origin, float maxRange, out Transform target)
    {
        target = null;
        if (maxRange <= 0f) return false;

        float rangeSqr = maxRange * maxRange;
        float bestSqr = float.MaxValue;

        IReadOnlyList<Component> entries = DamageableRegistry.All;
        for (int i = 0; i < entries.Count; i++)
        {
            Component entry = entries[i];
            if (!IsTargetable(entry)) continue;

            float sqr = ((Vector2)entry.transform.position - origin).sqrMagnitude;
            if (sqr > rangeSqr || sqr >= bestSqr) continue;

            bestSqr = sqr;
            target = entry.transform;
        }

        return target != null;
    }

    public int GetInRange(Vector2 origin, float maxRange, List<Transform> results)
    {
        results.Clear();
        if (maxRange <= 0f) return 0;

        float rangeSqr = maxRange * maxRange;

        IReadOnlyList<Component> entries = DamageableRegistry.All;
        for (int i = 0; i < entries.Count && results.Count < _maxResults; i++)
        {
            Component entry = entries[i];
            if (!IsTargetable(entry)) continue;

            if (((Vector2)entry.transform.position - origin).sqrMagnitude > rangeSqr) continue;

            results.Add(entry.transform);
        }

        return results.Count;
    }

    /// <summary>자기 계층(플레이어 자신·아군)은 조준하지 않는다.</summary>
    private bool IsTargetable(Component entry)
    {
        if (entry == null) return false;

        return entry.transform.root != transform.root;
    }
}
