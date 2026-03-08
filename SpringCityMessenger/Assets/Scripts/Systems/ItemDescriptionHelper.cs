namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 一些物品的默认文字说明，避免每次都在 Inspector 里手动填写。
    /// 如果配置表里没有写描述 / 效果，就会回退到这里。
    /// </summary>
    public static class ItemDescriptionHelper
    {
        public static string GetDefaultDescription(string itemId)
        {
            switch (itemId)
            {
                case "med_cold":
                    return "给海鸥用的小丸子，用来治疗生病。";
                default:
                    return string.Empty;
            }
        }

        public static string GetDefaultEffectDescription(string itemId)
        {
            switch (itemId)
            {
                case "med_cold":
                    return "恢复一点健康，并清除生病状态。";
                default:
                    return string.Empty;
            }
        }
    }
}

