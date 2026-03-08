using UnityEngine;

namespace SpringCityMessenger.Core
{
    /// <summary>
    /// 简单的货币管理：管理全局的浆果和小鱼数量。
    /// 可以挂在 GameManager 所在的同一个对象上，或者任何常驻对象上。
    /// </summary>
    public class CurrencyManager : MonoBehaviour
    {
        public int BerryCount
        {
            get => GameManager.Instance != null ? GameManager.Instance.berryCount : 0;
            set
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.berryCount = Mathf.Max(0, value);
                }
            }
        }

        public int FishCount
        {
            get => GameManager.Instance != null ? GameManager.Instance.fishCount : 0;
            set
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.fishCount = Mathf.Max(0, value);
                }
            }
        }

        public void AddBerry(int amount)
        {
            BerryCount += amount;
        }

        public bool SpendBerry(int amount)
        {
            if (BerryCount < amount) return false;
            BerryCount -= amount;
            return true;
        }

        public void AddFish(int amount)
        {
            FishCount += amount;
        }

        public bool SpendFish(int amount)
        {
            if (FishCount < amount) return false;
            FishCount -= amount;
            return true;
        }
    }
}

