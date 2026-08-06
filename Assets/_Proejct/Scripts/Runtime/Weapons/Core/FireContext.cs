using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 한 틱의 발사에 필요한 값. WeaponHandler가 틱마다 한 번 만들어 모든 무기에 공유한다.
    /// 조준 방향은 여기서 정하지 않는다. 무기마다 조준 방식이 다르므로 원본 단서만 실어 보내고
    /// 최종 방향은 각 무기가 AimHelper로 뽑아 쓴다.
    /// </summary>
    public readonly struct FireContext
    {
        public readonly Vector2 Origin;

        /// <summary>게임패드 Look 스틱 원본 입력. 정규화되어 있지 않고 데드존 안일 수도 있다.</summary>
        public readonly Vector2 AimInput;

        /// <summary>플레이어가 마지막으로 향한 방향. 조준 단서가 하나도 없을 때의 최종 폴백.</summary>
        public readonly Vector2 FacingDirection;

        public readonly BeatTick Tick;

        public FireContext(Vector2 origin, Vector2 aimInput, Vector2 facingDirection, in BeatTick tick)
        {
            Origin = origin;
            AimInput = aimInput;
            FacingDirection = facingDirection;
            Tick = tick;
        }

        public override string ToString() =>
            $"Origin: {Origin}, AimInput: {AimInput}, Facing: {FacingDirection}, Tick: {Tick}";
    }
}
