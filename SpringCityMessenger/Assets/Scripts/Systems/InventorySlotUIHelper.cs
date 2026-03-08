using SpringCityMessenger.Systems;

namespace SpringCityMessenger
{
    /// <summary>
    /// 提供给多个 UI 使用的公共静态方法。
    /// 目前只有：根据品质返回使用效果文字。
    /// </summary>
    public static class InventorySlotUIHelper
    {
        public static string GetEffectTextByQuality(FeedingSystem.FoodQuality quality)
        {
            switch (quality)
            {
                case FeedingSystem.FoodQuality.Junk:
                    return "体力+10，健康-5，饱食+15，经验+2（连续吃 3 次会生病）";
                case FeedingSystem.FoodQuality.Normal:
                    return "体力+20，健康+0，饱食+25，经验+5";
                case FeedingSystem.FoodQuality.Good:
                    return "体力+30，健康+5，饱食+35，经验+10";
                case FeedingSystem.FoodQuality.Premium:
                    return "体力+50，健康+10，饱食+45，经验+15";
                default:
                    return string.Empty;
            }
        }
    }
}

