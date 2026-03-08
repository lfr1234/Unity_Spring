using UnityEngine;
using TMPro;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 背包面板：只显示「家园库存」数量，并提供「装进行囊」按钮。
    /// 行囊（BackpackSystem）只在迁徙/准备迁徙界面使用，这里不展示行囊详情。
    /// </summary>
    public class BackpackPanelUI : MonoBehaviour
    {
        [Header("数据引用")]
        public HomeInventory homeInventory;
        public PackBackpackController packController;

        [Header("家园库存数量文字")]
        public TextMeshProUGUI homeJunkText;
        public TextMeshProUGUI homeNormalText;
        public TextMeshProUGUI homeGoodText;
        public TextMeshProUGUI homePremiumText;

        [Header("具体物品格子（动态生成）")]
        [Tooltip("用于摆放物品格子的根节点，建议挂有 GridLayoutGroup")]
        public Transform itemGridRoot;
        [Tooltip("物品格子的预制体")]
        public InventorySlotUI slotPrefab;

        private void Awake()
        {
            if (homeInventory == null) homeInventory = FindObjectOfType<HomeInventory>();
            if (packController == null) packController = FindObjectOfType<PackBackpackController>();
        }

        private void OnEnable()
        {
            Refresh();
        }

        /// <summary>
        /// 刷新面板上显示的数量。
        /// </summary>
        public void Refresh()
        {
            if (homeInventory != null)
            {
                if (homeJunkText != null) homeJunkText.text = "垃圾：" + homeInventory.junkFoodCount;
                if (homeNormalText != null) homeNormalText.text = "普通：" + homeInventory.normalFoodCount;
                if (homeGoodText != null) homeGoodText.text = "优质：" + homeInventory.goodFoodCount;
                if (homePremiumText != null) homePremiumText.text = "高级：" + homeInventory.premiumFoodCount;

                RefreshItemGrid();
            }

        }

        private void RefreshItemGrid()
        {
            if (itemGridRoot == null || slotPrefab == null || homeInventory == null) return;

            // 先清空旧格子
            for (int i = itemGridRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(itemGridRoot.GetChild(i).gameObject);
            }

            // 为每个具体物品生成一个格子
            foreach (var item in homeInventory.items)
            {
                var slot = Instantiate(slotPrefab, itemGridRoot);
                slot.Setup(item);
            }
        }

        /// <summary>
        /// 「装进行囊」按钮调用：执行装行囊并刷新显示。
        /// </summary>
        public void OnPackClick()
        {
            if (packController != null)
                packController.PackFromHome();
            Refresh();
        }
    }
}
