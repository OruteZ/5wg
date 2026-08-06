using UnityEngine.InputSystem;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 조준 방향을 구하는 순수 질의 모음. 어느 단서를 어떤 순서로 쓸지는 각 무기가 정하고,
    /// 여기서는 "그 단서로 방향이 나오는가"만 답한다.
    /// 모든 Try 계열은 유효한 방향을 만들지 못하면 false를 돌려주므로 폴백을 이어 붙이기 쉽다.
    /// </summary>
    public static class AimHelper
    {
        // 이 아래 길이는 방향으로 쓰기엔 너무 짧아 정규화 시 값이 튄다.
        private const float MinSqrMagnitude = 0.0001f;

        /// <summary>사거리 안의 가장 가까운 대상을 향하는 방향.</summary>
        public static bool TryAimAtNearestTarget(
            ITargetProvider targets, Vector2 origin, float range, out Vector2 direction)
        {
            direction = default;

            if (targets is null || range <= 0f) return false;
            if (!targets.TryGetNearest(origin, range, out Transform target)) return false;

            return TryGetDirection((Vector2)target.position - origin, out direction);
        }

        /// <summary>마우스 커서를 향하는 방향. 마우스가 없으면 false.</summary>
        public static bool TryAimAtMouse(Camera camera, Vector2 origin, out Vector2 direction)
        {
            direction = default;

            return TryGetMouseWorldPoint(camera, out Vector2 world)
                   && TryGetDirection(world - origin, out direction);
        }

        /// <summary>마우스 커서의 월드 좌표. 2D 직교/원근 카메라 모두 카메라 평면 기준으로 환산한다.</summary>
        public static bool TryGetMouseWorldPoint(Camera camera, out Vector2 world)
        {
            world = default;
            if (camera == null || Mouse.current is null) return false;

            Vector2 screen = Mouse.current.position.ReadValue();
            world = camera.ScreenToWorldPoint(
                new Vector3(screen.x, screen.y, -camera.transform.position.z));

            return true;
        }

        /// <summary>게임패드 스틱 입력을 방향으로. 데드존 안이면 false.</summary>
        public static bool TryAimAtStick(Vector2 aimInput, out Vector2 direction) =>
            TryGetDirection(aimInput, out direction);

        /// <summary>길이가 유의미할 때만 정규화해 돌려준다.</summary>
        public static bool TryGetDirection(Vector2 raw, out Vector2 direction)
        {
            if (raw.sqrMagnitude <= MinSqrMagnitude)
            {
                direction = default;
                return false;
            }

            direction = raw.normalized;
            return true;
        }

        public static Vector2 Rotate(Vector2 direction, float degrees)
        {
            if (Mathf.Approximately(degrees, 0f)) return direction;

            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            return new Vector2(
                direction.x * cos - direction.y * sin,
                direction.x * sin + direction.y * cos);
        }

        /// <summary>
        /// 부채꼴로 균등 분할한 index번째 방향. count가 1이면 정면 그대로.
        /// 다발 발사 무기가 각도 계산을 각자 다시 짜지 않도록 여기 둔다.
        /// </summary>
        public static Vector2 Spread(Vector2 direction, int index, int count, float totalDegrees)
        {
            if (count <= 1 || Mathf.Approximately(totalDegrees, 0f)) return direction;

            float step = totalDegrees / (count - 1);
            return Rotate(direction, -totalDegrees * 0.5f + step * index);
        }
    }
}
