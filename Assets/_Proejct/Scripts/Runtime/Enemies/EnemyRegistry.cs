using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 살아 있는 적 목록. 적이 스스로 등록·해제한다.
///
/// 물리 오버랩으로 적을 찾던 걸 대체한다. 오버랩은 발사마다 씬의 콜라이더를 훑고 후보마다
/// 인터페이스 TryGetComponent를 돌려야 했는데, 여기서는 이미 만들어진 목록을 순회만 하면 된다.
///
/// 해제는 마지막 원소를 빈자리로 옮기는 방식이라 O(1)이다. 순서는 보장하지 않는다.
/// </summary>
public static class EnemyRegistry
{
    private static readonly List<Enemy2D> ActiveEnemies = new(64);

    public static IReadOnlyList<Enemy2D> All => ActiveEnemies;

    public static int Count => ActiveEnemies.Count;

    public static void Register(Enemy2D enemy)
    {
        if (enemy == null || enemy.RegistryIndex >= 0) return;

        enemy.RegistryIndex = ActiveEnemies.Count;
        ActiveEnemies.Add(enemy);
    }

    public static void Unregister(Enemy2D enemy)
    {
        if (enemy == null) return;

        int index = enemy.RegistryIndex;
        if (index < 0 || index >= ActiveEnemies.Count) return;

        int last = ActiveEnemies.Count - 1;

        // 마지막 원소를 빈자리로 당겨온다. 자기 자신이 마지막이면 옮길 게 없다.
        if (index != last)
        {
            ActiveEnemies[index] = ActiveEnemies[last];
            ActiveEnemies[index].RegistryIndex = index;
        }

        ActiveEnemies.RemoveAt(last);
        enemy.RegistryIndex = -1;
    }

    /// <summary>도메인 리로드를 끈 상태에서 이전 플레이의 잔재가 남지 않게 비운다.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnEnterPlayMode() => ActiveEnemies.Clear();
}
