using System;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 무기 공통 뼈대. 레벨·컨텍스트·타이밍 보관을 처리하고 구현체는 OnFire만 채우면 된다.
    /// MonoBehaviour가 아니므로 인벤토리 조작에 GameObject 생성·파괴가 끼어들지 않는다.
    /// </summary>
    public abstract class WeaponBase : IWeapon
    {
        protected WeaponBase(WeaponDefinition definition, IFireTiming timing)
        {
            Definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            Timing = timing ?? throw new ArgumentNullException(nameof(timing));
            Level = 1;
        }

        public WeaponDefinition Definition { get; }
        public IFireTiming Timing { get; }
        public int Level { get; private set; }

        public virtual bool IsReady => true;

        protected WeaponContext Context { get; private set; }

        /// <summary>현재 레벨의 수치. 레벨이 바뀌면 자동으로 따라간다.</summary>
        protected WeaponLevelData Stats => Definition.GetLevelData(Level);

        public void Equip(in WeaponContext context)
        {
            Context = context;
            Timing.Reset();
            OnEquipped();
        }

        public void Unequip()
        {
            OnUnequipped();
            Context = default;
        }

        public void SetLevel(int level)
        {
            int clamped = Mathf.Clamp(level, 1, Definition.MaxLevel);
            if (clamped == Level) return;

            Level = clamped;
            OnLevelChanged();
        }

        public virtual void Tick(float deltaTime)
        {
        }

        /// <summary>
        /// 이 무기의 조준 방향. 기본값은 사거리 내 최근접 적 → 마우스 → 스틱 → 마지막 이동 방향 순이다.
        /// 마우스만 보는 무기, 늘 정면으로 나가는 무기 등은 이 메서드를 재정의한다.
        /// </summary>
        protected virtual Vector2 ResolveAimDirection(in FireContext context)
        {
            if (AimHelper.TryAimAtNearestTarget(Context.Targets, context.Origin, Stats.Range, out Vector2 toTarget))
            {
                return toTarget;
            }

            if (AimHelper.TryAimAtMouse(Context.Camera, context.Origin, out Vector2 toMouse))
            {
                return toMouse;
            }

            if (AimHelper.TryAimAtStick(context.AimInput, out Vector2 toStick))
            {
                return toStick;
            }

            return context.FacingDirection;
        }

        public void Fire(in FireContext context) => OnFire(context);

        protected abstract void OnFire(in FireContext context);

        protected virtual void OnEquipped()
        {
        }

        protected virtual void OnUnequipped()
        {
        }

        protected virtual void OnLevelChanged()
        {
        }
    }
}
