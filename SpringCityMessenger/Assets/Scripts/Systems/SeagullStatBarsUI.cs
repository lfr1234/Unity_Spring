using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpringCityMessenger.Core;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 根据 SeagullStatus，刷新下方三条属性条和等级文本。
    /// </summary>
    public class SeagullStatBarsUI : MonoBehaviour
    {
        [Header("引用")]
        public SeagullStatus seagull;

        public Image staminaFill;
        public Image healthFill;
        public Image hungerFill;

        public TextMeshProUGUI levelText;

        private void Awake()
        {
            if (seagull == null)
            {
                seagull = FindObjectOfType<SeagullStatus>();
            }

            UpdateUI();
        }

        private void Update()
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (seagull == null) return;

            if (staminaFill != null)
                staminaFill.fillAmount = seagull.stamina / 100f;

            if (healthFill != null)
                healthFill.fillAmount = seagull.health / 100f;

            if (hungerFill != null)
                hungerFill.fillAmount = seagull.hunger / 100f;

            if (levelText != null)
                levelText.text = $"Lv.{seagull.level}";
        }
    }
}

