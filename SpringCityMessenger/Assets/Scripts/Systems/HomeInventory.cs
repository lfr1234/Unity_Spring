using System.Collections.Generic;
using UnityEngine;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 家园库存：存放买来的食物，投喂时从这里扣除。
    /// 与行囊不同：行囊只用于迁徙，家园库存用于日常养成。
    /// 当前同时维护两套信息：
    /// - 按品质统计的 4 个大类（junk/normal/good/premium），兼容现有喂食与行囊逻辑；
    /// - 按具体物品（itemId）统计的列表，后续可用于正式背包格子显示与按物品喂食。
    /// </summary>
    public class HomeInventory : MonoBehaviour
    {
        [System.Serializable]
        public class InventoryItem
        {
            public string itemId;
            public string displayName;
            public FeedingSystem.FoodQuality quality;
            public int count;
        }

        [Header("当前持有的食物数量（按品质汇总）")]
        public int junkFoodCount;
        public int normalFoodCount;
        public int goodFoodCount;
        public int premiumFoodCount;

        [Header("当前持有的具体物品列表")]
        public List<InventoryItem> items = new List<InventoryItem>();

        /// <summary>
        /// 增加某种品质的食物（旧接口，兼容早期逻辑）。
        /// 后续建议优先使用 AddItem，通过 itemId 记录具体物品。
        /// </summary>
        public void AddFood(FeedingSystem.FoodQuality quality, int amount)
        {
            if (amount <= 0) return;

            switch (quality)
            {
                case FeedingSystem.FoodQuality.Junk:
                    junkFoodCount += amount;
                    break;
                case FeedingSystem.FoodQuality.Normal:
                    normalFoodCount += amount;
                    break;
                case FeedingSystem.FoodQuality.Good:
                    goodFoodCount += amount;
                    break;
                case FeedingSystem.FoodQuality.Premium:
                    premiumFoodCount += amount;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 按具体物品增加库存，会自动更新对应品质的汇总数量。
        /// 商店购买成功时推荐调用此接口。
        /// </summary>
        public void AddItem(string itemId, string displayName, FeedingSystem.FoodQuality quality, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return;

            var item = items.Find(i => i.itemId == itemId);
            if (item == null)
            {
                item = new InventoryItem
                {
                    itemId = itemId,
                    displayName = displayName,
                    quality = quality,
                    count = 0
                };
                items.Add(item);
            }

            item.count += amount;

            // 同步更新按品质汇总的数量，兼容现有喂食与行囊逻辑
            AddFood(quality, amount);
        }

        /// <summary>
        /// 按具体物品消耗若干份，成功返回 true，并同步更新品质汇总。
        /// </summary>
        public bool ConsumeItem(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;

            var item = items.Find(i => i.itemId == itemId);
            if (item == null || item.count < amount) return false;

            item.count -= amount;
            if (item.count <= 0)
            {
                items.Remove(item);
            }

            // 同步按品质消耗，但这里只扣“amount”这么多，避免再次在 items 列表中循环扣除导致重复减一。
            // 注意：药品等 quality==None 的物品不会计入四类食物汇总中，这里也无需处理。
            switch (item.quality)
            {
                case FeedingSystem.FoodQuality.Junk:
                    junkFoodCount = Mathf.Max(0, junkFoodCount - amount);
                    break;
                case FeedingSystem.FoodQuality.Normal:
                    normalFoodCount = Mathf.Max(0, normalFoodCount - amount);
                    break;
                case FeedingSystem.FoodQuality.Good:
                    goodFoodCount = Mathf.Max(0, goodFoodCount - amount);
                    break;
                case FeedingSystem.FoodQuality.Premium:
                    premiumFoodCount = Mathf.Max(0, premiumFoodCount - amount);
                    break;
                default:
                    break;
            }

            return true;
        }

        public int GetItemCount(string itemId)
        {
            var item = items.Find(i => i.itemId == itemId);
            return item != null ? item.count : 0;
        }

        public FeedingSystem.FoodQuality GetItemQuality(string itemId)
        {
            var item = items.Find(i => i.itemId == itemId);
            return item != null ? item.quality : FeedingSystem.FoodQuality.None;
        }

        /// <summary>
        /// 尝试消耗 1 份指定品质的食物，用于投喂。
        /// 成功返回 true，没有则返回 false。
        /// </summary>
        public bool TryConsume(FeedingSystem.FoodQuality quality)
        {
            switch (quality)
            {
                case FeedingSystem.FoodQuality.Junk:
                    if (junkFoodCount > 0)
                    {
                        junkFoodCount--;
                        LogRemaining(quality);
                        return true;
                    }
                    break;
                case FeedingSystem.FoodQuality.Normal:
                    if (normalFoodCount > 0)
                    {
                        normalFoodCount--;
                        LogRemaining(quality);
                        return true;
                    }
                    break;
                case FeedingSystem.FoodQuality.Good:
                    if (goodFoodCount > 0)
                    {
                        goodFoodCount--;
                        LogRemaining(quality);
                        return true;
                    }
                    break;
                case FeedingSystem.FoodQuality.Premium:
                    if (premiumFoodCount > 0)
                    {
                        premiumFoodCount--;
                        LogRemaining(quality);
                        return true;
                    }
                    break;
                default:
                    break;
            }
            return false;
        }

        private void LogRemaining(FeedingSystem.FoodQuality justAte)
        {
            Debug.Log($"[家园库存] 刚消耗 1 份 {justAte}，剩余：垃圾{junkFoodCount} 普通{normalFoodCount} 优质{goodFoodCount} 高级{premiumFoodCount}");
        }

        /// <summary>
        /// 消耗指定品质的若干份（用于装行囊等），返回实际消耗的数量。
        /// 会同步更新 items 列表和按品质汇总数量，保证格子显示正确。
        /// </summary>
        public int Consume(FeedingSystem.FoodQuality quality, int amount)
        {
            if (amount <= 0) return 0;
            int remaining = amount;

            // 从 items 列表中扣除，保证 ItemGrid 格子会正确减少
            for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var item = items[i];
                if (item.quality != quality) continue;

                int take = Mathf.Min(item.count, remaining);
                item.count -= take;
                remaining -= take;

                if (item.count <= 0)
                    items.RemoveAt(i);
            }

            int n = amount - remaining;

            // 同步更新按品质汇总的数量
            switch (quality)
            {
                case FeedingSystem.FoodQuality.Junk:
                    junkFoodCount = Mathf.Max(0, junkFoodCount - n);
                    break;
                case FeedingSystem.FoodQuality.Normal:
                    normalFoodCount = Mathf.Max(0, normalFoodCount - n);
                    break;
                case FeedingSystem.FoodQuality.Good:
                    goodFoodCount = Mathf.Max(0, goodFoodCount - n);
                    break;
                case FeedingSystem.FoodQuality.Premium:
                    premiumFoodCount = Mathf.Max(0, premiumFoodCount - n);
                    break;
            }

            return n;
        }

        /// <summary>
        /// 获取某种品质的当前数量。
        /// </summary>
        public int GetCount(FeedingSystem.FoodQuality quality)
        {
            switch (quality)
            {
                case FeedingSystem.FoodQuality.Junk: return junkFoodCount;
                case FeedingSystem.FoodQuality.Normal: return normalFoodCount;
                case FeedingSystem.FoodQuality.Good: return goodFoodCount;
                case FeedingSystem.FoodQuality.Premium: return premiumFoodCount;
                default: return 0;
            }
        }
    }
}
