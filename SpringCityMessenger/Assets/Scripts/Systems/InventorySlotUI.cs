using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SpringCityMessenger.Core;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 背包中单个物品格子的 UI。
    /// 现在格子本身只展示缩略图和数量，名称与描述通过悬浮提示显示。
    /// </summary>
    public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI 引用")]
        [Tooltip("物品图标")]
        public Image iconImage;

        [Tooltip("可选：在格子上直接显示名称（如果不需要可以留空或隐藏对应 Text）")]
        public TextMeshProUGUI nameText;

        [Tooltip("数量文本，例如 x3")]
        public TextMeshProUGUI countText;

        private string _itemId;
        private string _displayName;
        private FeedingSystem.FoodQuality _quality;
        private FeedingSystem _feedingSystem;
        private bool _displayOnly;
        private bool _isHaversackEat;  // 迁徙场景：点击格子吃行囊食物

        public string ItemId => _itemId;

        /// <summary>
        /// 行囊用：只显示物品，不响应点击。
        /// </summary>
        public void SetupDisplayOnly(string itemId, string displayName, FeedingSystem.FoodQuality quality, int count)
        {
            _displayOnly = true;
            _isHaversackEat = false;
            SetupCommon(itemId, displayName, quality, count);
        }

        /// <summary>
        /// 迁徙场景行囊用：点击格子吃这份食物，恢复体力/饱食。
        /// </summary>
        public void SetupForHaversackEat(string itemId, string displayName, FeedingSystem.FoodQuality quality, int count)
        {
            _displayOnly = false;
            _isHaversackEat = true;
            SetupCommon(itemId, displayName, quality, count);
        }

        /// <summary>
        /// 用一个 InventoryItem 来初始化格子显示。
        /// </summary>
        public void Setup(HomeInventory.InventoryItem item)
        {
            if (item == null) return;
            _displayOnly = false;
            _isHaversackEat = false;
            SetupCommon(item.itemId, item.displayName, item.quality, item.count);
        }

        private void SetupCommon(string itemId, string displayName, FeedingSystem.FoodQuality quality, int count)
        {
            _itemId = itemId;
            _displayName = displayName;
            _quality = quality;

            if (nameText != null)
                nameText.text = displayName;
            if (countText != null)
                countText.text = "x" + count;

            var entry = ItemVisualConfig.Get(itemId);
            if (entry != null && iconImage != null && entry.icon != null)
                iconImage.sprite = entry.icon;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var entry = ItemVisualConfig.Get(_itemId);

            string qualityText = _quality != FeedingSystem.FoodQuality.None
                ? _quality.ToString()
                : string.Empty;

            // 使用效果说明：
            // - 普通食物：基于 FeedingSystem 里的数值设置；
            // - 药品等特殊物品：优先使用配置表中的 effectDescription，若为空再用代码里的默认说明。
            string effectText = string.Empty;
            if (_quality != FeedingSystem.FoodQuality.None)
            {
                effectText = GetEffectTextByQuality(_quality);
            }
            else if (entry != null && !string.IsNullOrEmpty(entry.effectDescription))
            {
                effectText = entry.effectDescription;
            }
            else
            {
                effectText = ItemDescriptionHelper.GetDefaultEffectDescription(_itemId);
            }

            // 物品自定义描述（可选，从 ItemVisualConfig 里配置，若为空则使用默认描述）
            string desc = entry != null && !string.IsNullOrEmpty(entry.description)
                ? entry.description
                : ItemDescriptionHelper.GetDefaultDescription(_itemId);

            string tooltip = _displayName;
            if (!string.IsNullOrEmpty(qualityText))
            {
                tooltip += $"\n品质：{qualityText}";
            }
            if (!string.IsNullOrEmpty(desc))
            {
                tooltip += $"\n{desc}";
            }
            if (!string.IsNullOrEmpty(effectText))
            {
                tooltip += $"\n使用效果：{effectText}";
            }

            ItemTooltipUI.Show(tooltip);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ItemTooltipUI.Hide();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_displayOnly) return;
            if (string.IsNullOrEmpty(_itemId)) return;

            // 迁徙场景：点击行囊格子吃这份食物
            if (_isHaversackEat)
            {
                EatFromHaversack();
                return;
            }

            // 家园场景：点击背包格子投喂
            if (_feedingSystem == null) _feedingSystem = Object.FindObjectOfType<FeedingSystem>();
            if (_feedingSystem == null)
            {
                Debug.LogWarning("[InventorySlotUI] 未找到 FeedingSystem，无法投喂。");
                return;
            }

            bool success = _feedingSystem.FeedByItemId(_itemId);
            if (!success) return;

            HomeInventory inventory = _feedingSystem.homeInventory ?? Object.FindObjectOfType<HomeInventory>();
            if (inventory == null) return;

            int remainingCount = inventory.GetItemCount(_itemId);
            if (remainingCount > 0)
            {
                if (countText != null) countText.text = "x" + remainingCount;
            }
            else
            {
                ItemTooltipUI.Hide();
                Destroy(gameObject);
            }
        }

        private void EatFromHaversack()
        {
            var backpack = BackpackSystem.Instance ?? Object.FindObjectOfType<BackpackSystem>();
            if (backpack == null)
            {
                GameMessageUI.Show("行囊不存在。");
                return;
            }

            var seagull = Object.FindObjectOfType<SeagullStatus>();
            if (seagull == null)
            {
                GameMessageUI.Show("未找到海鸥状态，无法使用行囊物品。");
                return;
            }

            // 特殊：感冒药 med_cold —— 迁徙途中也可以从行囊里吃药，恢复健康（如果本来生病则一并治好）
            if (_itemId == "med_cold")
            {
                int dummySta, dummyHun;
                bool used = backpack.ConsumeItem(_itemId, 1, out dummySta, out dummyHun);
                if (!used)
                {
                    GameMessageUI.Show("感冒药已经用完了。");
                    return;
                }

                bool wasSick = seagull.isSick;
                seagull.isSick = false;          // 不管原来有没有生病，都顺便清一下
                seagull.ChangeHealth(+30);
                GameMessageUI.Show(wasSick
                    ? "吃了 1 份感冒药，清除了生病状态，健康+30。"
                    : "吃了 1 份感冒药，健康+30。");
            }
            else
            {
                int restoreSta, restoreHun;
                bool ate = backpack.ConsumeItem(_itemId, 1, out restoreSta, out restoreHun);
                if (!ate)
                {
                    GameMessageUI.Show("该食物已吃完。");
                    return;
                }

                seagull.ChangeStamina(restoreSta);
                seagull.ChangeHunger(restoreHun);
                GameMessageUI.Show($"吃了 1 份{_displayName}，体力+{restoreSta}，饱食+{restoreHun}");
            }

            // 刷新行囊面板
            var haversackUI = GetComponentInParent<HaversackPanelUI>();
            if (haversackUI != null) haversackUI.Refresh();
            else
            {
                var any = Object.FindObjectOfType<HaversackPanelUI>();
                if (any != null) any.Refresh();
            }
        }

        private static string GetEffectTextByQuality(FeedingSystem.FoodQuality quality)
        {
            return InventorySlotUIHelper.GetEffectTextByQuality(quality);
        }
    }
}

