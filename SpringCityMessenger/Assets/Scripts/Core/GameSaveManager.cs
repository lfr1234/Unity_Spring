using System.Collections.Generic;
using UnityEngine;
using SpringCityMessenger.Systems;

namespace SpringCityMessenger.Core
{
    /// <summary>
    /// 负责把当前玩家的主要进度保存到本地（PlayerPrefs + JSON），并在启动时读取。
    /// 目前保存内容：
    /// - 海鸥等级、经验、三条属性、生病状态
    /// - 货币：浆果、小鱼
    /// - 家园库存：按 itemId 记录物品及数量
    /// </summary>
    public class GameSaveManager : MonoBehaviour
    {
        [Header("引用")]
        public SeagullStatus seagullStatus;
        public CurrencyManager currencyManager;
        public HomeInventory homeInventory;
        public BackpackSystem backpackSystem;

        [System.Serializable]
        private class InventoryItemSave
        {
            public string itemId;
            public string displayName;
            public FeedingSystem.FoodQuality quality;
            public int count;
        }

        [System.Serializable]
        private class SaveData
        {
            public int level;
            public int exp;
            public int stamina;
            public int health;
            public int hunger;
            public bool isSick;

            public int berryCount;
            public int fishCount;

            public List<InventoryItemSave> backpackItems = new List<InventoryItemSave>();

            public List<InventoryItemSave> inventoryItems = new List<InventoryItemSave>();
        }

        private void Awake()
        {
            if (seagullStatus == null)
                seagullStatus = FindObjectOfType<SeagullStatus>();

            if (currencyManager == null)
                currencyManager = FindObjectOfType<CurrencyManager>();

            if (homeInventory == null)
                homeInventory = FindObjectOfType<HomeInventory>();

            if (backpackSystem == null)
                backpackSystem = FindObjectOfType<BackpackSystem>();

            LoadGame();
        }

        private string GetSaveKey()
        {
            // 存档按“用户名”区分，每个账号一份；
            // 使用 v2 前缀，避免旧版本用 userId 时多个账号共用一份存档的历史问题。
            string userPart = "default";
            string lastUsername = PlayerPrefs.GetString("SCM_LastUsername", "");

            if (!string.IsNullOrEmpty(lastUsername))
            {
                userPart = lastUsername;
            }
            else if (GameManager.Instance != null && GameManager.Instance.currentUser != null
                     && !string.IsNullOrEmpty(GameManager.Instance.currentUser.username))
            {
                userPart = GameManager.Instance.currentUser.username;
            }

            return $"SCM_GameSave_v2_{userPart}";
        }

        public void SaveGame()
        {
            // 确保在调用保存前有最新的引用（有时按钮连到的对象在场景切换后丢了引用）
            if (seagullStatus == null)
                seagullStatus = FindObjectOfType<SeagullStatus>();
            if (currencyManager == null)
                currencyManager = FindObjectOfType<CurrencyManager>();
            if (backpackSystem == null)
                backpackSystem = FindObjectOfType<BackpackSystem>();

            if (seagullStatus == null || currencyManager == null)
            {
                Debug.LogWarning("[存档] 缺少海鸥或货币引用，无法保存。");
                return;
            }

            string key = GetSaveKey();

            // 先尝试读取旧存档，这样在迁徙场景没有 HomeInventory 时也能保留原来的背包数据
            SaveData data = null;
            if (PlayerPrefs.HasKey(key))
            {
                string oldJson = PlayerPrefs.GetString(key, "");
                if (!string.IsNullOrEmpty(oldJson))
                {
                    try
                    {
                        data = JsonUtility.FromJson<SaveData>(oldJson);
                    }
                    catch
                    {
                        data = null;
                    }
                }
            }
            if (data == null)
                data = new SaveData();

            // 更新海鸥与货币部分
            data.level = seagullStatus.level;
            data.exp = seagullStatus.exp;
            data.stamina = seagullStatus.stamina;
            data.health = seagullStatus.health;
            data.hunger = seagullStatus.hunger;
            data.isSick = seagullStatus.isSick;
            data.berryCount = currencyManager.BerryCount;
            data.fishCount = currencyManager.FishCount;

            if (backpackSystem != null)
            {
                data.backpackItems.Clear();
                foreach (var item in backpackSystem.items)
                {
                    if (item == null || string.IsNullOrEmpty(item.itemId) || item.count <= 0) continue;
                    data.backpackItems.Add(new InventoryItemSave
                    {
                        itemId = item.itemId,
                        displayName = item.displayName,
                        quality = item.quality,
                        count = item.count
                    });
                }
            }

            // 只有在家园场景、有 HomeInventory 时才更新背包明细，避免在迁徙场景把背包清空
            if (homeInventory != null)
            {
                data.inventoryItems.Clear();
                foreach (var item in homeInventory.items)
                {
                    if (item == null || string.IsNullOrEmpty(item.itemId) || item.count <= 0) continue;
                    data.inventoryItems.Add(new InventoryItemSave
                    {
                        itemId = item.itemId,
                        displayName = item.displayName,
                        quality = item.quality,
                        count = item.count
                    });
                }
            }

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();

            Debug.Log($"[存档] 进度已保存。Key = {key}");
            GameMessageUI.Show("进度已保存。");
        }

