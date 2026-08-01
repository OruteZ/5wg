/// <summary>
/// 데미지를 받을 수 있는 대상. 투사체는 이 인터페이스만 알고 구체 타입은 모른다(디커플링).
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}
