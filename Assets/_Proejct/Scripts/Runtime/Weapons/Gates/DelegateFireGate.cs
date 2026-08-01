using System;

/// <summary>
/// 조건 하나를 위해 클래스를 새로 만들 필요가 없을 때 쓰는 게이트.
/// 예: handler.AddGate(new DelegateFireGate(() => IsAlive));
/// </summary>
public sealed class DelegateFireGate : IFireGate
{
    private readonly Func<IWeapon, bool> _predicate;

    public DelegateFireGate(Func<bool> predicate)
    {
        if (predicate is null) throw new ArgumentNullException(nameof(predicate));
        _predicate = _ => predicate();
    }

    public DelegateFireGate(Func<IWeapon, bool> predicate)
    {
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    public bool IsAllowed(IWeapon weapon) => _predicate(weapon);
}
