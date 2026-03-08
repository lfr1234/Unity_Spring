using UnityEngine;

namespace SpringCityMessenger.Core
{
    /// <summary>
    /// 简化版登录管理器：
    /// - 不做真正的服务器和数据库
    /// - 只是在本地保存一个当前用户和密码
    /// - 提供给 UI 按钮调用的 Register 和 Login 方法
    /// </summary>
    public class LoginManager : MonoBehaviour
    {
        private const string UserProfilesKey = "SCM_UserProfiles_v1";

        [Header("调试显示（运行时可见）")]
        public string currentUsername;
        public string currentNickname;
        public bool isLoggedIn;

        [System.Serializable]
        private class UserProfileList
        {
            public System.Collections.Generic.List<UserData> users = new System.Collections.Generic.List<UserData>();
        }

        private UserProfileList _profiles;

        private void Awake()
        {
            // 确保 GameManager 单例在登录场景就已经存在，
            // 这样第一次登录成功时就能把 currentUser 写进去，后续场景里才能正确显示昵称等信息。
            if (GameManager.Instance == null)
            {
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
                // 在这个全局对象上同时挂上 CurrencyManager 和 LogoutController，
                // 保证进入 HomeScene 后商店/采集和“退出登录”按钮都能正常工作。
                go.AddComponent<CurrencyManager>();
                go.AddComponent<LogoutController>();
            }
            else
            {
                // 如果已经有 GameManager 单例，确保它身上一定有 CurrencyManager 组件
                var existingGo = GameManager.Instance.gameObject;
                if (existingGo.GetComponent<CurrencyManager>() == null)
                {
                    existingGo.AddComponent<CurrencyManager>();
                }

                 // 同样确保有 LogoutController，可以在加载 HomeScene 时自动绑定 LogoutButton
                 if (existingGo.GetComponent<LogoutController>() == null)
                 {
                     existingGo.AddComponent<LogoutController>();
                 }
            }

            LoadProfiles();
        }

        private void LoadProfiles()
        {
            var json = PlayerPrefs.GetString(UserProfilesKey, "");
            if (string.IsNullOrEmpty(json))
            {
                _profiles = new UserProfileList();
                return;
            }

            _profiles = JsonUtility.FromJson<UserProfileList>(json);
            if (_profiles == null)
            {
                _profiles = new UserProfileList();
            }
        }

        private void SaveProfiles()
        {
            if (_profiles == null)
            {
                _profiles = new UserProfileList();
            }
            var json = JsonUtility.ToJson(_profiles);
            PlayerPrefs.SetString(UserProfilesKey, json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 供 UI 输入框调用的注册方法。
        /// 在实际项目中，你可以在按钮的 OnClick 里，把 InputField 的文本传进来。
        /// nickname 对应策划案里的“昵称”，用于展示。
        /// </summary>
        public void Register(string username, string password, string nickname)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Debug.LogWarning("注册失败：用户名或密码为空。");
                return;
            }

            if (username.Length < 3 || username.Length > 20)
            {
                Debug.LogWarning("注册失败：用户名长度需为 3-20 个字符。");
                return;
            }

            if (password.Length < 6)
            {
                Debug.LogWarning("注册失败：密码至少 6 位。");
                return;
            }

            if (string.IsNullOrEmpty(nickname))
            {
                nickname = username;
            }

            // 检查是否有重名用户
            if (_profiles.users.Exists(u => u.username == username))
            {
                Debug.LogWarning("注册失败：该用户名已存在。");
                return;
            }

            // 生成本地 userId / seagullId
            var user = new UserData
            {
                userId = System.Guid.NewGuid().ToString("N"),
                username = username,
                password = password,
                nickname = nickname,
                seagullId = System.Guid.NewGuid().ToString("N"),
                seagullName = "欧欧"
            };

            _profiles.users.Add(user);
            SaveProfiles();

            Debug.Log($"注册成功：{username}（昵称：{nickname}）");
        }

        /// <summary>
        /// 供 UI 输入框调用的登录方法。
        /// 成功后会在 GameManager 中创建一个 UserData 和默认海鸥。
        /// </summary>
        public void Login(string username, string password)
        {
            if (_profiles == null)
            {
                LoadProfiles();
            }

            if (_profiles.users.Count == 0)
            {
                Debug.LogWarning("尚未注册账号，请先注册。");
                return;
            }

            var savedUser = _profiles.users.Find(u => u.username == username);
            if (savedUser == null)
            {
                Debug.LogWarning("登录失败：用户不存在。");
                return;
            }

            if (password == savedUser.password)
            {
                isLoggedIn = true;
                currentUsername = savedUser.username;
                currentNickname = savedUser.nickname;
                Debug.Log($"登录成功：{savedUser.username}（昵称：{savedUser.nickname}）");

                // 记录本次登录的用户，用于 GameSaveManager 区分不同账号的存档
                PlayerPrefs.SetString("SCM_LastUserId", savedUser.userId);
                PlayerPrefs.SetString("SCM_LastUsername", savedUser.username);
                PlayerPrefs.Save();

                // 初始化 GameManager 中的用户数据
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.currentUser = savedUser;
                }
            }
            else
            {
                Debug.LogWarning("登录失败：用户名或密码错误。");
            }
        }
    }
}

