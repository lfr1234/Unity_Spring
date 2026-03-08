namespace SpringCityMessenger.Core
{
    /// <summary>
    /// 简化版用户与海鸥绑定信息。
    /// 这里先不用真正的账号系统，只做数据结构，方便后面扩展。
    /// </summary>
    [System.Serializable]
    public class UserData
    {
        // 用户信息
        public string userId;
        public string username;
        public string password;
        public string nickname;

        // 海鸥信息（和设计文档保持一致）
        public string seagullId;
        public string seagullName;
    }
}

