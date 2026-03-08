using System.Collections.Generic;
using UnityEngine;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 负责「把家园库存装进行囊」：从 HomeInventory 按具体物品转移到 BackpackSystem。
    ///  backpack 里是什么，装进行囊后行囊里就是什么。
    /// </summary>
    public class PackBackpackController : MonoBehaviour
    {
        [Header("引用（请拖上）")]
        public HomeInventory homeInventory;
        public BackpackSystem backpackSystem;

        [Header("每种品质最多装几份")]
        [Tooltip("装行囊时，每种品质最多从家园拿几份（比如普通3份、高级2份）")]
        public int maxPerQuality = 3;

        [Header("装行囊策略")]
        [Tooltip("装行囊前是否先清空旧行囊。不勾选则每次装行囊会累加，不会覆盖之前的")]
        public bool clearBackpackBeforePack = false;

        [Header("药品/特殊物品")]
        [Tooltip("是否把感冒药（med_cold）也装进行囊，用于迁徙途中恢复健康")]
        public bool packColdMedicine = true;

        private void Awake()
        {
            if (homeInventory == null) homeInventory = FindObjectOfType<HomeInventory>();
            if (backpackSystem == null) backpackSystem = FindObjectOfType<BackpackSystem>();
        }

        /// <summary>
        /// 从家园库存按具体物品装到行囊。背包里有什么就装什么。
        /// </summary>
        public void PackFromHome()
        {
            if (homeInventory == null) homeInventory = FindObjectOfType<HomeInventory>();
            if (backpackSystem == null) backpackSystem = BackpackSystem.Instance ?? FindObjectOfType<BackpackSystem>();

            if (homeInventory == null)
            {
                Debug.LogWarning("[装行囊] 未找到家园库存。");
                GameMessageUI.Show("未找到家园库存。");
                return;
            }
            if (backpackSystem == null)
            {
                Debug.LogWarning("[装行囊] 未找到行囊。");
                GameMessageUI.Show("未找到行囊。");
                return;
            }

            if (clearBackpackBeforePack)
                backpackSystem.ClearAll();

            // 统计每种品质已装多少
            int packedJunk = 0, packedNormal = 0, packedGood = 0, packedPremium = 0;

            // 按品质优先级装：先装高级，再优质、普通、垃圾
            PackItemsOfQuality(FeedingSystem.FoodQuality.Premium, ref packedPremium);
            PackItemsOfQuality(FeedingSystem.FoodQuality.Good, ref packedGood);
            PackItemsOfQuality(FeedingSystem.FoodQuality.Normal, ref packedNormal);
            PackItemsOfQuality(FeedingSystem.FoodQuality.Junk, ref packedJunk);

            // 可选：把感冒药也带上路（不计入食物上限）
            int packedMedicine = 0;
            if (packColdMedicine)
            {
                packedMedicine = PackColdMedicine();
            }

            int total = packedJunk + packedNormal + packedGood + packedPremium + packedMedicine;
            if (total > 0)
            {
                Debug.Log($"[装行囊] 已装入：垃圾{packedJunk} 普通{packedNormal} 优质{packedGood} 高级{packedPremium} 感冒药{packedMedicine}，共{total}份（含药品）。");
                GameMessageUI.Show("行囊已准备好！");

                // 如果行囊面板已打开，立即刷新显示
                var haversackUI = FindObjectOfType<HaversackPanelUI>();
                if (haversackUI != null) haversackUI.Refresh();
            }
            else
            {
                Debug.Log("[装行囊] 家园库存没有食物可装。");
                GameMessageUI.Show("家园库存没有食物可装。");
            }
        }

        private void PackItemsOfQuality(FeedingSystem.FoodQuality quality, ref int packedCount)
        {
            int remaining = maxPerQuality - packedCount;
            if (remaining <= 0) return;

            // 先收集要转移的物品（避免遍历时修改列表导致越界）
            var toTransfer = new List<(string id, string name, FeedingSystem.FoodQuality q, int count)>();
            int totalToTake = 0;

            foreach (var item in homeInventory.items)
            {
                if (item == null || item.quality != quality || item.count <= 0) continue;
                int take = Mathf.Min(item.count, remaining - totalToTake);
                if (take <= 0) break;
                toTransfer.Add((item.itemId, item.displayName, item.quality, take));
                totalToTake += take;
                if (totalToTake >= remaining) break;
            }

            foreach (var t in toTransfer)
            {
                if (homeInventory.ConsumeItem(t.id, t.count))
                {
                    backpackSystem.AddItem(t.id, t.name, t.q, t.count);
                    packedCount += t.count;
                }
            }
        }

        /// <summary>
        /// 把家园里的感冒药（med_cold）全部装进行囊，不占用每种品质的数量上限。
        /// </summary>
        private int PackColdMedicine()
        {
            if (homeInventory == null || backpackSystem == null) return 0;

            const string medId = "med_cold";
            int count = homeInventory.GetItemCount(medId);
            if (count <= 0) return 0;

            bool consumed = homeInventory.ConsumeItem(medId, count);
            if (!consumed) return 0;

            backpackSystem.AddItem(medId, "感冒药", FeedingSystem.FoodQuality.None, count);
            return count;
        }
    }
}
