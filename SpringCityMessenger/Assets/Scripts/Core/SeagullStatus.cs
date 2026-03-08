using UnityEngine;

namespace SpringCityMessenger.Core
{
    /// <summary>
    /// 海鸥核心属性与等级成长。
    /// 数值直接对应毕设文档中的设计表。
    /// </summary>
    public class SeagullStatus : MonoBehaviour
    {
        [Header("基础信息")]
        public string seagullName = "欧欧";

        [Header("等级与经验")]
        [Tooltip("当前等级（1-4）")]
        [Range(1, 4)]
        public int level = 1;

        [Tooltip("当前累计经验值")]
        public int exp = 0;

        [Header("当前属性（0-100）")]
        [Range(0, 100)] public int stamina = 50;
        [Range(0, 100)] public int health = 80;
        [Range(0, 100)] public int hunger = 50;

        [Header("属性衰减参数")]
        [Tooltip("饱食度每分钟衰减值")]
        public float hungerDecayPerMinute = 1f;

        [Tooltip("当饱食度<30时，体力每分钟衰减值")]
        public float staminaDecayWhenHungryPerMinute = 0.5f;

        [Tooltip("生病时健康度每分钟衰减值")]
        public float healthDecayWhenSickPerMinute = 0.2f;

        // 简化的生病状态标记（连续喂垃圾食品等条件逻辑之后再接）
        [Header("状态标记")]
        public bool isSick = false;

        private float _hungerTimer;
        private float _staminaTimer;
        private float _healthTimer;

        private void Update()
        {
            UpdateHunger();
            UpdateStamina();
            UpdateHealth();
        }

        #region 属性衰减逻辑

        private void Start()
        {
            // 启动时根据经验同步一次等级，但不重置当前属性
            CheckLevelUp(resetStats: false);
        }

        private void UpdateHunger()
        {
            _hungerTimer += Time.deltaTime;
            // 每 60 秒按设计减少一次饱食度
            if (_hungerTimer >= 60f)
            {
                _hungerTimer -= 60f;
                ChangeHunger(-(int)hungerDecayPerMinute);
            }
        }

        private void UpdateStamina()
        {
            if (hunger < 30)
            {
                _staminaTimer += Time.deltaTime;
                if (_staminaTimer >= 60f)
                {
                    _staminaTimer -= 60f;
                    ChangeStamina(-(int)staminaDecayWhenHungryPerMinute);
                }
            }
            else
            {
                _staminaTimer = 0f;
            }
        }

        private void UpdateHealth()
        {
            if (isSick)
            {
                _healthTimer += Time.deltaTime;
                if (_healthTimer >= 60f)
                {
                    _healthTimer -= 60f;
                    ChangeHealth(-(int)healthDecayWhenSickPerMinute);
                }
            }
            else
            {
                _healthTimer = 0f;
            }
        }

        #endregion

        #region 对外接口（投喂、加经验等）

        public void ChangeStamina(int delta)
        {
            stamina = Mathf.Clamp(stamina + delta, 0, 100);
        }

        public void ChangeHealth(int delta)
        {
            health = Mathf.Clamp(health + delta, 0, 100);
        }

        public void ChangeHunger(int delta)
        {
            hunger = Mathf.Clamp(hunger + delta, 0, 100);
        }

        public void AddExp(int amount)
        {
            if (amount <= 0) return;

            exp += amount;
            CheckLevelUp();
        }

        private void CheckLevelUp(bool resetStats = true)
        {
            // 只在经验达到对应门槛时“升一级”，不降级
            if (exp >= 600 && level < 4)
            {
                level = 4;
                if (resetStats)
                    ApplyLevelInitialStats();
            }
            else if (exp >= 300 && level < 3)
            {
                level = 3;
                if (resetStats)
                    ApplyLevelInitialStats();
            }
            else if (exp >= 100 && level < 2)
            {
                level = 2;
                if (resetStats)
                    ApplyLevelInitialStats();
            }
            // exp 不够下一级时，不动 level
        }

        /// <summary>
        /// 升级后按照文档中表格设置初始属性（只在升级时重置起点）。
        /// </summary>
        private void ApplyLevelInitialStats()
        {
            int targetStamina = stamina;
            int targetHealth = health;
            int targetHunger = hunger;

            switch (level)
            {
                case 1:
                    targetStamina = 50;
                    targetHealth = 80;
                    targetHunger = 50;
                    break;
                case 2:
                    targetStamina = 70;
                    targetHealth = 90;
                    targetHunger = 50;
                    break;
                case 3:
                    targetStamina = 85;
                    targetHealth = 95;
                    targetHunger = 50;
                    break;
                case 4:
                    targetStamina = 100;
                    targetHealth = 100;
                    targetHunger = 50;
                    break;
            }

            // 只抬“最低值”，不会把已经更高的数值压下去
            stamina = Mathf.Max(stamina, targetStamina);
            health = Mathf.Max(health, targetHealth);
            hunger = Mathf.Max(hunger, targetHunger);
        }

        #endregion
    }
}

