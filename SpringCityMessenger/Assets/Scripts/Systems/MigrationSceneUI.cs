using UnityEngine;
using TMPro;
using SpringCityMessenger.Systems;

public class MigrationSceneUI : MonoBehaviour
{
    [Header("引用")]
    public MigrationSystem migrationSystem;
    public TextMeshProUGUI dayInfoText;
    public TextMeshProUGUI regionText;

    // Unicode 转义，避免运行后字体/编码导致问号
    private static readonly string DayPrefix = "\u7b2c ";   // 第
    private static readonly string DayUnit = "\u5929 / \u5171 ";  // 天 / 共
    private static readonly string DaySuffix = "\u5929";    // 天
    private static readonly string RegionPrefix = "\u5f53\u524d\u4f4d\u7f6e\uff1a";  // 当前位置：

    private void Awake()
    {
        if (migrationSystem == null)
            migrationSystem = FindObjectOfType<MigrationSystem>();

        // 兜底：迁徙场景 Canvas 有时 scale 被设成 0，导致行囊等按钮点不了，运行时强制修复
        foreach (var c in FindObjectsOfType<Canvas>(true))
        {
            var r = c.GetComponent<RectTransform>();
            if (r != null && r.localScale == Vector3.zero)
            {
                r.localScale = Vector3.one;
                Debug.Log("[MigrationSceneUI] 已修复 Canvas scale 为 1: " + c.gameObject.name);
            }
        }
    }

    private void Update()
    {
        if (migrationSystem == null) return;

        int day = migrationSystem.currentDay;
        int total = migrationSystem.totalDays;
        day = Mathf.Clamp(day, 0, total);

        if (dayInfoText != null)
            dayInfoText.text = DayPrefix + day + DayUnit + total + DaySuffix;

        if (regionText != null)
            regionText.text = RegionPrefix + GetRegionName(day, total);
    }

    private static string GetRegionName(int day, int total)
    {
        if (day <= 0) return "\u51fa\u53d1\u5730"; // 出发地
        float t = (float)day / Mathf.Max(1, total);
        if (t <= 6f / 30f) return "\u897f\u4f2f\u5229\u4e9a";   // 西伯利亚
        if (t <= 12f / 30f) return "\u8499\u53e4\u8349\u539f";  // 蒙古草原
        if (t <= 18f / 30f) return "\u534e\u5317\u5e73\u539f";  // 华北平原
        if (t <= 24f / 30f) return "\u79e6\u5cad\u5c71\u8109";  // 秦岭山脉
        return "\u4e91\u8d35\u9ad8\u539f";                      // 云贵高原
    }
}
