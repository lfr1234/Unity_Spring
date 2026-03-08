using UnityEngine;
using UnityEngine.UI;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 商店分类切换的简单控制脚本。
    /// - 通过若干按钮切换显示多组商品列表。
    /// - 只会让当前分类的 ItemsGrid 处于激活状态，其余全部隐藏。
    /// </summary>
    public class ShopCategoriesUI : MonoBehaviour
    {
        [Header("各分类对应的商品列表根节点")]
        public GameObject junkItemsRoot;
        public GameObject normalItemsRoot;
        public GameObject goodItemsRoot;
        public GameObject premiumItemsRoot;
        public GameObject medicineItemsRoot;

        [Header("分类按钮（可选，仅用于交互状态）")]
        public Button junkButton;
        public Button normalButton;
        public Button goodButton;
        public Button premiumButton;
        public Button medicineButton;

        private void Start()
        {
            // 默认先显示普通食物分类
            ShowNormal();
        }

        private void HideAllRoots()
        {
            if (junkItemsRoot != null) junkItemsRoot.SetActive(false);
            if (normalItemsRoot != null) normalItemsRoot.SetActive(false);
            if (goodItemsRoot != null) goodItemsRoot.SetActive(false);
            if (premiumItemsRoot != null) premiumItemsRoot.SetActive(false);
            if (medicineItemsRoot != null) medicineItemsRoot.SetActive(false);
        }

        private void ResetButtons()
        {
            if (junkButton != null) junkButton.interactable = true;
            if (normalButton != null) normalButton.interactable = true;
            if (goodButton != null) goodButton.interactable = true;
            if (premiumButton != null) premiumButton.interactable = true;
            if (medicineButton != null) medicineButton.interactable = true;
        }

        public void ShowJunk()
        {
            HideAllRoots();
            ResetButtons();

            if (junkItemsRoot != null) junkItemsRoot.SetActive(true);
            if (junkButton != null) junkButton.interactable = false; // 当前分类按钮置灰，表示已选中
        }

        public void ShowNormal()
        {
            HideAllRoots();
            ResetButtons();

            if (normalItemsRoot != null) normalItemsRoot.SetActive(true);
            if (normalButton != null) normalButton.interactable = false;
        }

        public void ShowGood()
        {
            HideAllRoots();
            ResetButtons();

            if (goodItemsRoot != null) goodItemsRoot.SetActive(true);
            if (goodButton != null) goodButton.interactable = false;
        }

        public void ShowPremium()
        {
            HideAllRoots();
            ResetButtons();

            if (premiumItemsRoot != null) premiumItemsRoot.SetActive(true);
            if (premiumButton != null) premiumButton.interactable = false;
        }

        public void ShowMedicine()
        {
            HideAllRoots();
            ResetButtons();

            if (medicineItemsRoot != null) medicineItemsRoot.SetActive(true);
            if (medicineButton != null) medicineButton.interactable = false;
        }
    }
}

