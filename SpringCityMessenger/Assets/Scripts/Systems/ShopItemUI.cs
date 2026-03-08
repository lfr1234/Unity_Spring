using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 商店中单个商品卡片的 UI。
    /// - 显示：名称、价格、已拥有数量；
    /// - 点击购买按钮时，调用 ShopSystem.BuyItem；
    /// - 悬浮时显示物品说明（复用背包的 Tooltip）。
    ///
    /// 所有字段都通过 Inspector 配置，避免重复写死价格/品质逻辑。
    /// </summary>
    public class ShopItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("配置")]
        [Tooltip("这个卡片对应的 ShopSystem.ShopItem 枚举值")]
        public ShopSystem.ShopItem shopItem;

        [Tooltip("对应 HomeInventory / ItemVisualConfig 里的 itemId，例如 food_bread_crumb")]
        public string itemId;

        [Header("UI 引用")]
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI priceText;
        public TextMeshProUGUI ownedText;

        [Header("系统引用")]
        public ShopSystem shopSystem;
        public HomeInventory homeInventory;

        private void Awake()
        {
            if (shopSystem == null)
            {
                shopSystem = FindObjectOfType<ShopSystem>();
            }

            if (homeInventory == null)
            {
                homeInventory = FindObjectOfType<HomeInventory>();
            }

            RefreshOwnedCount();
            RefreshIcon();
        }

        public void RefreshOwnedCount()
        {
            if (ownedText == null || homeInventory == null || string.IsNullOrEmpty(itemId))
            {
                return;
            }

            int count = homeInventory.GetItemCount(itemId);
            ownedText.text = $"已拥有：{count}";
        }

        private void RefreshIcon()
        {
            if (iconImage == null || string.IsNullOrEmpty(itemId))
            {
                return;
            }

            var entry = ItemVisualConfig.Get(itemId);
            if (entry != null && entry.icon != null)
            {
                iconImage.sprite = entry.icon;
            }
        }

        public void OnBuyButtonClicked()
        {
            if (shopSystem == null)
            {
                Debug.LogWarning("[ShopItemUI] 未找到 ShopSystem，无法购买。");
                return;
            }

            bool success = shopSystem.BuyItem(shopItem);
            if (!success)
            {
                return;
            }

            RefreshOwnedCount();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return;
            }

            var entry = ItemVisualConfig.Get(itemId);

            string nameLine = nameText != null ? nameText.text : (entry != null ? entry.itemId : itemId);
            string qualityText = string.Empty;
            FeedingSystem.FoodQuality quality = FeedingSystem.FoodQuality.None;

            if (homeInventory != null)
            {
                quality = homeInventory.GetItemQuality(itemId);
                if (quality != FeedingSystem.FoodQuality.None)
                {
                    qualityText = quality.ToString();
                }
            }

            string tooltip = nameLine;
            if (!string.IsNullOrEmpty(qualityText))
            {
                tooltip += $"\n品质：{qualityText}";
            }

            string desc = entry != null && !string.IsNullOrEmpty(entry.description)
                ? entry.description
                : ItemDescriptionHelper.GetDefaultDescription(itemId);
            if (!string.IsNullOrEmpty(desc))
            {
                tooltip += $"\n{desc}";
            }

            // 使用效果说明：
            // - 普通食物：根据品质；
            // - 药品等特殊物品：优先用配置表中的 effectDescription，若为空则使用默认说明。
            string effectText = string.Empty;
            if (quality != FeedingSystem.FoodQuality.None)
            {
                effectText = InventorySlotUIHelper.GetEffectTextByQuality(quality);
            }
            else if (entry != null && !string.IsNullOrEmpty(entry.effectDescription))
            {
                effectText = entry.effectDescription;
            }
            else
            {
                effectText = ItemDescriptionHelper.GetDefaultEffectDescription(itemId);
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
    }
}

