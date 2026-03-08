using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpringCityMessenger.Systems
{
    /// <summary>
    /// 背包物品的悬浮提示面板。
    /// 用法：InventorySlotUI 在鼠标进入/离开时调用 Show/Hide。
    /// 如果没有预制，会在首次调用时自动创建，保证描述框一定能用。
    /// </summary>
    public class ItemTooltipUI : MonoBehaviour
    {
        public TextMeshProUGUI tooltipText;

        private static ItemTooltipUI _instance;
        private static bool _initialized;
        private static float _lastDebugLogTime;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            EnsureInitialized();
        }

        /// <summary>
        /// 确保 tooltip 已初始化（文本引用 + 移到常驻 Canvas）。
        /// 场景里 ItemTooltip 若一开始是未激活的，Awake 不会跑，需要首次 Show 时补调用。
        /// </summary>
        private void EnsureInitialized()
        {
            if (_initialized) return;
            if (_instance == null) return;

            if (_instance.tooltipText == null)
                _instance.tooltipText = _instance.GetComponentInChildren<TextMeshProUGUI>(true);
            if (_instance.tooltipText != null)
                _instance.tooltipText.text = string.Empty;
            _instance.gameObject.SetActive(false);

            // 常驻：移到独立 Canvas 并 DontDestroyOnLoad，换场景后描述框仍可用
            if (_instance.transform.root.name != "ItemTooltipCanvas")
            {
                var root = new GameObject("ItemTooltipCanvas");
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 32767;
                root.AddComponent<CanvasScaler>();
                root.AddComponent<GraphicRaycaster>();
                _instance.transform.SetParent(root.transform, true);
                DontDestroyOnLoad(root);
            }
            _initialized = true;
        }

        /// <summary>
        /// 场景里没有任何 ItemTooltip 时，运行时创建一个。
        /// </summary>
        private static ItemTooltipUI CreateDefaultTooltip()
        {
            var root = new GameObject("ItemTooltipCanvas");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32767;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("ItemTooltip");
            panel.transform.SetParent(root.transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300, 80);
            rect.anchoredPosition = Vector2.zero;

            var img = panel.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.14f, 0.98f);
            img.raycastTarget = false;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(panel.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 14;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            var tt = panel.AddComponent<ItemTooltipUI>();
            tt.tooltipText = tmp;
            _instance = tt;
            _initialized = true;
            DontDestroyOnLoad(root);
            return tt;
        }

        private static void PositionNearMouse(ItemTooltipUI tooltip)
        {
            if (tooltip == null) return;
            var rect = tooltip.GetComponent<RectTransform>();
            if (rect == null) return;

            var canvas = tooltip.GetComponentInParent<Canvas>();
            var canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            if (canvasRect == null) return;

            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, cam, out var localPoint))
                return;

            // 统一用左上角锚点坐标，避免预制体 anchor=右上角导致 anchoredPosition 乱飞
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);

            var canvasSize = canvasRect.rect.size;
            var canvasOrigin = new Vector2(-canvasSize.x * canvasRect.pivot.x, canvasSize.y * (1f - canvasRect.pivot.y));
            var pos = new Vector2(localPoint.x, localPoint.y) - canvasOrigin;

            // 偏移到鼠标右下侧一点（避免挡住光标）
            pos += new Vector2(16f, -16f);

            // 简单夹在屏幕内
            var w = rect.rect.width;
            var h = rect.rect.height;
            pos.x = Mathf.Clamp(pos.x, 0f, Mathf.Max(0f, canvasSize.x - w));
            pos.y = Mathf.Clamp(pos.y, -Mathf.Max(0f, canvasSize.y - h), 0f);

            rect.anchoredPosition = pos;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _initialized = false;
            }
        }

        public static void Show(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (_instance == null) _instance = FindObjectOfType<ItemTooltipUI>(true);
            if (_instance == null) _instance = CreateDefaultTooltip();
            if (_instance == null) return;

            _instance.EnsureInitialized();
            if (_instance.tooltipText == null) _instance.tooltipText = _instance.GetComponentInChildren<TextMeshProUGUI>(true);
            if (_instance.tooltipText == null) return;
            _instance.tooltipText.text = text;
            var bg = _instance.GetComponent<Image>();
            if (bg != null) bg.color = new Color(0.1f, 0.1f, 0.14f, 0.98f);

            // 确保始终挂在自己的 ItemTooltipCanvas（该 Canvas 排序层级最高）
            if (_instance.transform.root.name != "ItemTooltipCanvas")
            {
                var roots = Object.FindObjectsOfType<Canvas>();
                GameObject tooltipRoot = null;
                foreach (var c in roots)
                {
                    if (c.gameObject.name == "ItemTooltipCanvas")
                    {
                        tooltipRoot = c.gameObject;
                        break;
                    }
                }
                if (tooltipRoot == null)
                {
                    // 如果不在场景里（极端情况），重新创建一个
                    var root = new GameObject("ItemTooltipCanvas");
                    var canvas = root.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.overrideSorting = true;
                    canvas.sortingOrder = 32767;
                    root.AddComponent<CanvasScaler>();
                    root.AddComponent<GraphicRaycaster>();
                    tooltipRoot = root;
                    Object.DontDestroyOnLoad(root);
                }
                _instance.transform.SetParent(tooltipRoot.transform, true);
            }
            _instance.transform.SetAsLastSibling();

            _instance.gameObject.SetActive(true);

            PositionNearMouse(_instance);

            if (Time.unscaledTime - _lastDebugLogTime > 1f)
            {
                _lastDebugLogTime = Time.unscaledTime;
                var r = _instance.GetComponent<RectTransform>();
                var posStr = r != null ? r.anchoredPosition.ToString() : "(no rect)";
                Debug.Log("[ItemTooltipUI] Show ok. active=" + _instance.gameObject.activeInHierarchy + " textLen=" + text.Length + " pos=" + posStr);
            }
        }

        public static void Hide()
        {
            if (_instance == null) _instance = FindObjectOfType<ItemTooltipUI>(true);
            if (_instance == null) return;
            _instance.gameObject.SetActive(false);
        }
    }
}

