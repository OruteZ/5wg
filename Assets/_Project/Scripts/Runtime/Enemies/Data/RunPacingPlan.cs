using System;
using Alchemy.Inspector;
using UnityEngine;

namespace FiveWG.Enemies
{
    /// <summary>
    /// 한 판의 형태 — 난이도 곡선, 밀도 상한, 스폰 위치, 엘리트 일정.
    /// 스테이지가 달라도 같은 값들이다. 어떤 적이 어떤 비율로 나오는가는
    /// <see cref="StageEnemyPlan"/>에 있다.
    ///
    /// **곡선은 초로 잰다.** HP·스폰율·DPS가 전부 "초당 고정, BPM 비연동"으로 정해져 있어
    /// 마디로 옮기면 BPM이 오를 때 난이도만 빨라진다. 곡 구조상의 위치인 회차·웨이브·엘리트·보스는
    /// 반대로 마디로 재며, 그쪽은 <see cref="StageEnemyPlan"/>에 있다.
    /// </summary>
    [CreateAssetMenu(fileName = "RunPacingPlan", menuName = "5WG/Enemies/Run Pacing Plan")]
    public sealed class RunPacingPlan : ScriptableObject
    {
        [Title("시간 곡선")]
        [SerializeField, LabelText("기본 적 HP (HP 배수 1.0 기준)")]
        private TimedValue[] _baseHealth = Array.Empty<TimedValue>();

        [SerializeField, LabelText("스폰율 (마리/초)")]
        private TimedValue[] _spawnRate = Array.Empty<TimedValue>();

        [SerializeField, LabelText("목표 화면 내 적 수")]
        [Tooltip("평상시 스폰의 천장. 스폰율 표가 이 목표에서 역산된 값이다.")]
        private TimedValue[] _targetAlive = Array.Empty<TimedValue>();

        [Title("박자")]
        [SerializeField, LabelText("한 마디 박 수"), Min(1)]
        [Tooltip("기본 4/4. 회차·웨이브·엘리트가 이 값으로 마디를 센다.")]
        private int _beatsPerBar = 4;

        [Title("기본 이동 속도")]
        [SerializeField, LabelText("기본 적 이동 속도 (units/sec) — 미정, 임시값"), Min(0f)]
        [Tooltip("플레이어 이동속도가 확정되면 그 비율로 정해진다.")]
        private float _baseMoveSpeed = 2f;

        [Title("스폰 위치")]
        [SerializeField, LabelText("스폰 반경"), Min(0.1f)]
        [Tooltip("카메라 직교 크기 6 기준 중심-모서리 거리가 12.3이라 14는 어느 방향이든 화면 밖이다.")]
        private float _spawnRadius = 14f;

        [SerializeField, LabelText("디스폰 거리 배수"), Min(1f)]
        private float _despawnRadiusMultiplier = 2f;

        [SerializeField, LabelText("스폰 반경 흔들기"), Min(0f)]
        [Tooltip("기획에 없는 연출용 값. 0이면 원형 웨이브가 한 줄로 보인다.")]
        private float _spawnRadiusJitter = 1f;

        [SerializeField, LabelText("한방향 배치의 벌어짐 (도)"), Range(0f, 360f)]
        [Tooltip("기획에 없는 값. '한 방향에 모아서'를 각도 폭으로 옮긴 것이다.")]
        private float _oneDirectionArcDegrees = 60f;

        [Title("밀도")]
        [SerializeField, LabelText("평상시 상한"), Min(1)] private int _softCap = 250;

        [SerializeField, LabelText("하드 상한"), Min(1)]
        [Tooltip("웨이브는 평상시 상한을 넘겨도 되지만 이 값은 지킨다.")]
        private int _hardCap = 300;

        public int BeatsPerBar => _beatsPerBar;
        public float BaseMoveSpeed => _baseMoveSpeed;
        public float SpawnRadius => _spawnRadius;
        public float SpawnRadiusJitter => _spawnRadiusJitter;
        public float OneDirectionArcDegrees => _oneDirectionArcDegrees;
        public float DespawnRadius => _spawnRadius * _despawnRadiusMultiplier;
        public int SoftCap => _softCap;
        public int HardCap => _hardCap;

