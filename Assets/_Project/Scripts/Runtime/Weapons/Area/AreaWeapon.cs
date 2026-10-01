using System.Collections.Generic;
using FiveWG.Combat;
using UnityEngine;

namespace FiveWG.Weapons
{
    /// <summary>
    /// 반경 안을 때리는 무기. <see cref="DamageField"/>를 하나 뿌리는 것이 전부다.
    ///
    /// 악기 넷이 이 형태를 공유한다 — 킥(자기 중심 충격파), 크래시(넓은 원형 폭발),
    /// 베이스(주변 지속 오라), 신스(바닥에 남는 장판). 차이는 배치 위치와 지속시간뿐이라
    /// "즉발"과 "지속"을 따로 만들지 않았다.
    /// </summary>
    public sealed class AreaWeapon : WeaponBase
    {
        private readonly AreaWeaponDefinition _definition;

        // ponytail: 사거리 내 대상 목록을 담는 버퍼 하나. 발사가 서브비트마다 일어나긴 하지만
        // DensestCluster를 쓰는 무기가 한 자릿수라(원격 예고 폭발 하나) 공유 정적 리스트로 충분하다.
        private static readonly List<Transform> Candidates = new();

        public AreaWeapon(AreaWeaponDefinition definition, IFireTiming timing)
            : base(definition, timing)
        {
            _definition = definition;
        }

        protected override void OnFire(in FireContext context)
        {
            if (_definition.FieldPrefab == null || Context.projectiles == null) return;

            WeaponLevelData stats = Stats;

            if (!TryResolvePosition(stats, context, out Vector2 position)) return;

            DamageField field = Context.projectiles.Get(_definition.FieldPrefab);
            if (field == null) return;

            float coneHalfAngle = _definition.ConeHalfAngleDegrees;

            field.Begin(position, new DamageFieldData
            {
                Damage = stats.Damage,
                Radius = stats.AreaRadius,
                Duration = stats.Duration,
                TickInterval = _definition.TickInterval,
                Knockback = stats.Knockback,
                OwnerFaction = Context.Faction,

                // 추종은 자기 중심 배치에서만 뜻이 있다. 바닥에 떨어뜨린 장판이 따라오면 장판이 아니다.
                Follow = _definition.Placement == AreaPlacement.Self && _definition.FollowOwner
                    ? Context.owner
                    : null,

                // 부채꼴이 아니면 방향을 구할 필요가 없다 — 매 발사마다 조준 탐색을 공짜로 건너뛴다.
                FacingDirection = coneHalfAngle > 0f ? ResolveAimDirection(context) : Vector2.zero,
                ConeHalfAngleDegrees = coneHalfAngle,
                TelegraphDuration = _definition.TelegraphDuration,
            });
        }

        /// <summary>
        /// 대상 위치에 떨어뜨리는 무기는 겨눌 적이 없으면 발사 자체를 거른다.
        /// 빈 땅에 폭발을 띄우면 연출만 나가고 아무 일도 안 일어난다.
        /// </summary>
        private bool TryResolvePosition(in WeaponLevelData stats, in FireContext context, out Vector2 position)
        {
            if (_definition.Placement == AreaPlacement.Self)
            {
                position = context.origin;
                return true;
            }

            if (_definition.Placement == AreaPlacement.DensestCluster)
            {
                return TryResolveDensestCluster(stats, context.origin, out position);
            }

            if (Context.targets is not null &&
                Context.targets.TryGetNearest(context.origin, stats.Range, out Transform target))
            {
                position = target.position;
                return true;
            }

            position = default;
            return false;
        }

        /// <summary>
        /// 사거리 안 후보 각각을 중심으로 자기 반경(AreaRadius) 안에 몇 마리가 도는지 세어 가장 붐비는
        /// 후보를 고른다. 후보 수만큼 제곱 비교라 느리지만, 이 배치를 쓰는 무기가 한 자릿수라 괜찮다.
        /// </summary>
        private bool TryResolveDensestCluster(in WeaponLevelData stats, Vector2 origin, out Vector2 position)
        {
            position = default;
            if (Context.targets is null) return false;

            int count = Context.targets.GetInRange(origin, stats.Range, Candidates);
            if (count == 0) return false;

            float clusterRadiusSqr = stats.AreaRadius * stats.AreaRadius;
            int bestScore = -1;
            Vector2 bestPosition = default;

            for (int i = 0; i < count; i++)
            {
                Vector2 center = Candidates[i].position;
                int score = 0;

                for (int j = 0; j < count; j++)
                {
                    if (((Vector2)Candidates[j].position - center).sqrMagnitude <= clusterRadiusSqr) score++;
                }

                if (score <= bestScore) continue;

                bestScore = score;
                bestPosition = center;
            }

            position = bestPosition;
            return true;
        }
    }
}
