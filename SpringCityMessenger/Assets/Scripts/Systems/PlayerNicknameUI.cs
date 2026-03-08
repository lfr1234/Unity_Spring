using UnityEngine;
using TMPro;
using SpringCityMessenger.Core;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 在 UI 上显示当前登录用户的昵称。
    /// - 显示 GameManager.currentUser.nickname（如果有的话）；
    /// - 如果还没初始化 GameManager，就退回到 LoginManager.currentNickname；
    /// - 再不行就从本地保存的用户列表里，按最近登录的账号找到对应昵称；
    /// - 最后的兜底才是 PlayerPrefs 里上一次登录的用户名。
    /// </summary>
    public class PlayerNicknameUI : MonoBehaviour
    {
        [Header("UI 引用")]
        [Tooltip("用于显示昵称的 TextMeshPro 文本")]
        public TextMeshProUGUI nicknameText;

        [Header("显示格式")]
        [Tooltip("前缀，例如 \"信使：\"，留空则只显示昵称")]
        public string prefix = "";

        private void Start()
        {
            Refresh();
        }

        /// <summary>
        /// 重新从当前账号数据里读取昵称并刷新显示。
        /// </summary>
        public void Refresh()
        {
            if (nicknameText == null) return;

            string nameToShow = GetCurrentNickname();
            if (string.IsNullOrEmpty(nameToShow))
            {
                nicknameText.text = "";
                return;
            }

            nicknameText.text = string.IsNullOrEmpty(prefix)
                ? nameToShow
                : prefix + nameToShow;
        }

        private string GetCurrentNickname()
        {
            // 简化版：只信 GameManager 里的当前用户，避免被历史缓存干扰。
            // 要正确显示昵称，请务必从登录场景进入游戏，而不是直接 Play HomeScene。
            // 1. 优先从 GameManager.currentUser 取（登录成功后会写入这里，场景之间也会保留）
            if (GameManager.Instance != null && GameManager.Instance.currentUser != null)
            {
                var u = GameManager.Instance.currentUser;
                if (!string.IsNullOrEmpty(u.nickname))
                    return u.nickname;
                if (!string.IsNullOrEmpty(u.username))
                    return u.username;
            }

            // 2. 其次从 LoginManager.currentNickname 取（刚登录完、还在登录场景时用这个）
            var login = Object.FindObjectOfType<LoginManager>();
            if (login != null)
            {
                if (!string.IsNullOrEmpty(login.currentNickname))
                    return login.currentNickname;
                if (!string.IsNullOrEmpty(login.currentUsername))
                    return login.currentUsername;
            }

            // 3. 没有登录信息就不显示（Play HomeScene 直接启动的调试模式）
            return "";
        }
    }
}

