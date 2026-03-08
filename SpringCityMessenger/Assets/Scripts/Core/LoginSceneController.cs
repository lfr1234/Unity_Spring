using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace SpringCityMessenger.Core
{
    /// <summary>
    /// 登录场景用的小控制器。
    /// 把三个输入框（用户名 / 密码 / 昵称）和两个按钮（注册 / 登录）串起来。
    /// </summary>
    public class LoginSceneController : MonoBehaviour
    {
        [Header("UI 引用")]
        public TMP_InputField usernameInput;
        public TMP_InputField passwordInput;
        public TMP_InputField nicknameInput;

        [Header("系统引用")]
        public LoginManager loginManager;

        [Tooltip("登录成功后要进入的主场景名称，例如 HomeScene")]
        public string homeSceneName = "HomeScene";

        private void Awake()
        {
            if (loginManager == null)
            {
                loginManager = FindObjectOfType<LoginManager>();
            }
        }

        public void OnClickRegister()
        {
            if (loginManager == null)
            {
                Debug.LogWarning("[LoginScene] 未找到 LoginManager，无法注册。");
                return;
            }

            string username = usernameInput != null ? usernameInput.text : "";
            string password = passwordInput != null ? passwordInput.text : "";
            string nickname = nicknameInput != null ? nicknameInput.text : "";

            loginManager.Register(username, password, nickname);
        }

        public void OnClickLogin()
        {
            if (loginManager == null)
            {
                Debug.LogWarning("[LoginScene] 未找到 LoginManager，无法登录。");
                return;
            }

            string username = usernameInput != null ? usernameInput.text : "";
            string password = passwordInput != null ? passwordInput.text : "";

            loginManager.Login(username, password);

            if (loginManager.isLoggedIn)
            {
                if (!string.IsNullOrEmpty(homeSceneName))
                {
                    SceneManager.LoadScene(homeSceneName);
                }
                else
                {
                    Debug.LogWarning("[LoginScene] homeSceneName 未设置，登录成功但无法切场景。");
                }
            }
        }
    }
}

