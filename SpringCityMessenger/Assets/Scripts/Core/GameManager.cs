using UnityEngine;

namespace SpringCityMessenger.Core
{
    /// <summary>
    /// 全局游戏管理器（单例）。
    /// 负责保存当前玩家、海鸥数据，以及场景之间共享的状态。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("当前用户与海鸥状态")]
        public UserData currentUser;
        public SeagullStatus seagullStatus;

        [Header("全局资源")]
        public int berryCount;   // 浆果数量
        public int fishCount;    // 小鱼数量

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}

