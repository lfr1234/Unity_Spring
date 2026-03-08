using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 行囊面板：和背包一样用动态格子显示具体物品（新鲜面包屑x3、天然小鱼x2 等）。
    /// 「行囊」按钮绑定 ShowPanel 即可打开。
    /// </summary>
    public class HaversackPanelUI : MonoBehaviour
    {
        [Header("引用")]
        public BackpackSystem backpackSystem;

        [Header("交互")]
        [Tooltip("是否允许点击格子直接吃行囊食物（仅在迁徙场景生效）。")]
        public bool clickToEatInMigration = true;

        [Header("具体物品格子（与背包相同结构）")]
        [Tooltip("用于摆放物品格子的根节点，建议挂有 GridLayoutGroup")]
        public Transform itemGridRoot;
        [Tooltip("物品格子的预制体（与背包用同一个 InventorySlotUI）")]
        public InventorySlotUI slotPrefab;

        [Header("面板根节点（用于 Show/Hide）")]
        public GameObject panelRoot;

        private void Awake()
        {
            if (backpackSystem == null) backpackSystem = BackpackSystem.Instance ?? FindObjectOfType<BackpackSystem>();
        }

        private void OnEnable()
        {
            Refresh();
        }

        /// <summary>
        /// 刷新行囊格子显示。
        /// </summary>
        public void Refresh()
        {
            if (backpackSystem == null) backpackSystem = BackpackSystem.Instance ?? FindObjectOfType<BackpackSystem>();
            if (itemGridRoot == null || slotPrefab == null || backpackSystem == null) return;

            // 只有在迁徙场景才允许“点格子吃行囊”
            bool inMigrationScene = FindObjectOfType<MigrationSystem>() != null;
            bool allowClickEat = clickToEatInMigration && inMigrationScene;

            // 清空旧格子
            for (int i = itemGridRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(itemGridRoot.GetChild(i).gameObject);
            }

            // 为每个具体物品生成一个格子
            foreach (var item in backpackSystem.items)
            {
                if (item == null || string.IsNullOrEmpty(item.itemId) || item.count <= 0) continue;

                var slot = Instantiate(slotPrefab, itemGridRoot);
                if (allowClickEat)
                    slot.SetupForHaversackEat(item.itemId, item.displayName, item.quality, item.count);
                else
                    slot.SetupDisplayOnly(item.itemId, item.displayName, item.quality, item.count);
            }
        }

        /// <summary>
        /// 打开行囊面板。
        /// </summary>
        public void ShowPanel()
        {
            GameObject root = panelRoot != null ? panelRoot : gameObject;

            // 若行囊面板是场景根（没挂在 Canvas 下），会不渲染。运行时挂到主 Canvas 下
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null && root.transform.root == root.transform)
            {
                root.transform.SetParent(canvas.transform, false);
                var rt = root.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.localScale = Vector3.one;
                    rt.localRotation = Quaternion.identity;
                }
            }
            root.transform.SetAsLastSibling(); // 置于最前，避免被其他 UI 挡住
            var bg = root.GetComponent<Image>();
            if (bg != null) bg.color = new Color(0.12f, 0.12f, 0.16f, 1f);
            root.SetActive(true);
            Refresh();
        }

        /// <summary>
        /// 关闭行囊面板。
        /// </summary>
        public void HidePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }
    }
}
