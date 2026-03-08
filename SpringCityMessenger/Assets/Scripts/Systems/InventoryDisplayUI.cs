using UnityEngine;
using TMPro;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 在 UI 上显示家园库存（四种食物的数量）。
    /// </summary>
    public class InventoryDisplayUI : MonoBehaviour
    {
        [Header("引用")]
        public HomeInventory homeInventory;
        public TextMeshProUGUI inventoryText;

        private void Awake()
        {
            if (homeInventory == null)
            {
                homeInventory = FindObjectOfType<HomeInventory>();
            }
        }

        private void Update()
        {
            if (homeInventory == null || inventoryText == null) return;

            int junk = homeInventory.junkFoodCount;
            int normal = homeInventory.normalFoodCount;
            int good = homeInventory.goodFoodCount;
            int premium = homeInventory.premiumFoodCount;

            inventoryText.text = $"库存：垃圾{junk} 普通{normal} 优质{good} 高级{premium}";
        }
    }
}

