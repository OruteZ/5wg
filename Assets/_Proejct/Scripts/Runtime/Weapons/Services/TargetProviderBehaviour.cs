using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인스펙터에 연결할 수 있는 ITargetProvider의 공통 베이스.
/// Unity가 인터페이스 필드를 직렬화하지 못해서, 구현을 갈아끼우려면 이런 추상 컴포넌트가 필요하다.
/// </summary>
public abstract class TargetProviderBehaviour : MonoBehaviour, ITargetProvider
{
    public abstract bool TryGetNearest(Vector2 origin, float maxRange, out Transform target);

    public abstract int GetInRange(Vector2 origin, float maxRange, List<Transform> results);
}
