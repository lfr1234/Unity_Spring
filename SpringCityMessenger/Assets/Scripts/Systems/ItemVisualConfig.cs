using System.Collections.Generic;
using UnityEngine;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 物品外观与描述配置：
    /// - 每个物品一条，按 itemId 匹配；
    /// - 配置图标、简介、使用效果描述等。
    ///
    /// 把本脚本挂在某个常驻物体上（比如 BackpackWindow 或一个 GameSystems 节点），
    /// 在 Inspector 里把所有物品配置好即可。
    /// </summary>
    public class ItemVisualConfig : MonoBehaviour
    {
        [System.Serializable]
        public class ItemEntry
        {
            public string itemId;
            public Sprite icon;
            [TextArea]
            public string description;
            [TextArea]
            public string effectDescription;
        }

        public List<ItemEntry> items = new List<ItemEntry>();

        private static ItemVisualConfig _instance;

        private void Awake()
        {
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public static ItemEntry Get(string itemId)
        {
            if (_instance == null || string.IsNullOrEmpty(itemId)) return null;
            return _instance.items.Find(e => e != null && e.itemId == itemId);
        }
    }
}

