using UnityEngine;

using SpringCityMessenger.Core;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 家园资源点（浆果丛 / 小溪）。
    /// 按固定时间产出一定数量的资源，直到达到上限。
    /// </summary>
    public class ResourceNode : MonoBehaviour
    {
        public enum ResourceType
        {
            Berry,
            Fish
        }

        [Header("资源点类型与参数")]
        public ResourceType resourceType = ResourceType.Berry;

        [Tooltip("产出间隔（分钟）")]
        public float produceIntervalMinutes = 30f;

        [Tooltip("每次产出最小数量")]
        public int minAmountPerTick = 5;

        [Tooltip("每次产出最大数量")]
        public int maxAmountPerTick = 10;

        [Tooltip("资源最大堆积上限（超过则不再产出）")]
        public int maxStorage = 50;

        [Header("当前已堆积数量")]
        public int storedAmount = 0;

        private float _timer;
        private CurrencyManager _currency;

        private void Awake()
        {
            _currency = FindObjectOfType<CurrencyManager>();
        }

        private void Update()
        {
            if (_currency == null) return;

            _timer += Time.deltaTime;
            var intervalSeconds = produceIntervalMinutes * 60f;

            if (_timer >= intervalSeconds)
            {
                _timer -= intervalSeconds;
                ProduceOnce();
            }
        }

        private void ProduceOnce()
        {
            if (storedAmount >= maxStorage) return;

            int amount = Random.Range(minAmountPerTick, maxAmountPerTick + 1);
            storedAmount = Mathf.Clamp(storedAmount + amount, 0, maxStorage);
        }

        /// <summary>
        /// 由玩家点击采集时调用，把存量加到全局货币里。
        /// </summary>
        public void Collect()
        {
            if (_currency == null) return;
            if (storedAmount <= 0) return;

            switch (resourceType)
            {
                case ResourceType.Berry:
                    _currency.AddBerry(storedAmount);
                    break;
                case ResourceType.Fish:
                    _currency.AddFish(storedAmount);
                    break;
            }

            storedAmount = 0;
        }
    }
}