        public float SampleBaseHealth(float timeSec) => Sample(_baseHealth, timeSec);
        public float SampleSpawnRate(float timeSec) => Sample(_spawnRate, timeSec);
        public float SampleTargetAlive(float timeSec) => Sample(_targetAlive, timeSec);

        /// <summary>두 점 사이를 직선으로 잇는다. 표 바깥은 양 끝 값으로 고정한다.</summary>
        public static float Sample(TimedValue[] points, float timeSec)
        {
            if (points == null || points.Length == 0) return 0f;
            if (timeSec <= points[0].TimeSec) return points[0].Value;

            for (int i = 1; i < points.Length; i++)
            {
                if (timeSec > points[i].TimeSec) continue;

                TimedValue prev = points[i - 1];
                TimedValue next = points[i];

                float span = next.TimeSec - prev.TimeSec;
                if (span <= 0f) return next.Value;

                return Mathf.Lerp(prev.Value, next.Value, (timeSec - prev.TimeSec) / span);
            }

            return points[^1].Value;
        }

        private void OnValidate()
        {
            WarnIfUnsorted(_baseHealth, nameof(_baseHealth));
            WarnIfUnsorted(_spawnRate, nameof(_spawnRate));
            WarnIfUnsorted(_targetAlive, nameof(_targetAlive));

            if (_hardCap < _softCap)
            {
                Debug.LogWarning(
                    $"[{nameof(RunPacingPlan)}] 하드 상한({_hardCap})이 평상시 상한({_softCap})보다 작다. " +
                    "웨이브가 평상시보다 적게 나온다.", this);
            }
        }

        // 정렬돼 있지 않으면 보간이 조용히 엉뚱한 구간을 집는다. 자동 정렬하지 않는 건
        // 넣은 순서가 말없이 바뀌는 쪽이 더 나쁘기 때문이다.
        private void WarnIfUnsorted(TimedValue[] points, string fieldName)
        {
            if (points == null) return;

            for (int i = 1; i < points.Length; i++)
            {
                if (points[i].TimeSec >= points[i - 1].TimeSec) continue;

                Debug.LogWarning(
                    $"[{nameof(RunPacingPlan)}] {fieldName}이(가) 시각 순이 아니다 " +
                    $"({i - 1}번째 {points[i - 1].TimeSec}초 → {i}번째 {points[i].TimeSec}초). 보간이 어긋난다.", this);
                return;
            }
        }

        /// <summary>시각 하나에 값 하나. 표를 기획서와 같은 모양으로 두려고 곡선 대신 점을 쓴다.</summary>
        [Serializable]
        public struct TimedValue
        {
            [SerializeField, LabelText("시각 (sec)"), Min(0f)] private float _timeSec;
            [SerializeField, LabelText("값")] private float _value;

            public TimedValue(float timeSec, float value)
            {
                _timeSec = timeSec;
                _value = value;
            }

            public float TimeSec => _timeSec;
            public float Value => _value;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 보간의 인덱스 계산을 확인한다. 한 칸 흘리면 "8분인데 4분치 HP"가 되고,
        /// 플레이로는 밸런스가 이상한 것과 구분되지 않는다.
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void SelfCheck()
        {
            TimedValue[] points =
            {
                new(0f, 10f),
                new(120f, 14f),
                new(240f, 20f),
            };

            (float time, float expected)[] cases =
            {
                (-10f, 10f),
                (0f, 10f),
                (60f, 12f),
                (120f, 14f),
                (180f, 17f),
                (240f, 20f),
                (600f, 20f),
            };

            foreach ((float time, float expected) in cases)
            {
                float actual = Sample(points, time);
                if (Mathf.Approximately(actual, expected)) continue;

                Debug.LogError(
                    $"[{nameof(RunPacingPlan)}] {time}초의 보간이 {expected}여야 하는데 {actual}이다.");
            }

            if (!Mathf.Approximately(Sample(Array.Empty<TimedValue>(), 5f), 0f))
            {
                Debug.LogError($"[{nameof(RunPacingPlan)}] 빈 표는 0을 돌려줘야 한다.");
            }
        }
#endif
    }
}
