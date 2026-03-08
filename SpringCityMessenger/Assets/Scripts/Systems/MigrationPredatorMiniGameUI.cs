using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 天敌小游戏「躲避猛禽」：
    /// - 画面上有左/中/右三个区域，其中一个是危险区
    /// - 每一轮玩家要在倒计时结束前点击任意一个“安全区”
    /// - 点击危险区或超时不点都算本轮失败，多轮后决定挑战成败
    /// </summary>
    public class MigrationPredatorMiniGameUI : MonoBehaviour
    {
        [Header("UI 引用")]
        public GameObject panelRoot;
        public Button leftButton;
        public Button middleButton;
        public Button rightButton;
        public TextMeshProUGUI leftLabel;
        public TextMeshProUGUI middleLabel;
        public TextMeshProUGUI rightLabel;
        public TextMeshProUGUI roundText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI statusText;

        [Header("参数")]
        [Tooltip("总轮数")]
        public int totalRounds = 6;
        [Tooltip("每轮时长（秒）")]
        public float roundDuration = 1.2f;
        [Tooltip("允许失败的最大轮数，超过则挑战失败")]
        public int maxFailRounds = 2;

        private MigrationEventSystem _eventSystem;
        private MigrationEventSystem.EventType _currentEvent;

        private int _currentRound;
        private float _timeLeftInRound;
        private int _failCount;
        private bool _running;
        private int _dangerLane;     // 0 左 / 1 中 / 2 右
        private bool _clickedThisRound;

        private void Awake()
        {
            if (panelRoot == null)
                panelRoot = gameObject;

            if (leftButton != null) leftButton.onClick.AddListener(() => OnLaneClick(0));
            if (middleButton != null) middleButton.onClick.AddListener(() => OnLaneClick(1));
            if (rightButton != null) rightButton.onClick.AddListener(() => OnLaneClick(2));

            Hide();
        }

        public void StartMiniGame(MigrationEventSystem eventSystem, MigrationEventSystem.EventType evt)
        {
            _eventSystem = eventSystem;
            _currentEvent = evt;

            _currentRound = 0;
            _failCount = 0;
            _running = true;

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                panelRoot.transform.SetAsLastSibling();
            }

            NextRound();
        }

        private void Update()
        {
            if (!_running) return;

            _timeLeftInRound -= Time.deltaTime;
            if (_timeLeftInRound <= 0f)
            {
                // 一轮结束：如果没点击，算失败一次
                if (!_clickedThisRound)
                    _failCount++;

                if (_currentRound >= totalRounds || _failCount > maxFailRounds)
                {
                    FinishGame(_failCount <= maxFailRounds);
                    return;
                }

                NextRound();
                return;
            }

            RefreshTexts();
        }

        private void NextRound()
        {
            _currentRound++;
            _timeLeftInRound = roundDuration;
            _clickedThisRound = false;

            _dangerLane = Random.Range(0, 3);
            UpdateLaneLabels();
            RefreshTexts();
        }

        private void UpdateLaneLabels()
        {
            SetLaneLabel(leftLabel, 0);
            SetLaneLabel(middleLabel, 1);
            SetLaneLabel(rightLabel, 2);
        }

        private void SetLaneLabel(TextMeshProUGUI label, int lane)
        {
            if (label == null) return;

            if (lane == _dangerLane)
                label.text = "猛禽来袭！不要点";
            else
                label.text = "安全区";
        }

        private void RefreshTexts()
        {
            if (roundText != null)
                roundText.text = $"第 {_currentRound}/{totalRounds} 轮";

            if (timerText != null)
                timerText.text = $"本轮剩余：{_timeLeftInRound:F1}s";

            if (statusText != null)
            {
                if (_clickedThisRound)
                    statusText.text = "看结果…";
                else
                    statusText.text = "猛禽出现了！请点击任一“安全区”躲避！";
            }
        }

        private void OnLaneClick(int lane)
        {
            if (!_running || _clickedThisRound) return;

            _clickedThisRound = true;

            bool success = lane != _dangerLane;
            if (!success)
                _failCount++;

            if (_currentRound >= totalRounds || _failCount > maxFailRounds)
            {
                FinishGame(_failCount <= maxFailRounds);
            }
            else
            {
                NextRound();
            }
        }

        private void FinishGame(bool success)
        {
            if (!_running) return;
            _running = false;

            if (_eventSystem != null)
            {
                if (success)
                    _eventSystem.OnChooseChallengeSuccess();
                else
                    _eventSystem.OnChooseChallengeFail();
            }

            Hide();
        }

        public void Hide()
        {
            _running = false;
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }
    }
}