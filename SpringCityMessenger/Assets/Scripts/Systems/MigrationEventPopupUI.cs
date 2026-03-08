using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 迁徙事件弹窗：遭遇威胁时显示，玩家选择「挑战」或「躲避」。
    /// 需在迁徙场景的 Canvas 下建一个 Panel，挂此脚本，并拖好标题、描述、两个按钮的引用。
    /// </summary>
    public class MigrationEventPopupUI : MonoBehaviour
    {
        [Header("UI 引用")]
        public GameObject panelRoot;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI eventNameText;
        public TextMeshProUGUI descText;
        [Tooltip("可选：显示「挑战/躲避」的判定规则")]
        public TextMeshProUGUI choiceRuleText;
        public Button challengeButton;
        public Button evadeButton;

        [Header("挑战结果选择（测试用，未来替换为小游戏）")]
        [Tooltip("选「挑战」后显示的 Panel，内含「挑战成功」「挑战失败」两个按钮")]
        public GameObject challengeResultPanel;
        [Tooltip("可选：显示「小游戏接口预留，请选择测试结果」等提示")]
        public TextMeshProUGUI challengeResultHintText;
        public Button challengeSuccessButton;
        public Button challengeFailButton;

        [Header("文案（可 Inspector 填）")]
        public string titleLabel = "\u9047\u5230\u5a76\u613f\uff01";  // 遭遇威胁！
        public string challengeResultHint = "\u5c0f\u6e38\u620f\u63a5\u53e3\u9884\u7559\uff0c\u8bf7\u9009\u62e9\u6d4b\u8bd5\u7ed3\u679c\uff1a";  // 小游戏接口预留，请选择测试结果：

        private MigrationEventSystem _eventSystem;
        private MigrationEventSystem.EventType _currentEvent;

        public bool HasChallengeTestUIConfigured =>
            challengeResultPanel != null && challengeSuccessButton != null && challengeFailButton != null;

        private void Awake()
        {
            if (panelRoot == null)
            {
                // 若脚本挂在子物体上，找到所在 Canvas 下的根 Panel
                Transform t = transform;
                while (t.parent != null && t.parent.GetComponent<Canvas>() == null)
                    t = t.parent;
                panelRoot = t.gameObject;
            }
            if (challengeButton != null) challengeButton.onClick.AddListener(OnChallengeClick);
            if (evadeButton != null) evadeButton.onClick.AddListener(OnEvadeClick);
            if (challengeSuccessButton != null) challengeSuccessButton.onClick.AddListener(OnChallengeSuccessClick);
            if (challengeFailButton != null) challengeFailButton.onClick.AddListener(OnChallengeFailClick);
            if (challengeResultPanel != null) challengeResultPanel.SetActive(false);
            Hide();
        }

        /// <summary>
        /// 显示事件弹窗。
        /// </summary>
        public void Show(MigrationEventSystem eventSystem, MigrationEventSystem.EventType evt)
        {
            _eventSystem = eventSystem;
            _currentEvent = evt;

            if (titleText != null) titleText.text = titleLabel;
            if (eventNameText != null) eventNameText.text = MigrationEventSystem.GetEventName(evt);
            if (descText != null) descText.text = MigrationEventSystem.GetEventDesc(evt);
            if (choiceRuleText != null)
                choiceRuleText.text = MigrationEventSystem.GetChallengeRuleText(evt) + "\n\n" + MigrationEventSystem.GetEvadeRuleText();
            if (challengeResultPanel != null) challengeResultPanel.SetActive(false);

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                // 移到 Canvas 最后，确保盖在所有 UI 之上
                panelRoot.transform.SetAsLastSibling();
                var canvas = panelRoot.GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = panelRoot.AddComponent<Canvas>();
                    canvas.overrideSorting = true;
                    canvas.sortingOrder = 200;
                    if (panelRoot.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                        panelRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
                else
                {
                    canvas.overrideSorting = true;
                    canvas.sortingOrder = 200;
                }
            }
        }

        public void Hide()
        {
            if (challengeResultPanel != null) challengeResultPanel.SetActive(false);
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnChallengeClick()
        {
            if (_eventSystem == null) return;
            _eventSystem.OnChooseChallenge();
        }

        /// <summary>
        /// 选「挑战」后显示成功/失败按钮，供测试；未来小游戏完成后由此处接入。
        /// </summary>
        public void ShowChallengeResultButtons()
        {
            // 如果你没把测试用按钮/面板拖引用，也不要卡死：回退为随机判定
            if (!HasChallengeTestUIConfigured)
            {
                if (_eventSystem != null)
                    _eventSystem.ResolveChallengeByRandom();
                return;
            }

            if (challengeResultHintText != null && !string.IsNullOrEmpty(challengeResultHint))
                challengeResultHintText.text = challengeResultHint;
            if (challengeResultPanel != null)
                challengeResultPanel.SetActive(true);
        }

        private void OnChallengeSuccessClick()
        {
            if (_eventSystem != null) _eventSystem.OnChooseChallengeSuccess();
        }

        private void OnChallengeFailClick()
        {
            if (_eventSystem != null) _eventSystem.OnChooseChallengeFail();
        }

        private void OnEvadeClick()
        {
            if (_eventSystem != null)
                _eventSystem.OnChooseEvade();
        }
    }
}
