using System;
using Alchemy.Inspector;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 레벨 하나의 수치. 증분이 아니라 레벨별 절대값으로 둬서 기획 수치를 표에서 바로 읽고 쓰게 한다.
    /// 기획서 확정 후 비트 패턴 관련 필드가 여기에 추가된다.
    /// </summary>
    [Serializable]
    public struct WeaponLevelData
    {
        [LabelText("데미지")] public float Damage;
        [LabelText("발사 수")] public int ProjectileCount;
        [LabelText("사거리 (0이면 타겟 탐색 안 함)")] public float Range;

        public static WeaponLevelData Default => new()
        {
            Damage = 10f,
            ProjectileCount = 1,
            Range = 8f,
        };

        /// <summary>인스펙터에서 0으로 비워둔 값이 그대로 쓰이지 않게 최소치를 보정한다.</summary>
        public WeaponLevelData Sanitized() => new()
        {
            Damage = Mathf.Max(0f, Damage),
            ProjectileCount = Mathf.Max(1, ProjectileCount),
            Range = Mathf.Max(0f, Range),
        };
    }
}
