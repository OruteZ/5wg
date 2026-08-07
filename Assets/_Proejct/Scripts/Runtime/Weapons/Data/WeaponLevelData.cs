using System;
using Alchemy.Inspector;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 레벨 하나의 수치. 증분이 아니라 레벨별 절대값으로 둬서 기획 수치를 표에서 바로 읽고 쓰게 한다.
    ///
    /// 축 목록은 `.claude/docs/weapons.md`의 "스탯 축"과 1:1이다.
    /// 생존·전역 4축(이동속도·최대체력·경험치 획득량·자석 반경)은 여기 없다 — 무기가 아니라
    /// 플레이어에 붙으므로 PlayerMovement/PlayerController/PlayerExp/ExpOrb가 각자 들고 있다.
    /// </summary>
    [Serializable]
    public struct WeaponLevelData
    {
        [Title("위력")]
        [LabelText("데미지")] public float Damage;
        [LabelText("치명타 확률 (0~1)")] public float CritChance;
        [LabelText("치명타 배수")] public float CritMultiplier;
        [LabelText("관통 수")] public int Pierce;

        [Title("범위")]
        [LabelText("사거리 (0이면 타겟 탐색 안 함)")] public float Range;
        [LabelText("효과 반경")] public float AreaRadius;
        [LabelText("투사체 크기 배수")] public float ProjectileScale;
        [LabelText("지속시간 (장판·오라)")] public float Duration;

        [Title("빈도")]
        [LabelText("발사 수")] public int ProjectileCount;
        [LabelText("연주 밀도 배수")] public float NoteDensity;
        [LabelText("쿨다운 감소 (0~1)")] public float CooldownReduction;

        [Title("거동")]
        [LabelText("투사체 속도")] public float ProjectileSpeed;
        [LabelText("유도 강도")] public float Homing;
        [LabelText("넉백")] public float Knockback;
        [LabelText("반사 횟수")] public int Bounce;

        public static WeaponLevelData Default => new()
        {
            Damage = 10f,
            CritChance = 0f,
            CritMultiplier = 2f,
            Pierce = 0,
            Range = 8f,
            AreaRadius = 0f,
            ProjectileScale = 1f,
            Duration = 0f,
            ProjectileCount = 1,
            NoteDensity = 1f,
            CooldownReduction = 0f,
            ProjectileSpeed = 14f,
            Homing = 0f,
            Knockback = 0f,
            Bounce = 0,
        };

        /// <summary>인스펙터에서 0으로 비워둔 값이 그대로 쓰이지 않게 최소치를 보정한다.
        /// WeaponDefinition.OnValidate에서만 호출한다 — 런타임 조회 경로에는 없다.</summary>
        public WeaponLevelData Sanitized()
        {
            WeaponLevelData d = this;
            d.Damage = Mathf.Max(0f, d.Damage);
            d.CritChance = Mathf.Clamp01(d.CritChance);
            d.CritMultiplier = Mathf.Max(1f, d.CritMultiplier);
            d.Pierce = Mathf.Max(0, d.Pierce);
            d.Range = Mathf.Max(0f, d.Range);
            d.AreaRadius = Mathf.Max(0f, d.AreaRadius);
            d.ProjectileScale = Mathf.Max(0.01f, d.ProjectileScale);
            d.Duration = Mathf.Max(0f, d.Duration);
            d.ProjectileCount = Mathf.Max(1, d.ProjectileCount);
            d.NoteDensity = Mathf.Max(0f, d.NoteDensity);
            d.CooldownReduction = Mathf.Clamp01(d.CooldownReduction);
            d.ProjectileSpeed = Mathf.Max(0f, d.ProjectileSpeed);
            d.Homing = Mathf.Max(0f, d.Homing);
            d.Knockback = Mathf.Max(0f, d.Knockback);
            d.Bounce = Mathf.Max(0, d.Bounce);
            return d;
        }

        /// <summary>
        /// 합연산. 적용 순서는 "레벨 표 → 카드 강화 → 영구 강화"이고 뒤의 둘은 증분으로 저술한다.
        /// 배수 필드(치명타 배수·크기·밀도)도 증분이므로 0을 기본으로 더한다.
        /// </summary>
        public static WeaponLevelData operator +(WeaponLevelData a, WeaponLevelData b) => new()
        {
            Damage = a.Damage + b.Damage,
            CritChance = a.CritChance + b.CritChance,
            CritMultiplier = a.CritMultiplier + b.CritMultiplier,
            Pierce = a.Pierce + b.Pierce,
            Range = a.Range + b.Range,
            AreaRadius = a.AreaRadius + b.AreaRadius,
            ProjectileScale = a.ProjectileScale + b.ProjectileScale,
            Duration = a.Duration + b.Duration,
            ProjectileCount = a.ProjectileCount + b.ProjectileCount,
            NoteDensity = a.NoteDensity + b.NoteDensity,
            CooldownReduction = a.CooldownReduction + b.CooldownReduction,
            ProjectileSpeed = a.ProjectileSpeed + b.ProjectileSpeed,
            Homing = a.Homing + b.Homing,
            Knockback = a.Knockback + b.Knockback,
            Bounce = a.Bounce + b.Bounce,
        };

#if UNITY_EDITOR
        /// <summary>필드를 늘리고 operator+ 에 빠뜨리는 걸 잡는다. 리플렉션으로 전 필드를 대조한다.</summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void SelfCheck()
        {
            object a = default(WeaponLevelData), b = default(WeaponLevelData);
            var fields = typeof(WeaponLevelData).GetFields();

            for (int i = 0; i < fields.Length; i++)
            {
                fields[i].SetValue(a, Convert.ChangeType(i + 1, fields[i].FieldType));
                fields[i].SetValue(b, Convert.ChangeType(i + 2, fields[i].FieldType));
            }

            object sum = (WeaponLevelData)a + (WeaponLevelData)b;
            foreach (var f in fields)
            {
                float expected = Convert.ToSingle(f.GetValue(a)) + Convert.ToSingle(f.GetValue(b));
                float actual = Convert.ToSingle(f.GetValue(sum));
                if (!Mathf.Approximately(expected, actual))
                {
                    Debug.LogError($"[{nameof(WeaponLevelData)}] operator+ 가 '{f.Name}'을 빠뜨렸다.");
                }
            }
        }
#endif
    }
}