        public void LoadGame()
        {
            string key = GetSaveKey();
            if (!PlayerPrefs.HasKey(key))
            {
                Debug.Log($"[存档] 未找到存档 Key = {key}，使用默认初始数据。");

                // 关键：如果是新账号第一次进来，这里要清空全局单例里的数据，
                // 避免沿用上一个账号的行囊 / 家园背包。
                // 行囊：优先用单例 Instance，兜底用 FindObjectOfType，避免 Awake 顺序导致引用为空
                var backpack = BackpackSystem.Instance ?? FindObjectOfType<BackpackSystem>();
                if (backpack != null)
                    backpack.ClearAll();

                // 家园背包：直接现场查找一遍，确保拿到当前场景里的对象
                var inventory = homeInventory != null ? homeInventory : FindObjectOfType<HomeInventory>();
                if (inventory != null)
                {
                    inventory.items.Clear();
                    inventory.junkFoodCount = 0;
                    inventory.normalFoodCount = 0;
                    inventory.goodFoodCount = 0;
                    inventory.premiumFoodCount = 0;
                }

                // 货币：新账号第一次进来时，也要把全局货币重置为 0，避免沿用上一个账号的数值
                var currency = currencyManager != null ? currencyManager : FindObjectOfType<CurrencyManager>();
                if (currency != null)
                {
                    currency.BerryCount = 0;
                    currency.FishCount = 0;
                }

                return;
            }

            string json = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[存档] 存档内容为空。");
                return;
            }

            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                Debug.LogWarning("[存档] 解析存档失败。");
                return;
            }

            if (seagullStatus != null)
            {
                seagullStatus.level = data.level;
                seagullStatus.exp = data.exp;
                seagullStatus.stamina = data.stamina;
                seagullStatus.health = data.health;
                seagullStatus.hunger = data.hunger;
                seagullStatus.isSick = data.isSick;
            }

            if (currencyManager != null)
            {
                currencyManager.BerryCount = data.berryCount;
                currencyManager.FishCount = data.fishCount;
            }

            if (backpackSystem == null)
                backpackSystem = FindObjectOfType<BackpackSystem>();
            if (backpackSystem != null && data.backpackItems != null)
            {
                backpackSystem.items.Clear();
                foreach (var save in data.backpackItems)
                {
                    if (save == null || string.IsNullOrEmpty(save.itemId) || save.count <= 0) continue;
                    backpackSystem.items.Add(new BackpackSystem.HaversackItem
                    {
                        itemId = save.itemId,
                        displayName = save.displayName,
                        quality = save.quality,
                        count = save.count
                    });
                }
            }

            if (homeInventory != null)
            {
                homeInventory.items.Clear();
                foreach (var itemSave in data.inventoryItems)
                {
                    var item = new HomeInventory.InventoryItem
                    {
                        itemId = itemSave.itemId,
                        displayName = itemSave.displayName,
                        quality = itemSave.quality,
                        count = itemSave.count
                    };
                    homeInventory.items.Add(item);
                }

                // 重新计算按品质汇总的数量
                homeInventory.junkFoodCount = 0;
                homeInventory.normalFoodCount = 0;
                homeInventory.goodFoodCount = 0;
                homeInventory.premiumFoodCount = 0;
                foreach (var item in homeInventory.items)
                {
                    switch (item.quality)
                    {
                        case FeedingSystem.FoodQuality.Junk:
                            homeInventory.junkFoodCount += item.count;
                            break;
                        case FeedingSystem.FoodQuality.Normal:
                            homeInventory.normalFoodCount += item.count;
                            break;
                        case FeedingSystem.FoodQuality.Good:
                            homeInventory.goodFoodCount += item.count;
                            break;
                        case FeedingSystem.FoodQuality.Premium:
                            homeInventory.premiumFoodCount += item.count;
                            break;
                    }
                }
            }

            Debug.Log($"[存档] 进度已读取。Key = {key}");
        }


        private void OnApplicationQuit()
        {
            SaveGame();
        }
    }
}

