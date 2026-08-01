/// <summary>
/// 피격 대상. 투사체가 적의 구체 타입을 모르게 분리하기 위한 인터페이스.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}
