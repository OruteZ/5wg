using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지금 살아 있는 피격 대상 목록.
///
/// 조준 대상을 매 발사마다 Physics2D 오버랩으로 찾던 걸 대체한다. 자동공격이라 발사가 서브비트마다
/// 일어나고 무기도 6개까지 늘어나서, 쿼리 횟수가 (무기 수 × 서브비트)로 불어나는 구조였다.
///
/// 등록은 <see cref="Component.OnEnable"/>/<see cref="Component.OnDisable"/>에서 한다.
/// 풀이 개체를 껐다 켜는 것만으로 목록이 알아서 맞춰지므로 반납 시점을 따로 챙길 필요가 없다.
///
/// 등록하는 쪽은 반드시 <see cref="IDamageable"/>이어야 한다. 조회 측이 그렇게 가정한다.
/// </summary>
public static class DamageableRegistry
{
    private static readonly List<Component> Entries = new();

    /// <summary>등록된 대상들. 순회 중 등록·해제가 일어날 수 있으므로 인덱스로 훑고 null을 건너뛴다.</summary>
    public static IReadOnlyList<Component> All => Entries;

    public static int Count => Entries.Count;

    public static void Register(Component target)
    {
        if (target == null || Entries.Contains(target)) return;
        Entries.Add(target);
    }

    public static void Unregister(Component target)
    {
        if (target == null) return;
        Entries.Remove(target);
    }

    // 정적 목록은 플레이 모드를 나가도 살아남는다(도메인 리로드를 끈 경우).
    // 씬을 다시 시작할 때 죽은 참조가 남지 않도록 초기화한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => Entries.Clear();
}
