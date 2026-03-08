using UnityEngine;

using SpringCityMessenger.Core;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 负责根据食物品质，修改海鸥属性与经验。
    /// 这里直接写死数值，对应毕设文档的食物配置表，后期可以改成配置驱动。
    /// </summary>
    public class FeedingSystem : MonoBehaviour
    {
        private SeagullStatus _seagull;

        private int _consecutiveJunkFoodCount = 0;

        [Header("（可选）家园库存引用")]
        public HomeInventory homeInventory;

        private void Awake()
        {
            _seagull = FindObjectOfType<SeagullStatus>();
            if (homeInventory == null)
            {
                homeInventory = FindObjectOfType<HomeInventory>();
            }
        }

        public enum FoodQuality
        {
            Junk,
            Normal,
            Good,
            Premium,
            None   // 行囊格为空，BackpackSystem 用
        }

        /// <summary>
        /// 按具体物品进行投喂（例如喂“新鲜面包屑”）。
        /// 会优先从 HomeInventory 中消耗该物品，再根据其品质应用对应效果。
        /// 成功返回 true，失败（没有该物品等）返回 false。
        /// 对于药品（quality == None），会调用内部的用药逻辑，而不是食物数值效果。
        /// </summary>
        public bool FeedByItemId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;

            if (_seagull == null)
            {
                Debug.LogWarning("[喂食] SeagullStatus 未找到，无法投喂。");
                return false;
            }

            if (homeInventory == null)
            {
                homeInventory = FindObjectOfType<HomeInventory>();
            }
            if (homeInventory == null)
            {
                Debug.LogWarning("[喂食] 未找到 HomeInventory，无法按物品投喂。");
                return false;
            }

            var quality = homeInventory.GetItemQuality(itemId);
            if (quality == FoodQuality.None)
            {
                // 质量为 None，优先认为是“特殊物品”（如药品），走独立逻辑。
                return UseMedicine(itemId);
            }

            bool consumed = homeInventory.ConsumeItem(itemId, 1);
            if (!consumed)
            {
                Debug.Log($"[喂食] 背包中 {itemId} 数量不足，无法投喂。");
                return false;
            }

            int remaining = homeInventory.GetItemCount(itemId);
            Debug.Log($"[喂食] 已消耗 1 份 {itemId}（{quality}），该物品剩余 {remaining} 份。");

            // 根据该物品所属品质，应用同一套数值效果
            switch (quality)
            {
                case FoodQuality.Junk:
                    ApplyJunkFood();
                    break;
                case FoodQuality.Normal:
                    ApplyNormalFood();
                    break;
                case FoodQuality.Good:
                    ApplyGoodFood();
                    break;
                case FoodQuality.Premium:
                    ApplyPremiumFood();
                    break;
            }

            // 喂食后刷新商店里“已拥有：x”的显示
            var shopItems = Object.FindObjectsOfType<ShopItemUI>(true);
            foreach (var ui in shopItems)
            {
                ui.RefreshOwnedCount();
            }

            return true;
        }

        /// <summary>
        /// 对外统一的投喂接口，UI 按钮可以直接调用这个方法。
        /// </summary>
        public void Feed(FoodQuality quality)
        {
            if (quality == FoodQuality.None) return;  // 新增这一行
            if (_seagull == null)
            {
                Debug.LogWarning("SeagullStatus 未找到，无法投喂。");
                return;
            }

            // 如果配置了家园库存，则先尝试从库存中扣除一份对应食物
            if (homeInventory != null)
            {
                bool consumed = homeInventory.TryConsume(quality);
                if (!consumed)
                {
                    Debug.Log($"[家园库存] 没有可用的 {quality} 食物，无法投喂。");
                    return;
                }
            }

            switch (quality)
            {
                case FoodQuality.Junk:
                    ApplyJunkFood();
                    break;
                case FoodQuality.Normal:
                    ApplyNormalFood();
                    break;
                case FoodQuality.Good:
                    ApplyGoodFood();
                    break;
                case FoodQuality.Premium:
                    ApplyPremiumFood();
                    break;
            }


        }

        #region 各类食物具体效果（数值来自文档）

        private void ApplyJunkFood()
        {
            // 这里使用“过期面包”作为代表：体力+10, 健康-5, 饱食+15, 经验+2
            _seagull.ChangeStamina(+10);
            _seagull.ChangeHealth(-5);
            _seagull.ChangeHunger(+15);
            _seagull.AddExp(+2);

            _consecutiveJunkFoodCount++;
            if (_consecutiveJunkFoodCount >= 3)
            {
                _seagull.isSick = true;
                GameMessageUI.Show("吃太多垃圾食品，海鸥有点生病了……");
            }
        }

        /// <summary>
        /// 按物品 ID 使用药品等特殊道具。
        /// 目前仅支持：感冒药（med_cold）——用于清除生病状态并回复少量健康。
        /// </summary>
        private bool UseMedicine(string itemId)
        {
            if (homeInventory == null)
            {
                homeInventory = FindObjectOfType<HomeInventory>();
            }
            if (homeInventory == null)
            {
                Debug.LogWarning("[用药] 未找到 HomeInventory，无法使用药品。");
                return false;
            }

            if (_seagull == null)
            {
                _seagull = FindObjectOfType<SeagullStatus>();
                if (_seagull == null)
                {
                    Debug.LogWarning("[用药] 未找到 SeagullStatus，无法使用药品。");
                    return false;
                }
            }

            // 目前只有感冒药：med_cold
            if (itemId != "med_cold")
            {
                Debug.Log($"[用药] 未知的药品 itemId: {itemId}，暂不处理。");
                return false;
            }

            // 如果没生病，就不消耗药品，只给出提示
            if (!_seagull.isSick)
            {
                GameMessageUI.Show("现在状态很好，不需要吃药。");
                return false;
            }

            bool consumed = homeInventory.ConsumeItem(itemId, 1);
            if (!consumed)
            {
                Debug.Log($"[用药] 背包中 {itemId} 数量不足，无法用药。");
                return false;
            }

            _seagull.isSick = false;
            _seagull.ChangeHealth(+30);
            Debug.Log("[用药] 使用 1 份感冒药，清除了生病状态，健康+30。");
            GameMessageUI.Show("海鸥吃了感冒药，感觉好多了（健康+30）。");

            // 用药也会改变库存，同步刷新商店显示
            var shopItems = Object.FindObjectsOfType<ShopItemUI>(true);
            foreach (var ui in shopItems)
            {
                ui.RefreshOwnedCount();
            }

            return true;
        }

        private void ApplyNormalFood()
        {
            // 以“新鲜面包屑”为代表：体力+20, 健康+0, 饱食+25, 经验+5
            _seagull.ChangeStamina(+20);
            _seagull.ChangeHealth(+0);
            _seagull.ChangeHunger(+25);
            _seagull.AddExp(+5);

            _consecutiveJunkFoodCount = 0;
        }

        private void ApplyGoodFood()
        {
            // 以“科学鸥粮”为代表：体力+30, 健康+5, 饱食+35, 经验+10
            _seagull.ChangeStamina(+30);
            _seagull.ChangeHealth(+5);
            _seagull.ChangeHunger(+35);
            _seagull.AddExp(+10);

            _consecutiveJunkFoodCount = 0;
        }

        private void ApplyPremiumFood()
        {
            // 以“天然小鱼”为代表：体力+50, 健康+10, 饱食+45, 经验+15
            _seagull.ChangeStamina(+50);
            _seagull.ChangeHealth(+10);
            _seagull.ChangeHunger(+45);
            _seagull.AddExp(+15);

            _consecutiveJunkFoodCount = 0;
        }

        private void Update()
        {
            // 之前这里有 1-4 调试按键投喂，现在全部移除，避免和 UI 点击叠加导致“看起来像一次点掉多份”的情况。
        }


        // 给按钮用的无参数方法：喂普通食物
        public void FeedNormalForButton()
        {
            Feed(FoodQuality.Normal);
        }

        public void FeedJunkForButton()
        {
            Feed(FoodQuality.Junk);
        }

        public void FeedGoodForButton()
        {
            Feed(FoodQuality.Good);
        }

        public void FeedPremiumForButton()
        {
            Feed(FoodQuality.Premium);
        }

        #endregion
    }
}

