using System;
using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;

namespace FiveWG.Pickup
{
    /// <summary>
    /// 적 하나가 죽을 때 무엇이 나오는지 정하는 표. 확률은 전부 여기 있고 코드에는 없다.
    ///
    /// 묶음을 나누는 건 티켓과 아이템의 뽑는 방식이 다르기 때문이다.
    /// 티켓은 일반 8% / 고액 0.5%가 한 판정을 나눠 쓰고 둘 다 빗나가면 아무것도 안 나온다.
    /// 아이템 3종은 서로 상관이 없어서 물병과 자석이 같이 나올 수 있다.
    /// </summary>
    [CreateAssetMenu(fileName = "PickupDropTable", menuName = "5WG/Pickup/Drop Table")]
    public sealed class PickupDropTable : ScriptableObject
    {
        [SerializeField, LabelText("묶음")] private DropGroup[] _groups = Array.Empty<DropGroup>();

        /// <summary>
        /// 적 하나가 죽었을 때 나올 픽업을 뽑아 <paramref name="results"/>에 담는다.
        /// 목록을 여기서 만들지 않는 건 이 경로가 후반에 초당 수십 번 불리기 때문이다.
        /// </summary>
        public void Roll(List<PickupDefinition> results)
        {
            results.Clear();
            if (_groups == null) return;

            foreach (DropGroup group in _groups)
            {
                group?.Roll(results);
            }
        }

        [Serializable]
        public sealed class DropEntry
        {
            [SerializeField, LabelText("픽업")] private PickupDefinition _definition;

            [SerializeField, LabelText("확률 (%)"), Range(0f, 100f)] private float _chancePercent;

            public PickupDefinition Definition => _definition;
            public float ChancePercent => _chancePercent;
        }

        [Serializable]
        public sealed class DropGroup
        {
            [SerializeField, LabelText("이름")]
            [Tooltip("인스펙터에서 구분하려고 붙이는 이름. 게임에는 안 나온다.")]
            private string _label;

            [SerializeField, LabelText("하나만 뽑기")]
            [Tooltip("켜면 이 묶음 전체에서 한 번만 뽑는다. 아래 확률들이 그 한 번을 나눠 갖고, " +
                     "다 빗나가면 아무것도 안 나온다. 티켓이 이 경우다 (고액 0.5% / 일반 8% / 나머지 91.5%는 꽝).\n\n" +
                     "끄면 항목마다 따로 뽑는다. 서로 상관이 없어서 여러 개가 같이 나올 수 있다. " +
                     "아이템이 이 경우다 (물병과 자석이 한 마리에서 같이 나올 수 있음).")]
            private bool _onlyOne = true;

            // 티켓은 12분(보스 회차)부터 안 나와야 한다. 지금은 스테이지 경과 시간을 알 방법이
            // 없어서 이 값을 아무도 안 본다. 회차가 생기면 그때 드랍 쪽에 연결한다.
            [SerializeField, LabelText("드랍 종료 (분)"), Min(0f)]
            [Tooltip("이 시각을 넘기면 더 나오지 않는다. 0이면 판이 끝날 때까지 계속 나온다.\n\n" +
                     "아직 동작하지 않는다. 스테이지가 몇 분째인지 알려주는 것이 없어서, " +
                     "회차 시스템이 생길 때까지 값만 받아 둔다.")]
            private float _dropUntilMinutes;

            [SerializeField, LabelText("목록")] private DropEntry[] _entries = Array.Empty<DropEntry>();

            public float DropUntilMinutes => _dropUntilMinutes;

            public void Roll(List<PickupDefinition> results)
            {
                if (_entries == null || _entries.Length == 0) return;

                if (_onlyOne)
                {
                    RollOne(results);
                    return;
                }

                foreach (DropEntry entry in _entries)
                {
                    if (entry?.Definition == null) continue;
                    if (UnityEngine.Random.value * 100f < entry.ChancePercent) results.Add(entry.Definition);
                }
            }

            // 확률 합이 100을 넘으면 뒤쪽 항목은 영영 안 나온다. 합을 맞추는 건 기획이 할 일이라
            // 여기서 자동으로 줄이지 않는다. 넣은 값이 조용히 바뀌는 쪽이 더 나쁘다.
            private void RollOne(List<PickupDefinition> results)
            {
                float roll = UnityEngine.Random.value * 100f;
                float sum = 0f;

                foreach (DropEntry entry in _entries)
                {
                    if (entry?.Definition == null) continue;

                    sum += entry.ChancePercent;
                    if (roll >= sum) continue;

                    results.Add(entry.Definition);
                    return;
                }
            }
        }
    }
}
