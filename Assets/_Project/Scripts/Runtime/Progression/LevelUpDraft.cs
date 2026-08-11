using System.Collections.Generic;
using FiveWG.Weapons;
using UnityEngine;

namespace FiveWG.Progression
{
    /// <summary>
    /// 카드 한 장. 무엇을 주는지와 그것을 어떻게 적용하는지를 같이 안다.
    ///
    /// 소모성 보상(힐)은 여기 없다. 고를 것이 없는 회차는 카드가 아예 뜨지 않으므로
    /// "카드 한 장"이 아니다 — 후보가 비었을 때 무엇을 줄지는 화면 쪽이 정한다.
    /// </summary>
    public readonly struct LevelUpOption
    {
        public readonly WeaponDefinition Weapon;

        /// <summary>오르기 전 레벨. 0이면 아직 없는 무기다.</summary>
        public readonly int CurrentLevel;

        private LevelUpOption(WeaponDefinition weapon, int currentLevel)
        {
            Weapon = weapon;
            CurrentLevel = currentLevel;
        }

        public static LevelUpOption NewWeapon(WeaponDefinition weapon) => new(weapon, 0);

        public static LevelUpOption Upgrade(WeaponDefinition weapon, int currentLevel) =>
            new(weapon, currentLevel);

        public string Title => Weapon.DisplayName;

        public string Detail => CurrentLevel <= 0 ? "새 악기" : $"Lv {CurrentLevel} → {CurrentLevel + 1}";

        /// <summary>보유 중이면 <see cref="WeaponInventory.TryAcquire"/>가 알아서 레벨업으로 흡수한다.</summary>
        public void Apply(WeaponInventory inventory) => inventory?.TryAcquire(Weapon, out _);
    }

    /// <summary>
    /// 레벨업 한 번에 띄울 카드를 뽑는다. 규칙은 `.claude/docs/progression.md`의 "카드 선택" 절이다.
    ///
    /// MonoBehaviour가 아니라 순수 static이다 — 상태가 없고, 규칙만 있으면 UI 없이도 검증할 수 있어서다.
    /// </summary>
    public static class LevelUpDraft
    {
        public const int CardCount = 3;

        /// <summary>
        /// 후보를 모아 최대 3장을 중복 없이 뽑는다.
        ///
        /// 가중치는 신규/강화 사이에만 쓴다. **신규 무기는 빈 슬롯 수만큼 가중되고 강화는 1이다.**
        /// 초반엔 슬롯이 비어 있어 신규가 잘 뜨고, 슬롯이 찰수록 자연히 강화로 기운다.
        /// 무기끼리는 균등하다 — 10종뿐이라 희귀도를 넣으면 원하는 무기를 못 만나는 판이 생긴다.
        ///
        /// 후보가 하나도 없으면(슬롯이 차고 전부 만렙) **빈 목록**이다. 그 회차에 무엇을 줄지는
        /// 여기서 정하지 않는다 — 소모성 보상은 카드가 아니라 화면 쪽 규칙이다.
        /// </summary>
        public static void Build(
            WeaponInventory inventory, IReadOnlyList<WeaponDefinition> pool, List<LevelUpOption> results)
        {
            results.Clear();
            if (inventory is null || pool is null) return;

            int emptySlots = WeaponInventory.Capacity - inventory.Count;

            var candidates = new List<LevelUpOption>();
            var weights = new List<int>();

            for (int i = 0; i < pool.Count; i++)
            {
                WeaponDefinition definition = pool[i];
                if (definition == null) continue;

                if (inventory.TryGetSlot(definition, out int slot))
                {
                    IWeapon owned = inventory.GetAt(slot);
                    if (owned.Level >= definition.MaxLevel) continue;   // 만렙은 후보가 아니다

                    candidates.Add(LevelUpOption.Upgrade(definition, owned.Level));
                    weights.Add(1);
                    continue;
                }

                if (emptySlots <= 0) continue;   // 슬롯이 꽉 차면 신규는 아예 안 나온다

                candidates.Add(LevelUpOption.NewWeapon(definition));
                weights.Add(emptySlots);
            }

            // 3장을 못 채우면 남은 만큼만 띄운다. 하나도 없으면 그대로 빈 목록이다.
            int draw = Mathf.Min(CardCount, candidates.Count);
            for (int n = 0; n < draw; n++)
            {
                int picked = PickWeightedIndex(weights);
                results.Add(candidates[picked]);

                // 같은 라운드에 같은 항목이 두 번 뜨지 않도록 뽑은 것을 후보에서 뺀다.
                candidates.RemoveAt(picked);
                weights.RemoveAt(picked);
            }
        }

        /// <summary>
        /// 가중치에 비례해 인덱스 하나를 고른다. 가중치가 전부 0이면 균등하게 고른다.
        /// 반환값은 항상 유효한 인덱스다 — 부동소수 없이 정수로만 누적해 마지막 칸이 새지 않게 했다.
        /// </summary>
        private static int PickWeightedIndex(List<int> weights)
        {
            int total = 0;
            for (int i = 0; i < weights.Count; i++) total += Mathf.Max(0, weights[i]);

            if (total <= 0) return Random.Range(0, weights.Count);

            int roll = Random.Range(0, total);
            for (int i = 0; i < weights.Count; i++)
            {
                roll -= Mathf.Max(0, weights[i]);
                if (roll < 0) return i;
            }

            return weights.Count - 1;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 규칙이 조용히 깨지면 "가끔 이상한 카드가 뜬다"로만 보여서 재현이 어렵다.
        /// 특히 가중 추첨은 범위를 한 칸 흘리면 마지막 후보가 영영 안 뜨는 식으로 조용히 틀어진다.
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void SelfCheck()
        {
            var results = new List<LevelUpOption>();
            Build(null, null, results);
            if (results.Count != 0)
            {
                Debug.LogError($"[{nameof(LevelUpDraft)}] 인벤토리가 없는데 카드를 만들었다.");
            }

            // 가중치 0인 후보가 뽑히면 슬롯이 꽉 찼는데 신규 무기가 뜨는 버그가 된다.
            var weights = new List<int> { 3, 0, 1 };
            var hits = new int[weights.Count];
            for (int i = 0; i < 4000; i++) hits[PickWeightedIndex(weights)]++;

            if (hits[1] != 0)
            {
                Debug.LogError($"[{nameof(LevelUpDraft)}] 가중치 0인 후보가 {hits[1]}번 뽑혔다.");
            }

            // 마지막 칸이 새면 0이 나온다. 비율까지 재면 시드에 따라 흔들리므로 "뽑히긴 하는가"만 본다.
            if (hits[0] == 0 || hits[2] == 0)
            {
                Debug.LogError($"[{nameof(LevelUpDraft)}] 가중 추첨이 후보를 통째로 빠뜨렸다: {hits[0]}/{hits[1]}/{hits[2]}");
            }
        }
#endif
    }
}
