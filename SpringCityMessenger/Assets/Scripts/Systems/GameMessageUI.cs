using UnityEngine;
using TMPro;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 在游戏画面上显示一条临时提示（如“迁徙成功”“装行囊完成”），几秒后自动消失。
    /// 任何脚本可调用 GameMessageUI.Show("内容")。
    /// </summary>
    public class GameMessageUI : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("用于显示提示的 TextMeshProUGUI，放在 Canvas 中央")]
        public TextMeshProUGUI messageText;

        [Header("显示时长（秒）")]
        public float displayDuration = 2.5f;

        private static GameMessageUI _instance;
        private float _hideTime;

        private void Awake()
        {
            if (messageText == null)
            {
                messageText = GetComponentInChildren<TextMeshProUGUI>();
            }
            _instance = this;
            if (messageText != null)
            {
                messageText.text = "";
                messageText.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void Update()
        {
            if (_instance == null || _instance.messageText == null) return;
            if (_instance.messageText.gameObject.activeSelf && Time.time >= _instance._hideTime)
            {
                _instance.messageText.text = "";
                _instance.messageText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 在画面上显示一条提示，若干秒后自动消失。
        /// </summary>
        public static void Show(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameMessageUI>();
            }
            if (_instance == null)
            {
                Debug.Log("[GameMessageUI] 场景中未找到 GameMessageUI，提示仅输出到 Console：" + text);
                return;
            }
            if (_instance.messageText == null)
            {
                Debug.Log("[GameMessageUI] 未设置 messageText，提示仅输出到 Console：" + text);
                return;
            }
            _instance.messageText.text = text;
            _instance.messageText.gameObject.SetActive(true);
            _instance._hideTime = Time.time + _instance.displayDuration;
        }
    }
}
