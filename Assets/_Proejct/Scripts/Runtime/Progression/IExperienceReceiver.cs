/// <summary>
/// 경험치를 받는 대상. 오브가 수집 대상의 구체 타입을 모르게 분리하기 위한 인터페이스.
/// IDamageable과 같은 역할을 경험치 축에서 한다.
/// </summary>
public interface IExperienceReceiver
{
    void AddExperience(float amount);
}
