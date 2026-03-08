using UnityEngine;
using SpringCityMessenger.Core;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 迁徙威胁事件：触发时弹出选择（挑战/躲避），玩家选后再应用奖惩。
    /// 按策划案实现事件名称、描述、成功/失败/躲避的数值。
    /// </summary>
    public class MigrationEventSystem : MonoBehaviour
    {
        [Range(0f, 1f)]
        [Tooltip("每天遇到事件的概率，0.7 表示 70%")]
        public float eventChancePerDay = 0.7f;

        [Header("挑战成功奖励（浆果/小鱼）")]
        public int successBerryMin = 5;
        public int successBerryMax = 15;
        public int successFishMin = 0;
        public int successFishMax = 2;

        private MigrationSystem _migrationSystem;
        private SeagullStatus _seagull;
        private CurrencyManager _currency;
        private EventType? _pendingEvent;
        private MigrationEventPopupUI _popup;
        private MigrationMiniGameUI _miniGame;                 // 天气：稳定飞行
        private MigrationPredatorMiniGameUI _predatorMiniGame; // 天敌：躲避猛禽
        private MigrationObstacleMiniGameUI _obstacleMiniGame; // 人为：左右闪避

        public bool IsEventPending => _pendingEvent != null;

        private void Awake()
        {
            _migrationSystem = FindObjectOfType<MigrationSystem>();
            _seagull = FindObjectOfType<SeagullStatus>();
            _currency = FindObjectOfType<CurrencyManager>();
            _popup = FindObjectOfType<MigrationEventPopupUI>(true);
        }

        /// <summary>
        /// 每天飞行完后调用，可能触发事件并弹出选择。
        /// </summary>
        public void TryTriggerEvent()
        {
            if (_migrationSystem == null || _seagull == null) return;

            if (Random.value > eventChancePerDay)
            {
                return;
            }

            int day = _migrationSystem.currentDay;
            string region = GetRegionByDay(day);
            EventType[] candidates = GetCandidatesByRegion(region);
            if (candidates == null || candidates.Length == 0) return;

            EventType evt = candidates[Random.Range(0, candidates.Length)];
            if (evt == EventType.SafeDay)
            {
                GameMessageUI.Show("\u4eca\u5929\u8def\u9014\u9879\u8f9b\u82e6\uff0c\u4f46\u6574\u4f53\u8fd8\u7b97\u9876\u5229\u3002"); // 今天路途艰辛，但整体还算顺利
                return;
            }

            _pendingEvent = evt;
            if (_popup == null) _popup = FindObjectOfType<MigrationEventPopupUI>(true);
            if (_popup != null)
                _popup.Show(this, evt);
            else
            {
                Debug.LogWarning("[迁徙事件] 未找到 MigrationEventPopupUI，直接按挑战掷骰处理");
                bool success = RollChallengeSuccess(evt);
                ApplyResult(evt, isChallenge: true, success);
                _pendingEvent = null;
            }
        }

        /// <summary>
        /// 新流程：在推进天数前，按“将要飞行的那一天 dayToFly”来决定是否触发事件。
        /// 触发则弹窗并返回 true；不触发返回 false。
        /// </summary>
        public bool TryTriggerEventForDay(int dayToFly)
        {
            if (_migrationSystem == null || _seagull == null) return false;
            if (_pendingEvent != null) return true;

            if (Random.value > eventChancePerDay)
                return false;

            string region = GetRegionByDay(dayToFly);
            EventType[] candidates = GetCandidatesByRegion(region);
            if (candidates == null || candidates.Length == 0) return false;

            EventType evt = candidates[Random.Range(0, candidates.Length)];
            if (evt == EventType.SafeDay)
            {
                GameMessageUI.Show("今天路途艰辛，但整体还算顺利。");
                return false;
            }

            _pendingEvent = evt;
            if (_popup == null) _popup = FindObjectOfType<MigrationEventPopupUI>(true);
            if (_popup != null)
            {
                _popup.Show(this, evt);
                return true;
            }

            Debug.LogWarning("[迁徙事件] 未找到 MigrationEventPopupUI，直接按挑战掷骰处理");
            ResolveChallengeByRandom();
            return false;
        }

        /// <summary>
        /// 玩家点击「挑战」后：
        /// - 天敌事件：躲避猛禽小游戏
        /// - 天气事件：稳定飞行小游戏
        /// - 人为事件：左右闪避小游戏
        /// - 其他事件：退回到弹窗测试按钮或随机判定
        /// </summary>
        public void OnChooseChallenge()
        {
            if (!_pendingEvent.HasValue) return;
            var evt = _pendingEvent.Value;

            // 天敌事件：使用“躲避猛禽”小游戏
            if (IsPredatorEvent(evt))
            {
                if (_popup == null) _popup = FindObjectOfType<MigrationEventPopupUI>(true);
                if (_popup != null) _popup.Hide();

                if (_predatorMiniGame == null)
                    _predatorMiniGame = FindObjectOfType<MigrationPredatorMiniGameUI>(true);
                if (_predatorMiniGame != null)
                {
                    _predatorMiniGame.StartMiniGame(this, evt);
                    return;
                }
            }

            // 天气事件：使用“稳定飞行”小游戏
            if (IsWeatherEvent(evt))
            {
                if (_popup == null) _popup = FindObjectOfType<MigrationEventPopupUI>(true);
                if (_popup != null) _popup.Hide();

                if (_miniGame == null)
                    _miniGame = FindObjectOfType<MigrationMiniGameUI>(true);
                if (_miniGame != null)
                {
                    _miniGame.StartMiniGame(this, evt);
                    return;
                }
            }

            // 人为事件：使用“左右闪避”小游戏
            if (IsHumanEvent(evt))
            {
                if (_popup == null) _popup = FindObjectOfType<MigrationEventPopupUI>(true);
                if (_popup != null) _popup.Hide();

                if (_obstacleMiniGame == null)
                    _obstacleMiniGame = FindObjectOfType<MigrationObstacleMiniGameUI>(true);
                if (_obstacleMiniGame != null)
                {
                    _obstacleMiniGame.StartMiniGame(this, evt);
                    return;
                }
            }

            // 其他事件或没找到小游戏面板时，退回到弹窗上的测试按钮
            if (_popup == null) _popup = FindObjectOfType<MigrationEventPopupUI>(true);
            if (_popup != null)
            {
                _popup.ShowChallengeResultButtons();
            }
            else
            {
                ResolveChallengeByRandom();
            }
        }

        /// <summary>
        /// 兜底：当测试 UI 没配好或弹窗丢失时，按“挑战随机判定”直接结算，避免卡死。
        /// </summary>
        public void ResolveChallengeByRandom()
        {
            if (!_pendingEvent.HasValue) return;
            EventType evt = _pendingEvent.Value;
            bool success = RollChallengeSuccess(evt);
            ApplyResult(evt, isChallenge: true, success);
            _pendingEvent = null;
            if (_popup != null) _popup.Hide();
            // 本次点击“继续飞行”的天数在结算后推进
            if (_migrationSystem != null)
                _migrationSystem.CompletePendingDayAdvance();
        }

        /// <summary>
        /// 测试用：玩家点击「挑战成功」后调用。未来由小游戏结果回调替代。
        /// </summary>
        public void OnChooseChallengeSuccess()
        {
            if (!_pendingEvent.HasValue) return;
            EventType evt = _pendingEvent.Value;
            ApplyResult(evt, isChallenge: true, success: true);
            _pendingEvent = null;
            if (_popup != null) _popup.Hide();
            if (_migrationSystem != null)
                _migrationSystem.CompletePendingDayAdvance();
        }

        /// <summary>
        /// 测试用：玩家点击「挑战失败」后调用。未来由小游戏结果回调替代。
        /// </summary>
        public void OnChooseChallengeFail()
        {
            if (!_pendingEvent.HasValue) return;
            EventType evt = _pendingEvent.Value;
            ApplyResult(evt, isChallenge: true, success: false);
            _pendingEvent = null;
            if (_popup != null) _popup.Hide();
            if (_migrationSystem != null)
                _migrationSystem.CompletePendingDayAdvance();
        }

        /// <summary>
        /// 如果有待处理事件但弹窗没显示出来，可调用它把弹窗重新弹出来。
        /// </summary>
        public void TryShowPendingPopup()
        {
            if (!_pendingEvent.HasValue) return;
            if (_popup == null) _popup = FindObjectOfType<MigrationEventPopupUI>(true);
            if (_popup != null)
            {
                _popup.Show(this, _pendingEvent.Value);
            }
            else
            {
                Debug.LogWarning("[迁徙事件] 有待处理事件，但找不到弹窗，已自动按挑战随机判定结算。");
                ResolveChallengeByRandom();
            }
        }

        /// <summary>
        /// 玩家点击「躲避」后调用。
        /// </summary>
        public void OnChooseEvade()
        {
            if (!_pendingEvent.HasValue) return;

            EventType evt = _pendingEvent.Value;
            ApplyResult(evt, isChallenge: false, success: false);
            _pendingEvent = null;
            if (_popup != null) _popup.Hide();
            if (_migrationSystem != null)
                _migrationSystem.CompletePendingDayAdvance();
        }

        private bool RollChallengeSuccess(EventType evt)
        {
            float rate = GetSuccessRate(evt);
            return Random.value < rate;
        }

        private float GetSuccessRate(EventType evt)
        {
            switch (evt)
            {
                case EventType.Goshawk:
                case EventType.Rainstorm:
                case EventType.PowerLines:
                    return 0.6f;
                case EventType.Peregrine:
                case EventType.SteppeEagle:
                case EventType.Snowstorm:
                case EventType.Sandstorm:
                case EventType.SmogFog:
                case EventType.CityHighRise:
                case EventType.WindFarm:
                    return 0.5f;
                case EventType.GoldenEagle:
                case EventType.HeavyRainFog:
                case EventType.PollutedWater:
                    return 0.4f;
                default:
                    return 0.5f;
            }
        }

        /// <summary>
        /// 供弹窗显示：本事件挑战成功率（整数百分比，如 60）。
        /// 判定方式：选「挑战」后掷一次随机数，若随机数 &lt; 成功率则成功，否则失败。
        /// </summary>
        public static int GetSuccessRatePercent(EventType evt)
        {
            switch (evt)
            {
                case EventType.Goshawk:
                case EventType.Rainstorm:
                case EventType.PowerLines:
                    return 60;
                case EventType.Peregrine:
                case EventType.SteppeEagle:
                case EventType.Snowstorm:
                case EventType.Sandstorm:
                case EventType.SmogFog:
                case EventType.CityHighRise:
                case EventType.WindFarm:
                    return 50;
                case EventType.GoldenEagle:
                case EventType.HeavyRainFog:
                case EventType.PollutedWater:
                    return 40;
                default:
                    return 50;
            }
        }

        /// <summary>
        /// 供弹窗显示：选「挑战」时的规则说明（含成功率、成功/失败后果）。
        /// </summary>
        public static string GetChallengeRuleText(EventType evt)
        {
            int rate = GetSuccessRatePercent(evt);
            return "【挑战】按概率掷骰，本事件成功率 " + rate + "%。\n" +
                   "成功：少量扣体力/健康，奖励浆果与小鱼。\n" +
                   "失败：扣较多体力与健康。";
        }

        /// <summary>
        /// 供弹窗显示：选「躲避」时的规则说明。躲避不掷骰，直接按固定代价结算。
        /// </summary>
        public static string GetEvadeRuleText()
        {
            return "【躲避】不掷骰，直接付出固定代价：\n体力 -25，健康 -20，饱食 -15。";
        }

        private void ApplyResult(EventType evt, bool isChallenge, bool success)
        {
            if (isChallenge && success)
            {
                ApplySuccessReward(evt);
                GameMessageUI.Show("\u6311\u6218\u6210\u529f\uff01"); // 挑战成功！
            }
            else if (isChallenge && !success)
            {
                ApplyFailPenalty(evt);
                GameMessageUI.Show("\u6311\u6218\u5931\u8d25\u2026\u2026"); // 挑战失败……
            }
            else
            {
                ApplyEvadeCost(evt);
                GameMessageUI.Show("\u907f\u5f00\u4e86\u5a76\u5371\uff0c\u4f46\u82b1\u8d39\u4e86\u65f6\u95f4\u3002"); // 避开了威胁，但花费了时间
            }
            CheckMigrationFailureAfterEvent();
        }

        /// <summary>
        /// 事件后检查：体力或健康归零则触发迁徙失败回家。
        /// </summary>
        private void CheckMigrationFailureAfterEvent()
        {
            if (_seagull == null || _migrationSystem == null) return;
            if (_seagull.stamina <= 0 || _seagull.health <= 0)
            {
                _migrationSystem.RequestMigrationFailure("\u8fc7\u5ea6\u75b2\u52b3\u6216\u4f24\u4f24\uff0c\u8fd4\u56de\u5bb6\u56ed\u3002"); // 过度疲劳或损伤，返回家园
            }
            else if (_seagull.health < 30)
            {
                _migrationSystem.RequestMigrationFailure("\u5065\u5eb7\u5ea6\u8fc7\u4f4e\uff0c\u65e0\u6cd5\u7ee7\u7eed\u5f92\u5f84\u3002"); // 健康度过低，无法继续迁徙
            }
        }

        /// <summary>
        /// 挑战成功：天数不变，少量扣属性，奖励道具。
        /// </summary>
        private void ApplySuccessReward(EventType evt)
        {
            switch (evt)
            {
                case EventType.Goshawk: _seagull.ChangeStamina(-5); _seagull.ChangeHealth(-3); break;
                case EventType.Peregrine: _seagull.ChangeStamina(-3); _seagull.ChangeHealth(-2); break;
                case EventType.GoldenEagle: _seagull.ChangeStamina(-8); _seagull.ChangeHealth(-5); break;
                case EventType.SteppeEagle: _seagull.ChangeStamina(-4); _seagull.ChangeHealth(-2); break;
                case EventType.Rainstorm: _seagull.ChangeStamina(-2); break;
                case EventType.Snowstorm: _seagull.ChangeStamina(-5); _seagull.ChangeHealth(-3); break;
                case EventType.Sandstorm: _seagull.ChangeStamina(-4); _seagull.ChangeHealth(-2); break;
                case EventType.SmogFog: _seagull.ChangeStamina(-3); _seagull.ChangeHealth(-2); break;
                case EventType.HeavyRainFog: _seagull.ChangeStamina(-4); _seagull.ChangeHealth(-2); break;
                case EventType.PowerLines: _seagull.ChangeStamina(-5); _seagull.ChangeHealth(-3); break;
                case EventType.CityHighRise: _seagull.ChangeStamina(-6); _seagull.ChangeHealth(-4); break;
                case EventType.WindFarm: _seagull.ChangeStamina(-4); _seagull.ChangeHealth(-5); break;
                case EventType.PollutedWater: _seagull.ChangeStamina(-5); _seagull.ChangeHealth(-8); break;
                default: break;
            }
            GiveItemReward(successBerryMin, successBerryMax, successFishMin, successFishMax);
        }

        /// <summary>
        /// 挑战失败：天数不变，扣较多属性。
        /// </summary>
        private void ApplyFailPenalty(EventType evt)
        {
            switch (evt)
            {
                case EventType.Goshawk: _seagull.ChangeStamina(-15); _seagull.ChangeHealth(-10); break;
                case EventType.Peregrine: _seagull.ChangeStamina(-20); _seagull.ChangeHealth(-12); break;
                case EventType.GoldenEagle: _seagull.ChangeStamina(-25); _seagull.ChangeHealth(-15); break;
                case EventType.SteppeEagle: _seagull.ChangeStamina(-18); _seagull.ChangeHealth(-10); break;
                case EventType.Rainstorm: _seagull.ChangeStamina(-12); _seagull.ChangeHealth(-5); break;
                case EventType.Snowstorm: _seagull.ChangeStamina(-15); _seagull.ChangeHealth(-15); break;
                case EventType.Sandstorm: _seagull.ChangeStamina(-18); _seagull.ChangeHealth(-12); break;
                case EventType.SmogFog: _seagull.ChangeStamina(-15); _seagull.ChangeHealth(-8); break;
                case EventType.HeavyRainFog: _seagull.ChangeStamina(-18); _seagull.ChangeHealth(-10); break;
                case EventType.PowerLines: _seagull.ChangeStamina(-20); _seagull.ChangeHealth(-12); break;
                case EventType.CityHighRise: _seagull.ChangeStamina(-22); _seagull.ChangeHealth(-15); break;
                case EventType.WindFarm: _seagull.ChangeStamina(-20); _seagull.ChangeHealth(-18); break;
                case EventType.PollutedWater: _seagull.ChangeStamina(-25); _seagull.ChangeHealth(-25); break;
                default: break;
            }
        }

        /// <summary>
        /// 躲避：天数不变，扣大量属性（相当于挑战失败且更重）。
        /// </summary>
        private void ApplyEvadeCost(EventType evt)
        {
            _seagull.ChangeStamina(-25);
            _seagull.ChangeHealth(-20);
            _seagull.ChangeHunger(-15);
        }

        private void GiveItemReward(int berryMin, int berryMax, int fishMin, int fishMax)
        {
            if (_currency == null) return;
            int berry = Random.Range(berryMin, berryMax + 1);
            int fish = Random.Range(fishMin, fishMax + 1);
            if (berry > 0) _currency.AddBerry(berry);
            if (fish > 0) _currency.AddFish(fish);
        }

        private bool IsWeatherEvent(EventType evt)
        {
            return evt == EventType.Rainstorm
                   || evt == EventType.Snowstorm
                   || evt == EventType.Sandstorm
                   || evt == EventType.SmogFog
                   || evt == EventType.HeavyRainFog;
        }

        private bool IsHumanEvent(EventType evt)
        {
            return evt == EventType.PowerLines
                   || evt == EventType.CityHighRise
                   || evt == EventType.WindFarm
                   || evt == EventType.PollutedWater;
        }

        private bool IsPredatorEvent(EventType evt)
        {
            return evt == EventType.Goshawk
                   || evt == EventType.Peregrine
                   || evt == EventType.GoldenEagle
                   || evt == EventType.SteppeEagle;
        }

        private string GetRegionByDay(int day)
        {
            if (day <= 6) return "\u897f\u4f2f\u5229\u4e9a";
            if (day <= 12) return "\u8499\u53e4\u8349\u539f";
            if (day <= 18) return "\u534e\u5317\u5e73\u539f";
            if (day <= 24) return "\u79e6\u5cad\u5c71\u8109";
            return "\u4e91\u8d35\u9ad8\u539f";
        }

        private EventType[] GetCandidatesByRegion(string region)
        {
            if (region == "\u897f\u4f2f\u5229\u4e9a") return new[] { EventType.Goshawk, EventType.Snowstorm };
            if (region == "\u8499\u53e4\u8349\u539f") return new[] { EventType.SteppeEagle, EventType.Sandstorm, EventType.WindFarm };
            if (region == "\u534e\u5317\u5e73\u539f") return new[] { EventType.PowerLines, EventType.SmogFog };
            if (region == "\u79e6\u5cad\u5c71\u8109") return new[] { EventType.GoldenEagle, EventType.HeavyRainFog };
            if (region == "\u4e91\u8d35\u9ad8\u539f") return new[] { EventType.PollutedWater, EventType.SafeDay };
            return new[] { EventType.SafeDay };
        }

        public static string GetEventName(EventType evt)
        {
            switch (evt)
            {
                case EventType.Goshawk: return "\u82cd\u9e70";
                case EventType.Peregrine: return "\u6e38\u96c0";
                case EventType.GoldenEagle: return "\u91d1\u96d5";
                case EventType.SteppeEagle: return "\u8349\u539f\u96d5";
                case EventType.Rainstorm: return "\u66b4\u98a4\u96e8";
                case EventType.Snowstorm: return "\u66b4\u98a4\u96ea";
                case EventType.Sandstorm: return "\u6c99\u5c18\u66b4";
                case EventType.SmogFog: return "\u96fe\u973e";
                case EventType.HeavyRainFog: return "\u5927\u96e8\u96fe";
                case EventType.PowerLines: return "\u9ad8\u538b\u7535\u7ebf";
                case EventType.CityHighRise: return "\u9ad8\u6977\u5efa\u7b51";
                case EventType.WindFarm: return "\u98ce\u529b\u53d1\u7535";
                case EventType.PollutedWater: return "\u6c61\u67d3\u6c34\u57df";
                case EventType.SafeDay: return "\u5e73\u5b89\u65e5";
                default: return evt.ToString();
            }
        }

        public static string GetEventDesc(EventType evt)
        {
            switch (evt)
            {
                case EventType.Goshawk: return "\u68a6\u89c1\u82cd\u9e70\u4ece\u68ee\u6797\u4e2d\u51fa\u51fb\uff0c\u5feb\u70b9\u51fb\u5b89\u5168\u533a\u57df\u907f\u5f00\uff01";
                case EventType.Peregrine: return "\u6e38\u96c0\u4ece\u9ad8\u7a7a\u4f10\u51b2\u4e0b\u6765\uff0c\u5c3d\u529b\u907f\u5f00\u5b83\u7684\u653b\u51fb\uff01";
                case EventType.GoldenEagle: return "\u91d1\u96d5\u5728\u9ad8\u7a7a\u76f4\u65cb\uff0c\u8d8a\u8fc7\u5b83\u5c31\u80fd\u7ee7\u7eed\u524d\u8fdb\uff01";
                case EventType.SteppeEagle: return "\u8349\u539f\u96d5\u4f4e\u7a7a\u7ffc\u8fc8\uff0c\u5c0f\u5fc3\u8d2f\u5f84\uff01";
                case EventType.Rainstorm: return "\u66b4\u98a4\u96e8\u4e2d\u7eed\u7eed\u70b9\u51fb\u7ef4\u6301\u5e73\u8861\uff01";
                case EventType.Snowstorm: return "\u66b4\u98a4\u96ea\u4f4e\u6e29\uff0c\u7a33\u5b9a\u98de\u884c\u624d\u80fd\u8d8a\u8fc7\uff01";
                case EventType.Sandstorm: return "\u6c99\u5c18\u6ee5\u5929\uff0c\u7ee7\u7eed\u70b9\u51fb\u4fdd\u6301\u5e73\u8861\uff01";
                case EventType.SmogFog: return "\u96fe\u973e\u5bfc\u81f4\u80fd\u89c1\u5ea6\u4f4e\uff0c\u7a33\u5b9a\u98de\u884c\uff01";
                case EventType.HeavyRainFog: return "\u5927\u96e8\u52a0\u96fe\uff0c\u8c28\u614e\u524d\u884c\uff01";
                case EventType.PowerLines: return "\u9ad8\u538b\u7535\u7ebf\u7f51\uff0c\u5de6\u53f3\u95ea\u907f\u5207\u6362\u8dd1\u9053\uff01";
                case EventType.CityHighRise: return "\u9ad8\u6977\u5efa\u7b51\u7fa4\uff0c\u5c0f\u5fc3\u7a7f\u8d8a\uff01";
                case EventType.WindFarm: return "\u98ce\u529b\u53d1\u7535\u573a\u662f\u6f5c\u5728\u5a76\u5371\uff0c\u907f\u5f00\u901a\u9053\uff01";
                case EventType.PollutedWater: return "\u6c61\u67d3\u6c34\u57df\u5f71\u54cd\u98df\u7269\u6765\u6e90\uff0c\u8d8a\u8fc7\u5b83\uff01";
                default: return "\u8d70\u8fc7\u8fd9\u91cc\u5c31\u5b89\u5168\u4e86\u3002";
            }
        }

        public enum EventType
        {
            Goshawk, Peregrine, GoldenEagle, SteppeEagle,
            Rainstorm, Snowstorm, Sandstorm, SmogFog, HeavyRainFog,
            PowerLines, CityHighRise, WindFarm, PollutedWater,
            SafeDay
        }
    }
}
