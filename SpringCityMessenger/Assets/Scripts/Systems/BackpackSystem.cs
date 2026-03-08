using System.Collections.Generic;
using UnityEngine;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 行囊系统：按具体物品存储（如新鲜面包屑x3、天然小鱼x2），与背包一样是动态格子。
    /// 迁徙时按品质优先级吃（ Premium > Good > Normal > Junk）。
    /// </summary>
    public class BackpackSystem : MonoBehaviour
    {
        public static BackpackSystem Instance { get; private set; }

        [System.Serializable]
        public class HaversackItem
        {
            public string itemId;
            public string displayName;
            public FeedingSystem.FoodQuality quality;
            public int count;
        }

        [Header("行囊中的具体物品（与背包相同结构）")]
        public List<HaversackItem> items = new List<HaversackItem>();

        /// <summary>
        /// 行囊里食物总数（用于出发校验）。
        /// </summary>
        public int TotalFoodCount
        {
            get
            {
                int sum = 0;
                foreach (var item in items)
                {
                    // 只把真正的食物算入总数；quality==None（如药品）不算作“有食物”
                    if (item != null && item.count > 0 && item.quality != FeedingSystem.FoodQuality.None)
                        sum += item.count;
                }
                return sum;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ClearAll()
        {
            items.Clear();
        }

        /// <summary>
        /// 添加具体物品到行囊。
        /// </summary>
        public void AddItem(string itemId, string displayName, FeedingSystem.FoodQuality quality, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return;

            var item = items.Find(i => i != null && i.itemId == itemId);
            if (item == null)
            {
                item = new HaversackItem
                {
                    itemId = itemId,
                    displayName = displayName,
                    quality = quality,
                    count = 0
                };
                items.Add(item);
            }

            item.count += amount;
        }

        /// <summary>
        /// 按具体物品消耗若干份（玩家点击格子吃时调用）。
        /// 返回是否成功，以及体力/饱食回复值（按该物品品质）。
        /// </summary>
        public bool ConsumeItem(string itemId, int amount, out int staminaRestore, out int hungerRestore)
        {
            staminaRestore = 0;
            hungerRestore = 0;
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;

            var item = items.Find(i => i != null && i.itemId == itemId);
            if (item == null || item.count < amount) return false;

            item.count -= amount;
            if (item.count <= 0) items.Remove(item);

            GetSingleFoodRestore(item.quality, out staminaRestore, out hungerRestore);
            Debug.Log($"[行囊] 吃掉 {amount} 份 {item.displayName}（{item.quality}）");
            return true;
        }

        /// <summary>
        /// 迁徙时吃掉 1 份食物：优先吃 Premium > Good > Normal > Junk。
        /// 返回是否吃到，以及体力/饱食回复值。
        /// </summary>
        public bool ConsumeFoodForMigration(out int staminaRestore, out int hungerRestore)
        {
            staminaRestore = 0;
            hungerRestore = 0;

            // 按品质优先级找一份可吃的
            var target = FindOneToConsume(FeedingSystem.FoodQuality.Premium)
                ?? FindOneToConsume(FeedingSystem.FoodQuality.Good)
                ?? FindOneToConsume(FeedingSystem.FoodQuality.Normal)
                ?? FindOneToConsume(FeedingSystem.FoodQuality.Junk);

            if (target == null) return false;

            target.count--;
            if (target.count <= 0)
                items.Remove(target);

            GetSingleFoodRestore(target.quality, out staminaRestore, out hungerRestore);
            Debug.Log($"[行囊] 吃掉1份 {target.displayName}（{target.quality}）");
            return true;
        }

        private HaversackItem FindOneToConsume(FeedingSystem.FoodQuality quality)
        {
            foreach (var item in items)
            {
                if (item != null && item.quality == quality && item.count > 0)
                    return item;
            }
            return null;
        }

        private void GetSingleFoodRestore(FeedingSystem.FoodQuality quality, out int stamina, out int hunger)
        {
            switch (quality)
            {
                case FeedingSystem.FoodQuality.Junk:
                    stamina = 5; hunger = 15; break;
                case FeedingSystem.FoodQuality.Normal:
                    stamina = 15; hunger = 25; break;
                case FeedingSystem.FoodQuality.Good:
                    stamina = 25; hunger = 35; break;
                case FeedingSystem.FoodQuality.Premium:
                    stamina = 40; hunger = 45; break;
                default:
                    stamina = 0; hunger = 0; break;
            }
        }
    }
}
