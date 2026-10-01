using UnityEngine;

namespace FiveWG.Enemies
{
    /// <summary>
    /// 스테이지가 적 생산자에게 요구하는 것 전부 — 켜고 끄기, 일괄 회수.
    ///
    /// 인터페이스가 아니라 추상 MonoBehaviour인 것은 씬이 이 자리를 인스펙터로 배선하기
    /// 때문이다. 유니티 직렬화는 인터페이스 필드를 받지 못한다.
    /// </summary>
    public abstract class EnemySpawnerBase : MonoBehaviour
    {
        /// <summary>StageDirector가 스테이지 시작·종료에 맞춰 호출한다.</summary>
        public abstract void SetSpawning(bool enabled);

        /// <summary>살아 있는 적을 전부 회수한다. 보상은 나오지 않는다.</summary>
        public abstract void ClearAll();
    }
}
