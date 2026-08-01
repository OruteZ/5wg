/// <summary>
/// 보유 무기 하나. "언제 쏘는지"는 Timing이, "쏴도 되는지"는 IFireGate가 판단하므로
/// 구현체는 발사 내용에만 집중한다.
/// </summary>
public interface IWeapon
{
    WeaponDefinition Definition { get; }

    /// <summary>1부터 시작. Definition.MaxLevel까지.</summary>
    int Level { get; }

    IFireTiming Timing { get; }

    /// <summary>재장전·차지처럼 무기 내부 사정으로 발사가 막혀 있는지. 외부 조건은 게이트가 본다.</summary>
    bool IsReady { get; }

    void Equip(in WeaponContext context);
    void Unequip();
    void SetLevel(int level);

    /// <summary>비트와 무관한 내부 시간 갱신. 매 프레임 호출된다.</summary>
    void Tick(float deltaTime);

    /// <summary>실제 발사. 타이밍 판단은 이 안에서 하지 않는다.</summary>
    void Fire(in FireContext context);
}
