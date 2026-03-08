using UnityEngine;
using UnityEngine.SceneManagement;
using SpringCityMessenger.Core;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 迁徙流程的最简版逻辑：天数推进、体力消耗、吃行囊食物的节奏。
    /// 先搭一个"按天前进"的框架，之后再慢慢接入事件与小游戏。
    /// </summary>
    public class MigrationSystem : MonoBehaviour
    {
        private static MigrationSystem _instance;

        [Header("当前迁徙进度")]
        public int currentDay = 0;
        public int totalDays = 30; // 完整迁徙 30 天

        [Header("每日体力消耗")]
        public int staminaCostPerDay = 2;

        [Header("吃饭节奏")]
        [Tooltip("是否开启“每隔几天自动吃一次行囊食物”")]
        public bool autoEatFromHaversack = false;

        [Tooltip("自动吃行囊时，每隔多少天吃一次（例如 5 表示每 5 天自动吃一份）")]
        public int eatIntervalDays = 5;

        [Header("行囊配置引用（可选）")]
        public BackpackSystem backpackSystem;

        [Header("完整迁徙成功奖励（策划案）")]
        public int rewardBerry = 150;
        public int rewardFish = 15;
        public int rewardExp = 50;

        [Header("场景名称设置")]
        [Tooltip("家园场景名称，需要和 Build Settings 中保持一致")]
        public string homeSceneName = "Scenes/HomeScene";

        [Header("结束返回家园设置")]
        [Tooltip("迁徙成功/失败后，等待多少秒再返回家园")]
        public float returnDelaySeconds = 1.5f;

        private SeagullStatus _seagull;
        private CurrencyManager _currency;
        private bool _isEnding = false;
        private bool _pendingDayAdvance = false;

        private void Awake()
        {
            // 防止场景切换时出现两个 MigrationSystem 同时运行
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            _seagull = FindObjectOfType<SeagullStatus>();
            _currency = FindObjectOfType<CurrencyManager>();
        }

        /// <summary>
        /// 在 UI 上挂一个按钮，点击时调用这个方法，模拟"继续飞一天"。
        /// </summary>
        public void FlyOneDay()
        {
            FlyOneDayInternal(triggerEventAfterFlight: true);
        }

        /// <summary>
        /// 供事件系统调用：推进到下一天，但不在结尾再次触发事件（避免连环弹窗）。
        /// </summary>
        public void FlyOneDayNoEvent()
        {
            FlyOneDayInternal(triggerEventAfterFlight: false);
        }

        private void FlyOneDayInternal(bool triggerEventAfterFlight)
        {
            if (_isEnding) return;

            var eventSystem = FindObjectOfType<MigrationEventSystem>();
            if (eventSystem != null && eventSystem.IsEventPending)
            {
                // 之前如果弹窗没显示出来，会导致一直卡住；这里尝试把弹窗重新弹出，并给玩家一个提示
                eventSystem.TryShowPendingPopup();
                GameMessageUI.Show("有事件未处理，请先选择挑战或躲避。");
                return;
            }

            if (_seagull == null)
            {
                Debug.LogWarning("SeagullStatus 未找到，无法进行迁徙。");
                return;
            }

            int dayBeforeCheck = currentDay;
            if (!CanStartOrContinueMigration())
            {
                // 已经在迁徙途中（飞过至少 1 天）但体力/健康不够 → 算迁徙失败，清空进度
                if (dayBeforeCheck > 0)
                {
                    Debug.Log("迁徙失败：体力或健康不足，无法继续。返回家园，下次可重新出发。");
                    GameMessageUI.Show("迁徙失败，返回家园。");
                    currentDay = 0;
                    // 自动切回家园场景（带一点延迟，方便看清提示）
                    StartReturnHome();
                }
                else
                {
                    Debug.Log("当前状态不满足迁徙条件。");
                }
                return;
            }

            // 已经抵达昆明，不允许再继续飞行
            if (currentDay >= totalDays)
            {
                Debug.Log("已经抵达昆明，迁徙结束。");
                return;
            }

            // 新流程：点“继续飞行”时先决定是否遭遇事件（不推进天数），
            // 事件结算后再推进当天，避免“先+1天才弹窗”的割裂感。
            int dayToFly = currentDay + 1;
            if (eventSystem != null && triggerEventAfterFlight)
            {
                bool triggered = eventSystem.TryTriggerEventForDay(dayToFly);
                if (triggered)
                {
                    _pendingDayAdvance = true;
                    return;
                }
            }

            AdvanceOneDay(dayToFly, allowTriggerEvent: false);
            if (triggerEventAfterFlight)
                GameMessageUI.Show("今天平安无事。");
        }

        /// <summary>
        /// 由事件系统在玩家完成选择后调用：把本次“继续飞行”对应的天数推进掉。
        /// </summary>
        public void CompletePendingDayAdvance()
        {
            if (!_pendingDayAdvance) return;
            _pendingDayAdvance = false;
            if (_isEnding) return;
            int dayToFly = currentDay + 1;
            AdvanceOneDay(dayToFly, allowTriggerEvent: false);
        }

        private void AdvanceOneDay(int dayToFly, bool allowTriggerEvent)
        {
            if (_isEnding) return;
            if (currentDay >= totalDays) return;

            currentDay = dayToFly;

            // 每天固定体力消耗
            _seagull.ChangeStamina(-staminaCostPerDay);
            // 每天饱食度略微下降一点，体现长途消耗
            _seagull.ChangeHunger(-1);

            // 如果刚好到达终点，优先结算成功（不再继续吃行囊/触发事件）
            if (currentDay >= totalDays)
            {
                CompleteSuccess();
                return;
            }

            // 自动吃行囊：默认关闭，只在 Inspector 勾选 autoEatFromHaversack 时按间隔天数触发
            if (autoEatFromHaversack && eatIntervalDays > 0 && currentDay % eatIntervalDays == 0)
            {
                if (backpackSystem == null)
                    backpackSystem = FindObjectOfType<BackpackSystem>();

                int restoreSta = 0, restoreHun = 0;
                bool ate = backpackSystem != null && backpackSystem.ConsumeFoodForMigration(out restoreSta, out restoreHun);

                if (ate)
                {
                    _seagull.ChangeStamina(restoreSta);
                    _seagull.ChangeHunger(restoreHun);
                    Debug.Log($"第 {currentDay} 天：自动吃掉行囊食物，体力+{restoreSta}，饱食+{restoreHun}。");
                }
                else
                {
                    if (backpackSystem == null)
                        Debug.LogWarning("[行囊] 场景里没有 BackpackSystem，请给 Backpack 挂上 BackpackSystem，或在 MigrationSystem 里拖上 Backpack。");
                    else
                        Debug.Log($"第 {currentDay} 天：行囊里没有可吃的食物（四种数量都是0）。");
                }
            }

            Debug.Log($"第 {currentDay} 天迁徙，当前体力：{_seagull.stamina}，健康：{_seagull.health}，饱食：{_seagull.hunger}");

            // 失败判断：体力或健康归零 → 结算并清空进度，方便下次再出发
            if (_seagull.stamina <= 0 || _seagull.health <= 0)
            {
                Debug.Log("迁徙失败：体力或健康归零。返回家园，下次可重新出发。");
                GameMessageUI.Show("迁徙失败，返回家园。");
                currentDay = 0;
                StartReturnHome();
                return;
            }

            // 保留扩展：如果未来要“结尾触发事件”，可以在这里打开 allowTriggerEvent
            if (allowTriggerEvent)
            {
                var eventSystem = FindObjectOfType<MigrationEventSystem>();
                if (eventSystem != null)
                    eventSystem.TryTriggerEvent();
            }
        }

        /// <summary>
        /// 手动从行囊吃一份食物：在迁徙场景里的“吃行囊”按钮调用。
        /// 优先吃高级 > 优质 > 普通 > 垃圾。
        /// </summary>
        public void EatFromHaversackOnce()
        {
            if (_isEnding) return;

            if (_seagull == null)
                _seagull = FindObjectOfType<SeagullStatus>();

            if (backpackSystem == null)
                backpackSystem = BackpackSystem.Instance ?? FindObjectOfType<BackpackSystem>();

            if (backpackSystem == null)
            {
                Debug.LogWarning("[行囊] 未找到 BackpackSystem，无法吃行囊食物。");
                GameMessageUI.Show("行囊不存在，无法吃东西。");
                return;
            }

            int restoreSta, restoreHun;
            bool ate = backpackSystem.ConsumeFoodForMigration(out restoreSta, out restoreHun);
            if (!ate)
            {
                GameMessageUI.Show("行囊里没有可吃的食物。");
                return;
            }

            if (_seagull == null)
            {
                Debug.LogWarning("[行囊] 未找到 SeagullStatus，无法加属性。");
                return;
            }

            _seagull.ChangeStamina(restoreSta);
            _seagull.ChangeHunger(restoreHun);
            GameMessageUI.Show($"吃了一份行囊食物，体力+{restoreSta}，饱食+{restoreHun}");
        }

        private void CompleteSuccess()
        {
            // 调试：确保你在 Console 里能看到 totalDays 的实际值
            Debug.Log($"[结算] 达到终点：currentDay={currentDay}, totalDays={totalDays}");

            if (_currency != null)
            {
                _currency.AddBerry(rewardBerry);
                _currency.AddFish(rewardFish);
            }
            if (_seagull != null)
            {
                _seagull.AddExp(rewardExp);
            }
            Debug.Log($"迁徙成功，抵达昆明！获得 浆果×{rewardBerry}，小鱼×{rewardFish}，经验+{rewardExp}。");
            GameMessageUI.Show("迁徙成功，抵达昆明！");
            StartReturnHome();
        }

        /// <summary>
        /// 由 MigrationEventSystem 在事件后调用：体力/健康过低时请求迁徙失败回家。
        /// </summary>
        public void RequestMigrationFailure(string message = null)
        {
            if (_isEnding) return;
            currentDay = 0;
            GameMessageUI.Show(string.IsNullOrEmpty(message) ? "迁徙失败，返回家园。" : message);
            StartReturnHome();
        }

        private void StartReturnHome()
        {
            if (_isEnding) return;
            _isEnding = true;

            // 返回家园前先存一次档，确保奖励/状态能带回去
            var save = FindObjectOfType<GameSaveManager>();
            if (save != null)
                save.SaveGame();

            if (!string.IsNullOrEmpty(homeSceneName))
            {
                StartCoroutine(ReturnHomeAfterDelay());
            }
        }

        private System.Collections.IEnumerator ReturnHomeAfterDelay()
        {
            yield return new WaitForSeconds(returnDelaySeconds);
            SceneManager.LoadScene(homeSceneName);
        }

        /// <summary>
        /// 校验是否满足出发 / 继续迁徙的基本条件。
        /// 出发（第 0 天）要求体力≥50、饱食≥30；继续飞行只要求体力≥单日消耗且未死亡。
        /// </summary>
        public bool CanStartOrContinueMigration()
        {
            if (_seagull.level < 3)
            {
                Debug.Log("等级不足，至少需要 Lv.3 才能迁徙。");
                return false;
            }

            // 出发时要求 50 体力；已经在路上时只要求够飞一天
            int staminaRequired = (currentDay == 0) ? 50 : staminaCostPerDay;
            if (_seagull.stamina < staminaRequired)
            {
                Debug.Log(currentDay == 0 ? "体力不足，至少需要 50 点体力才能出发。" : "体力不足，无法继续飞行。");
                return false;
            }

            if (_seagull.health < 30)
            {
                Debug.Log("健康度太低，无法上路。");
                return false;
            }

            // 饱食度：出发要求 30；继续时只要求 > 0（路上会消耗）
            int hungerRequired = (currentDay == 0) ? 30 : 1;
            if (_seagull.hunger < hungerRequired)
            {
                Debug.Log(currentDay == 0 ? "饱食度太低，请先喂食再出发。" : "饱食度归零，无法继续。");
                return false;
            }

            if (_seagull.isSick)
            {
                Debug.Log("海鸥生病中，不能迁徙。");
                return false;
            }

            return true;
        }

        private void Update()
        {
            // 按空格键，模拟"继续飞一天"
            if (Input.GetKeyDown(KeyCode.Space))
            {
                FlyOneDay();
            }
        }
    }
}