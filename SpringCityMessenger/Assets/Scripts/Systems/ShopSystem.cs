using UnityEngine;

using SpringCityMessenger.Core;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 简化版商店系统：根据固定表售卖食物和药品。
    /// 这里只实现逻辑，UI 可以之后再接按钮。
    /// </summary>
    public class ShopSystem : MonoBehaviour
    {
        private CurrencyManager _currency;
        [Header("（可选）家园库存引用")]
        public HomeInventory homeInventory;

        private void Awake()
        {
            _currency = FindObjectOfType<CurrencyManager>();
        }

        public enum ShopItem
        {
            // 垃圾食品
            ExpiredBread,
            FriedSnack,
            Candy,

            // 普通食物
            FreshBreadcrumbs,
            Corn,
            Rice,

            // 优质食物
            ScienceFeed,
            SmallShrimp,
            Mealworm,

            // 高级食物
            NaturalFish,
            HighEnergyDriedFish,
            TonicFeed,

            // 药品
            ColdMedicine
        }

        /// <summary>
        /// 购买指定物品，返回是否成功。
        /// TODO：可以扩展为返回一个真正的道具对象，加入背包。
        /// </summary>
        public bool BuyItem(ShopItem item)
        {
            if (_currency == null)
            {
                Debug.LogWarning("CurrencyManager 未找到，无法购买。");
                return false;
            }

            int berryCost = 0;
            int fishCost = 0;
            bool isFood = true;
            FeedingSystem.FoodQuality foodQuality = FeedingSystem.FoodQuality.Junk;
            string itemId = null;
            string displayName = null;

            switch (item)
            {
                // 垃圾食品
                case ShopItem.ExpiredBread:
                    berryCost = 5;
                    foodQuality = FeedingSystem.FoodQuality.Junk;
                    itemId = "food_expired_bread";
                    displayName = "过期面包";
                    break;
                case ShopItem.FriedSnack:
                    berryCost = 8;
                    foodQuality = FeedingSystem.FoodQuality.Junk;
                    itemId = "food_fried_snack";
                    displayName = "油炸零食";
                    break;
                case ShopItem.Candy:
                    berryCost = 6;
                    foodQuality = FeedingSystem.FoodQuality.Junk;
                    itemId = "food_candy";
                    displayName = "糖果";
                    break;

                // 普通食物
                case ShopItem.FreshBreadcrumbs:
                    berryCost = 15;
                    foodQuality = FeedingSystem.FoodQuality.Normal;
                    itemId = "food_bread_crumb";
                    displayName = "新鲜面包屑";
                    break;
                case ShopItem.Corn:
                    berryCost = 18;
                    foodQuality = FeedingSystem.FoodQuality.Normal;
                    itemId = "food_corn";
                    displayName = "玉米粒";
                    break;
                case ShopItem.Rice:
                    berryCost = 12;
                    foodQuality = FeedingSystem.FoodQuality.Normal;
                    itemId = "food_rice";
                    displayName = "米饭";
                    break;

                // 优质食物
                case ShopItem.ScienceFeed:
                    berryCost = 30;
                    foodQuality = FeedingSystem.FoodQuality.Good;
                    itemId = "food_seagull_feed";
                    displayName = "科学鸥粮";
                    break;
                case ShopItem.SmallShrimp:
                    berryCost = 35;
                    foodQuality = FeedingSystem.FoodQuality.Good;
                    itemId = "food_shrimp";
                    displayName = "小虾米";
                    break;
                case ShopItem.Mealworm:
                    berryCost = 28;
                    foodQuality = FeedingSystem.FoodQuality.Good;
                    itemId = "food_worm";
                    displayName = "面包虫";
                    break;

                // 高级食物
                case ShopItem.NaturalFish:
                    berryCost = 50;
                    foodQuality = FeedingSystem.FoodQuality.Premium;
                    itemId = "food_fresh_fish";
                    displayName = "天然小鱼";
                    break;
                case ShopItem.HighEnergyDriedFish:
                    fishCost = 8;
                    foodQuality = FeedingSystem.FoodQuality.Premium;
                    itemId = "food_premium_fish";
                    displayName = "高能鱼干";
                    break;
                case ShopItem.TonicFeed:
                    fishCost = 10;
                    foodQuality = FeedingSystem.FoodQuality.Premium;
                    itemId = "food_special_feed";
                    displayName = "滋补鸥粮";
                    break;

                // 药品（也作为一种“物品”进入背包，但不按品质套用食物效果）
                case ShopItem.ColdMedicine:
                    berryCost = 100;
                    isFood = true; // 这里置为 true，是为了让它通过 AddItem 进入 HomeInventory
                    foodQuality = FeedingSystem.FoodQuality.None; // 特殊标记：不是普通食物
                    itemId = "med_cold";
                    displayName = "感冒药";
                    break;
            }

            // 检查货币是否足够
            if (berryCost > 0 && _currency.BerryCount < berryCost)
            {
                Debug.Log("浆果不足，无法购买。");
                return false;
            }

            if (fishCost > 0 && _currency.FishCount < fishCost)
            {
                Debug.Log("小鱼不足，无法购买。");
                return false;
            }

            // 扣费
            if (berryCost > 0) _currency.SpendBerry(berryCost);
            if (fishCost > 0) _currency.SpendFish(fishCost);

            Debug.Log($"购买物品成功：{item}，花费 浆果:{berryCost} 小鱼:{fishCost}");

            // 如果这是食物，则加入家园库存
            if (isFood)
            {
                if (string.IsNullOrEmpty(itemId))
                {
                    // 防御性判断：不应该发生，只是为了防止未来改动遗漏 itemId
                    itemId = item.ToString();
                    displayName = item.ToString();
                }

                if (homeInventory == null)
                {
                    homeInventory = FindObjectOfType<HomeInventory>();
                }

                if (homeInventory != null)
                {
                    homeInventory.AddItem(itemId, displayName, foodQuality, 1);
                    Debug.Log($"[家园库存] 获得 1 份 {displayName}（{foodQuality}）.");
                }
                else
                {
                    Debug.LogWarning("[家园库存] 未找到 HomeInventory，本次购买未记录库存，但已扣费。");
                }
            }

            return true;


        }

        /// <summary>
        /// 给「购买面包屑」按钮用，无参数。
        /// </summary>
        public void BuyBreadcrumbForButton()
        {
            BuyItem(ShopItem.FreshBreadcrumbs);
        }

        /// <summary>
        /// 给「购买科学鸥粮」按钮用，无参数。
        /// </summary>
        public void BuyScienceFeedForButton()
        {
            BuyItem(ShopItem.ScienceFeed);
        }

        /// <summary>
        /// 给「购买天然小鱼」按钮用，无参数。
        /// </summary>
        public void BuyNaturalFishForButton()
        {
            BuyItem(ShopItem.NaturalFish);
        }
    }
}

