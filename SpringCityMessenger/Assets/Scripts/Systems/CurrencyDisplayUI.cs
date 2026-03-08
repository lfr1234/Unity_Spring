using UnityEngine;
using TMPro;
using SpringCityMessenger.Core;

public class CurrencyDisplayUI : MonoBehaviour
{
    public TextMeshProUGUI berryCountText;
    public TextMeshProUGUI fishCountText;

    [Tooltip("在 Inspector 里填「浆果: 」或你想要的文字，脚本只会在后面加数字")]
    public string berryLabel;
    [Tooltip("在 Inspector 里填「小鱼: 」或你想要的文字，脚本只会在后面加数字")]
    public string fishLabel;

    private void Update()
    {
        if (GameManager.Instance == null) return;

        string bl = string.IsNullOrEmpty(berryLabel) ? "Berry: " : berryLabel;
        string fl = string.IsNullOrEmpty(fishLabel) ? "Fish: " : fishLabel;

        if (berryCountText != null)
            berryCountText.text = bl + GameManager.Instance.berryCount;

        if (fishCountText != null)
            fishCountText.text = fl + GameManager.Instance.fishCount;
    }
}
