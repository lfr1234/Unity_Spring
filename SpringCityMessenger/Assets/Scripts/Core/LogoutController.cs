using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SpringCityMessenger.Core
{
    /// <summary>
    /// 挂在 GameManager 上。每次加载家园场景后，自动找到名为 LogoutButton 的按钮并用代码绑定点击，
    /// 避免切到第二个号后引用丢失点不了。
    /// </summary>
    public class LogoutController : MonoBehaviour
    {
        [Tooltip("登录场景名称，切回登录时加载")]
        public string loginSceneName = "LoginScene";

        [Tooltip("有退出登录按钮的场景名（每次加载此场景后会重新绑定按钮）")]
        public string homeSceneName = "HomeScene";

        [Header("可选")]
        [Tooltip("不填则自动查找 GameSaveManager")]
        public GameSaveManager gameSaveManager;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != homeSceneName)
                return;

            // 只保留一个 EventSystem
            var eventSystems = FindObjectsOfType<EventSystem>();
            for (int i = 1; i < eventSystems.Length; i++)
                Destroy(eventSystems[i].gameObject);

            // 按名字找 LogoutButton 并用代码绑定
            var go = GameObject.Find("LogoutButton");
            if (go == null)
            {
                Debug.LogWarning("[退出登录] 场景里没有名为 LogoutButton 的物体。");
                return;
            }

            var btn = go.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogWarning("[退出登录] LogoutButton 物体上没有 Button 组件。");
                return;
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(DoLogout);
            btn.interactable = true;
            if (btn.targetGraphic != null)
                btn.targetGraphic.raycastTarget = true;

            // 把按钮所在 Canvas 置顶，避免被挡住
            var canvas = btn.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 999;
            }
        }

        public void DoLogout()
        {
            if (gameSaveManager == null)
                gameSaveManager = FindObjectOfType<GameSaveManager>();
            if (gameSaveManager != null)
                gameSaveManager.SaveGame();

            if (string.IsNullOrEmpty(loginSceneName))
            {
                Debug.LogWarning("[退出登录] 未设置 loginSceneName。");
                return;
            }

            SceneManager.LoadScene(loginSceneName);
        }
    }
}
