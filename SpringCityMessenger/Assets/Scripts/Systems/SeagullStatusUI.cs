using UnityEngine;
using SpringCityMessenger.Core;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 负责根据 SeagullStatus 的状态，控制海鸥头顶的小图标等视觉效果。
    /// 目前只处理：生病状态 isSick → 显示 / 隐藏生病图标。
    /// </summary>
    public class SeagullStatusUI : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("海鸥的核心状态脚本")]
        public SeagullStatus seagullStatus;

        [Tooltip("用于表示生病的小图标（一个挂在海鸥上方的 Image）")]
        public GameObject sickIcon;

        private void Awake()
        {
            if (seagullStatus == null)
            {
                seagullStatus = FindObjectOfType<SeagullStatus>();
            }

            // 启动时根据当前状态刷新一次
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (sickIcon == null || seagullStatus == null)
            {
                return;
            }

            // 生病 → 显示图标；未生病 → 隐藏图标
            if (sickIcon.activeSelf != seagullStatus.isSick)
            {
                sickIcon.SetActive(seagullStatus.isSick);
            }
        }
    }
}