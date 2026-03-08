using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 人为障碍小游戏「左右闪避」：
    /// - 画面上有三条“路线”（左中右），海鸥在其中一条上自动前进
    /// - 每一轮会提示哪一条路线即将出现障碍，玩家需要用左右按钮切换到安全路线
    /// - 多轮后根据被障碍击中的次数判定挑战成功 / 失败
    /// </summary>
    public class MigrationObstacleMiniGameUI : MonoBehaviour
    {
        [Header("UI 引用")]
        public GameObject panelRoot;
        public RectTransform seagullMarker;       // 表示海鸥当前位置的小图标
        public RectTransform laneLeft;
        public RectTransform laneMiddle;
        public RectTransform laneRight;
        public Button moveLeftButton;
        public Button moveRightButton;
        public TextMeshProUGUI hintText;
        public TextMeshProUGUI roundText;
        public TextMeshProUGUI timerText;

        [Header("参数")]
        [Tooltip("总轮数")]
        public int totalRounds = 6;
        [Tooltip("每轮持续时间（秒）")]
        public float roundDuration = 1.2f;
        [Tooltip("允许被障碍击中的最大次数")]
        public int maxHitsAllowed = 2;

        private MigrationEventSystem _eventSystem;
        private MigrationEventSystem.EventType _currentEvent;

        private int _currentLane = 1;   // 0 左 1 中 2 右
        private int _dangerLane;
        private int _currentRound;
        private int _hitCount;
        private float _timeLeftInRound;
        private bool _running;
        private bool _clickedThisRound;

        private void Awake()
        {
            if (panelRoot == null)
                panelRoot = gameObject;

            if (moveLeftButton != null) moveLeftButton.onClick.AddListener(MoveLeft);
            if (moveRightButton != null) moveRightButton.onClick.AddListener(MoveRight);

            Hide();
        }

        public void StartMiniGame(MigrationEventSystem eventSystem, MigrationEventSystem.EventType evt)
        {
            _eventSystem = eventSystem;
            _currentEvent = evt;

            _currentLane = 1;
            _currentRound = 0;
            _hitCount = 0;
            _running = true;

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                panelRoot.transform.SetAsLastSibling();
            }

            UpdateSeagullMarker();
            NextRound();
        }

        private void Update()
        {
            if (!_running) return;

            _timeLeftInRound -= Time.deltaTime;
            if (_timeLeftInRound <= 0f)
            {
                // 到时间：如果还没点击过，直接判定一轮结果
                if (!_clickedThisRound && _currentLane == _dangerLane)
                    _hitCount++;

                if (_currentRound >= totalRounds || _hitCount > maxHitsAllowed)
                {
                    FinishGame(_hitCount <= maxHitsAllowed);
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

            // 随机选择一个危险路线
            _dangerLane = Random.Range(0, 3);

            RefreshTexts();
        }

        private void RefreshTexts()
        {
            if (roundText != null)
                roundText.text = $"第 {_currentRound}/{totalRounds} 轮  被撞：{_hitCount}/{maxHitsAllowed}";

            if (timerText != null)
                timerText.text = $"本轮剩余：{_timeLeftInRound:F1}s";

            if (hintText != null)
            {
                string dangerLaneName = _dangerLane == 0 ? "左侧" : _dangerLane == 1 ? "中间" : "右侧";
                hintText.text = $"前方{dangerLaneName}有障碍！请用左右按钮切换到安全路线。";
            }
        }

        private void MoveLeft()
        {
            if (!_running) return;
            if (_currentLane > 0)
            {
                _currentLane--;
                _clickedThisRound = true;
                UpdateSeagullMarker();
            }
        }

        private void MoveRight()
        {
            if (!_running) return;
            if (_currentLane < 2)
            {
                _currentLane++;
                _clickedThisRound = true;
                UpdateSeagullMarker();
            }
        }

        private void UpdateSeagullMarker()
        {
            if (seagullMarker == null) return;

            RectTransform targetLane = _currentLane == 0 ? laneLeft :
                                       _currentLane == 1 ? laneMiddle :
                                       laneRight;
            if (targetLane == null) return;

            Vector2 pos = seagullMarker.anchoredPosition;
            pos.x = targetLane.anchoredPosition.x;
            seagullMarker.anchoredPosition = pos;
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

