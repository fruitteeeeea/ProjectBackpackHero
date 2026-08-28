using BackpackHero.Audio;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    internal sealed class GameSfxDebugWindow : ScriptableObject
    {
        internal void DrawTab()
        {
            EditorGUILayout.LabelField("游戏声音", EditorStyles.boldLabel);

            GameSfxService service = GameSfxService.Instance;
            if (!EditorApplication.isPlaying || service == null)
            {
                EditorGUILayout.HelpBox("请进入 Play Mode，等待 GameSfxService 创建。", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("服务状态", "已连接");
            EditorGUILayout.LabelField("SFX 总开关", service.IsEnabled ? "开启" : "关闭");
            EditorGUILayout.LabelField("AudioSource", service.HasAudioSource ? "可用" : "缺失");
            EditorGUILayout.LabelField("音效目录", service.HasCatalog ? "已加载" : "未加载（使用 Resources 回退）");
            EditorGUILayout.LabelField("默认按钮音效", service.DefaultUiClipName);
            EditorGUILayout.LabelField("已绑定按钮数", service.GetComponent<UiSfxAutoBinder>()?.BoundButtonCount.ToString() ?? "0");

            EditorGUILayout.Space();
            bool enabled = EditorGUILayout.Toggle("打印按钮点击日志", service.UiDiagnosticLoggingEnabled);
            if (enabled != service.UiDiagnosticLoggingEnabled)
            {
                service.SetUiDiagnosticLoggingEnabled(enabled);
            }

            EditorGUILayout.HelpBox("日志前缀：[SFX-DIAG]。此开关只在当前 Play Mode 有效，不会修改 SFX 设置或 PlayerPrefs。", MessageType.None);
            using (new EditorGUI.DisabledScope(!service.UiDiagnosticLoggingEnabled))
            {
                if (GUILayout.Button("输出音效服务状态快照"))
                {
                    service.LogDiagnosticSnapshot();
                }
            }
        }
    }
}
