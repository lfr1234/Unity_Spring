using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 恶劣天气小游戏「稳定飞行」：
    /// - 一根平衡条中间是安全区，指针会被风左右推
    /// - 玩家不断点击来把指针拉回中间，尽量保持在安全区内
    /// - 挑战结果通过 MigrationEventSystem.OnChooseChallengeSuccess/Fail 回传
    /// </summary>
    public class MigrationMiniGameUI : MonoBehaviour
    {
        [Header("UI 引用")]
        public GameObject panelRoot;
        public Button tapButton;
        [Tooltip("状态提示文本，如“保持平衡！”、“快要失衡了！”")]
        public TextMeshProUGUI tapCountText;
        public TextMeshProUGUI timerText;
        [Tooltip("整根平衡条的 RectTransform，用于计算指针位置")]
        public RectTransform balanceBar;
        [Tooltip("表示当前平衡位置的指针/小圆点")]
        public RectTransform pointer;

        [Header("参数")]
        [Tooltip("小游戏时长（秒）")]
        public float duration = 5f;
        [Tooltip("指针偏移范围（-maxDrift ~ +maxDrift 映射到整根条）")]
        public float maxDrift = 1f;
        [Tooltip("安全区范围（绝对值小于该值视为安全）")]
        public float safeZone = 0.4f;
        [Tooltip("基础风力大小")]
        public float baseWindSpeed = 0.6f;
        [Tooltip("风力随机扰动大小")]
        public float windJitter = 0.8f;
        [Tooltip("每次点击将指针向中间拉回的力度")]
        public float tapForce = 0.5f;
        [Tooltip("指针连续在危险区最多允许时间（秒），超过则失败）")]
        public float maxOutOfSafeTime = 1.5f;

        private MigrationEventSystem _eventSystem;
        private MigrationEventSystem.EventType _currentEvent;

        private float _timeLeft;
        private bool _running;
        private float _driftPos;     // -maxDrift ~ +maxDrift，0 为中间
        private float _windSpeed;    // 当前风力
        private float _outOfSafeTime;

        private void Awake()
        {
            if (panelRoot == null)
                panelRoot = gameObject;

            if (tapButton != null)
                tapButton.onClick.AddListener(OnTapButtonClick);

            Hide();
        }

        public void StartMiniGame(MigrationEventSystem eventSystem, MigrationEventSystem.EventType evt)
        {
            _eventSystem = eventSystem;
            _currentEvent = evt;

            _timeLeft = duration;
            _running = true;
            _driftPos = 0f;
            _windSpeed = baseWindSpeed * (Random.value > 0.5f ? 1f : -1f);
            _outOfSafeTime = 0f;

            RefreshTexts();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                panelRoot.transform.SetAsLastSibling();
            }
        }

        private void Update()
        {
            if (!_running) return;

            // 更新风力（缓慢抖动）
            float jitter = Random.Range(-windJitter, windJitter) * Time.deltaTime;
            _windSpeed += jitter;
            _windSpeed = Mathf.Clamp(_windSpeed, -baseWindSpeed - windJitter, baseWindSpeed + windJitter);

            // 风推动指针
            _driftPos += _windSpeed * Time.deltaTime;
            _driftPos = Mathf.Clamp(_driftPos, -maxDrift, maxDrift);

            // 记录在危险区的时间
            if (Mathf.Abs(_driftPos) > safeZone)
            {
                _outOfSafeTime += Time.deltaTime;
                if (_outOfSafeTime >= maxOutOfSafeTime)
                {
                    FinishMiniGame(false);
                    return;
                }
            }
            else
            {
                _outOfSafeTime = 0f;
            }

            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0f)
            {
                _timeLeft = 0f;
                FinishMiniGame(true);
                return;
            }

            RefreshTexts();
        }

        private void RefreshTexts()
        {
            if (tapCountText != null)
            {
                if (Mathf.Abs(_driftPos) <= safeZone * 0.5f)
                    tapCountText.text = "飞行稳定，继续保持！";
                else if (Mathf.Abs(_driftPos) <= safeZone)
                    tapCountText.text = "有点偏了，快点击修正！";
                else
                    tapCountText.text = "危险！赶快点击保持平衡！";
            }

            if (timerText != null)
                timerText.text = $"剩余时间：{_timeLeft:F1}s";

            // 更新指针位置
            if (pointer != null && balanceBar != null)
            {
                float halfWidth = balanceBar.rect.width * 0.5f;
                float x = Mathf.Clamp(_driftPos / maxDrift, -1f, 1f) * halfWidth;
                var anchored = pointer.anchoredPosition;
                anchored.x = x;
                pointer.anchoredPosition = anchored;
            }
        }

        private void OnTapButtonClick()
        {
            if (!_running) return;

            // 每次点击把指针往中间拉
            _driftPos = Mathf.MoveTowards(_driftPos, 0f, tapForce);
            RefreshTexts();
        }

        private void FinishMiniGame(bool success)
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

