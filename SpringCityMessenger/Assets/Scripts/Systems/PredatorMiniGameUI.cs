using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 天敌小游戏「躲避猛禽」：
    /// - 三个落点，其中一个是猛禽（危险），其余为安全区
    /// - 每轮随机刷新猛禽位置，玩家需在限定时间内点击安全区
    /// - 点击猛禽或超时视为失败，累计若干轮成功则挑战成功
    /// </summary>
    public class PredatorMiniGameUI : MonoBehaviour
    {
        [Header("UI 引用")]
        public GameObject panelRoot;
        public Button leftButton;
        public Button middleButton;
        public Button rightButton;
        public TextMeshProUGUI tipText;
        public TextMeshProUGUI roundText;

        [Header("参数")]
        [Tooltip("需要成功的轮数")]
        public int requiredSuccessRounds = 3;
        [Tooltip("每一轮可反应的时间（秒）")]
        public float roundDuration = 2.0f;

        private MigrationEventSystem _eventSystem;
        private MigrationEventSystem.EventType _currentEvent;

        private int _currentRound;
        private int _successCount;
        private float _roundTimeLeft;
        private int _predatorIndex; // 0 左，1 中，2 右
        private bool _running;
        private bool _waitingForClick;

        private void Awake()
        {
            if (panelRoot == null)
                panelRoot = gameObject;

            if (leftButton != null) leftButton.onClick.AddListener(() => OnSpotClicked(0));
            if (middleButton != null) middleButton.onClick.AddListener(() => OnSpotClicked(1));
            if (rightButton != null) rightButton.onClick.AddListener(() => OnSpotClicked(2));

            Hide();
        }

        public void StartMiniGame(MigrationEventSystem eventSystem, MigrationEventSystem.EventType evt)
        {
            _eventSystem = eventSystem;
            _currentEvent = evt;

            _currentRound = 0;
            _successCount = 0;
            _running = true;

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                panelRoot.transform.SetAsLastSibling();
            }

            StartNextRound();
        }

        private void StartNextRound()
        {
            _currentRound++;
            _roundTimeLeft = roundDuration;
            _waitingForClick = true;

            // 随机一个位置作为猛禽
            _predatorIndex = Random.Range(0, 3);
            UpdateButtonsVisual();
            UpdateTexts();
        }

        private void Update()
        {
            if (!_running || !_waitingForClick) return;

            _roundTimeLeft -= Time.deltaTime;
            if (_roundTimeLeft <= 0f)
            {
                // 这一轮超时视为失败
                Finish(false);
            }
            else
            {
                UpdateTexts();
            }
        }

        private void OnSpotClicked(int index)
        {
            if (!_running || !_waitingForClick) return;

            _waitingForClick = false;

            if (index == _predatorIndex)
            {
                // 点到猛禽，失败
                Finish(false);
            }
            else
            {
                // 成功躲避一轮
                _successCount++;
                if (_successCount >= requiredSuccessRounds)
                {
                    Finish(true);
                }
                else
                {
                    StartNextRound();
                }
            }
        }

        private void Finish(bool success)
        {
            if (!_running) return;
            _running = false;
            _waitingForClick = false;

            if (_eventSystem != null)
            {
                if (success)
                    _eventSystem.OnChooseChallengeSuccess();
                else
                    _eventSystem.OnChooseChallengeFail();
            }

            Hide();
        }

        private void UpdateButtonsVisual()
        {
            SetButtonVisual(leftButton, 0 == _predatorIndex);
            SetButtonVisual(middleButton, 1 == _predatorIndex);
            SetButtonVisual(rightButton, 2 == _predatorIndex);
        }

        private void SetButtonVisual(Button btn, bool isPredator)
        {
            if (btn == null) return;
            var text = btn.GetComponentInChildren<TextMeshProUGUI>();
            var img = btn.GetComponent<Image>();

            if (isPredator)
            {
                if (text != null) text.text = "猛禽";
                if (img != null) img.color = new Color(0.8f, 0.2f, 0.2f);
            }
            else
            {
                if (text != null) text.text = "安全";
                if (img != null) img.color = new Color(0.2f, 0.6f, 0.2f);
            }
        }

        private void UpdateTexts()
        {
            if (roundText != null)
                roundText.text = $"第 {_currentRound} 轮 / 目标 {_successCount}/{requiredSuccessRounds}";

            if (tipText != null)
                tipText.text = $"请选择安全落点，避开猛禽！（剩余时间：{_roundTimeLeft:F1}s）";
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }
    }
}

