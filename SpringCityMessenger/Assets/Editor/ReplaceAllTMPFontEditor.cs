using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// 一键把当前场景里所有 TextMeshProUGUI 的字体换成指定 Font Asset。
/// 用法：菜单 Tools → 一键替换 TMP 字体，在窗口里拖入字体资源后点按钮。
/// </summary>
public class ReplaceAllTMPFontEditor : EditorWindow
{
    private TMP_FontAsset _targetFont;
    private Vector2 _scroll;

    [MenuItem("Tools/一键替换 TMP 字体")]
    public static void ShowWindow()
    {
        var w = GetWindow<ReplaceAllTMPFontEditor>("替换 TMP 字体");
        w.minSize = new Vector2(320, 120);
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.HelpBox(
            "将下面指定的 Font Asset 应用到当前场景中所有 TextMeshProUGUI。\n" +
            "请先在 Project 里创建好带中文的 TMP Font Asset（如用黑体生成）。",
            MessageType.Info);

        EditorGUILayout.Space(4);
        _targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField("目标 Font Asset", _targetFont, typeof(TMP_FontAsset), false);

        EditorGUILayout.Space(8);

        if (_targetFont == null)
        {
            EditorGUILayout.HelpBox("请先指定一个 TMP Font Asset。", MessageType.Warning);
        }
        else if (GUILayout.Button("应用到当前场景所有 TMP 文本", GUILayout.Height(32)))
        {
            ApplyToScene();
        }

        EditorGUILayout.EndScrollView();
    }

    private void ApplyToScene()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        int count = 0;
        foreach (var root in roots)
        {
            var tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                if (tmp.font != _targetFont)
                {
                    Undo.RecordObject(tmp, "Replace TMP Font");
                    tmp.font = _targetFont;
                    count++;
                }
            }
        }
        Debug.Log($"[一键替换 TMP 字体] 已将 {_targetFont.name} 应用到 {count} 个 TextMeshProUGUI。");
        EditorUtility.DisplayDialog("完成", $"已替换 {count} 个 TMP 文本的字体。", "确定");
    }
}
