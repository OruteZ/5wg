using System.Collections.Generic;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 자동공격 무기가 적을 찾는 통로. 무기가 적의 구체 타입이나 탐색 방식을 모르게 분리한다.
    /// </summary>
    public interface ITargetProvider
    {
        bool TryGetNearest(Vector2 origin, float maxRange, out Transform target);

        /// <summary>범위 내 대상을 results에 채우고 개수를 반환한다. 다중 타격 무기용.</summary>
        int GetInRange(Vector2 origin, float maxRange, List<Transform> results);
    }
}
