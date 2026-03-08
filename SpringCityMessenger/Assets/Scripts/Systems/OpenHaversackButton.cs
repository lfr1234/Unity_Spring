using UnityEngine;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 挂在「行囊」按钮上，点击时自动查找场景中的行囊面板并打开。
    /// 不需要在 Inspector 里拖任何引用，家园和迁徙场景都能用。
    /// </summary>
    public class OpenHaversackButton : MonoBehaviour
    {
        public void OpenHaversack()
        {
            var ui = FindObjectOfType<HaversackPanelUI>(true);
            if (ui == null)
            {
                Debug.LogWarning("[行囊] 当前场景没有找到 HaversackPanelUI。");
                return;
            }
            ui.ShowPanel();
            Debug.Log("[行囊] 已调用 ShowPanel，面板应已打开。");
        }
    }
}
