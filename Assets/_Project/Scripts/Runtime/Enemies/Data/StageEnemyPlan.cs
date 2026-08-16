using System;
using Alchemy.Inspector;
using UnityEngine;

namespace FiveWG.Enemies
{
    /// <summary>
    /// 한 스테이지의 적 편성 — 회차별 가중치, 지정 웨이브, 보스.
    ///
    /// 전부 그 스테이지의 적을 이름으로 가리키므로 스테이지 밖으로 나갈 수 없다.
    /// 스테이지가 달라도 같은 값(곡선·밀도·엘리트 일정)은 <see cref="RunPacingPlan"/>에 있다.
    /// </summary>
    [CreateAssetMenu(fileName = "StageEnemyPlan", menuName = "5WG/Enemies/Stage Enemy Plan")]
    public sealed class StageEnemyPlan : ScriptableObject
    {
        [Title("판의 페이스")]
        [SerializeField, Required("페이싱이 없으면 곡선도 밀도 상한도 없어 스폰이 일어나지 않는다.")]
        [LabelText("페이싱 (스테이지 간 공유)")]
        private RunPacingPlan _pacing;

        [Title("회차")]
        [SerializeField, LabelText("회차별 가중치")] private Round[] _rounds = Array.Empty<Round>();

        [Title("지정 웨이브")]
        [SerializeField, LabelText("웨이브")] private Wave[] _waves = Array.Empty<Wave>();

        [Title("엘리트")]
        [SerializeField, LabelText("엘리트")] private EliteEntry _elite = new();

        [Title("보스")]
        [SerializeField, LabelText("보스")] private BossEntry _boss = new();

        public RunPacingPlan Pacing => _pacing;
        public Wave[] Waves => _waves;
        public EliteEntry Elite => _elite;
        public BossEntry Boss => _boss;

        /// <summary>시작 마디가 지난 회차 중 가장 늦은 것.</summary>
        public Round GetRound(float bar)
        {
            Round current = null;

            foreach (Round round in _rounds)
            {
                if (round == null || round.StartBar > bar) continue;
                if (current == null || round.StartBar >= current.StartBar) current = round;
            }

            return current;
        }

        /// <summary>
        /// 한 회차의 평상시 추첨 표. 가중치를 시간에 따라 보간하지 않는 건 기획이 회차 단위로
        /// 끊어 정했기 때문이다.
        /// </summary>
        [Serializable]
        public sealed class Round
        {
            [SerializeField, LabelText("이름")]
            [Tooltip("인스펙터에서 구분하려고 붙이는 이름. 게임에는 안 나온다.")]
            private string _label;

            [SerializeField, LabelText("시작 마디"), Min(0f)] private float _startBar;
            [SerializeField, LabelText("가중치")] private Weight[] _weights = Array.Empty<Weight>();

            public string Label => _label;
            public float StartBar => _startBar;
            public Weight[] Weights => _weights;
        }

        [Serializable]
        public struct Weight
        {
            [SerializeField, LabelText("적")] private EnemyDefinition _definition;
            [SerializeField, LabelText("가중치"), Min(0f)] private float _weight;

            public EnemyDefinition Definition => _definition;
            public float Value => _weight;
        }

        /// <summary>
        /// 엘리트 등장 일정. 수치는 일반 적과 똑같이 <see cref="EnemyDefinition"/> 에셋에 있고
        /// 여기에는 "언제 몇 마리"만 둔다.
        /// </summary>
        [Serializable]
        public sealed class EliteEntry
        {
            [SerializeField, LabelText("사용")] private bool _enabled = true;

            [SerializeField, LabelText("엘리트 정의")]
            [Tooltip("비워두면 엘리트가 나오지 않는다. 상자 보상도 같이 사라지므로 디렉터가 경고한다.")]
            private EnemyDefinition _definition;

            [SerializeField, LabelText("첫 등장 (마디)"), Min(0f)] private float _startBar = 90f;
            [SerializeField, LabelText("등장 간격 (마디)"), Min(1f)] private float _intervalBars = 120f;
            [SerializeField, LabelText("총 마리 수"), Min(0)] private int _count = 3;

            public bool Enabled => _enabled;
            public EnemyDefinition Definition => _definition;
            public int Count => _count;

            public float GetSpawnBar(int index) => _startBar + _intervalBars * index;
        }

        /// <summary>
        /// 4회차의 보스. 패턴도 페이즈도 없이 직진하는 일반 적과 같은 동작이라 전용 클래스가 없다.
        /// 다른 것은 <see cref="EnemyDefinition"/>의 수치와 넉백·디스폰 면역뿐이다.
        /// </summary>
        [Serializable]
        public sealed class BossEntry
        {
            [SerializeField, LabelText("사용")] private bool _enabled = true;

            [SerializeField, LabelText("보스 정의")]
            [Tooltip("비워두면 보스가 나오지 않는다. 승리 조건이 사라지므로 디렉터가 경고한다.")]
            private EnemyDefinition _definition;

            [SerializeField, LabelText("등장 마디"), Min(0f)] private float _spawnBar = 360f;

            public bool Enabled => _enabled;
            public EnemyDefinition Definition => _definition;
            public float SpawnBar => _spawnBar;
        }

        /// <summary>평상시 스폰 위에 한 번에 얹는 덩어리. 마디는 판 시작 기준이다.</summary>
        [Serializable]
        public sealed class Wave
        {
            [SerializeField, LabelText("이름")] private string _label;
            [SerializeField, LabelText("마디"), Min(0f)] private float _bar;
            [SerializeField, LabelText("배치")] private EnemyFormation _formation = EnemyFormation.Random;
            [SerializeField, LabelText("구성")] private Entry[] _entries = Array.Empty<Entry>();

            public string Label => _label;
            public float Bar => _bar;
            public EnemyFormation Formation => _formation;
            public Entry[] Entries => _entries;

            public int TotalCount
            {
                get
                {
                    int total = 0;
                    foreach (Entry entry in _entries) total += entry.Count;
                    return total;
                }
            }

            [Serializable]
            public struct Entry
            {
                [SerializeField, LabelText("적")] private EnemyDefinition _definition;
                [SerializeField, LabelText("수"), Min(0)] private int _count;

                public EnemyDefinition Definition => _definition;
                public int Count => _count;
            }
        }
    }
}
