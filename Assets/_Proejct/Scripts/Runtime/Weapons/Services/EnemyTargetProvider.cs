using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemyRegistry를 직접 훑는 타겟 탐색. 물리 질의도, 컴포넌트 조회도 하지 않는다.
/// 적이 Enemy2D인 한 이게 기본이다. 그 밖의 대상까지 잡아야 하면 PhysicsTargetProvider를 쓴다.
/// </summary>
public sealed class EnemyTargetProvider : TargetProviderBehaviour
{
    public override bool TryGetNearest(Vector2 origin, float maxRange, out Transform target)
    {
        target = null;
        if (maxRange <= 0f) return false;

        IReadOnlyList<Enemy2D> enemies = EnemyRegistry.All;
        float bestSqr = maxRange * maxRange;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy2D enemy = enemies[i];
            float sqr = ((Vector2)enemy.Transform.position - origin).sqrMagnitude;
            if (sqr >= bestSqr) continue;

            bestSqr = sqr;
            target = enemy.Transform;
        }

        return target != null;
    }

    public override int GetInRange(Vector2 origin, float maxRange, List<Transform> results)
    {
        results.Clear();
        if (maxRange <= 0f) return 0;

        IReadOnlyList<Enemy2D> enemies = EnemyRegistry.All;
        float maxSqr = maxRange * maxRange;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy2D enemy = enemies[i];
            if (((Vector2)enemy.Transform.position - origin).sqrMagnitude > maxSqr) continue;

            results.Add(enemy.Transform);
        }

        return results.Count;
    }
}
