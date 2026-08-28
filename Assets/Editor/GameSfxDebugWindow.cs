using BackpackHero.Audio;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    internal sealed class GameSfxDebugWindow : ScriptableObject
    {
        private readonly DebugDraft<GameSfxTuning> tuningDraft = new();
        private GameSfxService lastService;

        internal void DrawTab()
        {
            EditorGUILayout.LabelField("游戏声音", EditorStyles.boldLabel);

            GameSfxService service = GameSfxService.Instance;
            if (!EditorApplication.isPlaying || service == null)
            {
                EditorGUILayout.HelpBox("请进入 Play Mode，等待 GameSfxService 创建。", MessageType.Info);
                return;
            }

            SyncTuningDraft(service);

            EditorGUILayout.LabelField("服务状态", "已连接");
            EditorGUILayout.LabelField("SFX 总开关", service.IsEnabled ? "开启" : "关闭");
            EditorGUILayout.LabelField("AudioSource", service.HasAudioSource ? "可用" : "缺失");
            EditorGUILayout.LabelField("音效目录", service.HasCatalog ? "已加载" : "未加载（使用 Resources 回退）");
            EditorGUILayout.LabelField("默认按钮音效", service.DefaultUiClipName);
            EditorGUILayout.LabelField("已绑定按钮数", service.GetComponent<UiSfxAutoBinder>()?.BoundButtonCount.ToString() ?? "0");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("非按钮游戏音效调音", EditorStyles.boldLabel);
            GameSfxTuning tuning = tuningDraft.Value;
            EditorGUI.BeginChangeCheck();
            float basePitch = EditorGUILayout.Slider("基础音高", tuning.BasePitch,
                GameSfxTuning.MinimumPitch, GameSfxTuning.MaximumPitch);
            float randomPitchOffset = EditorGUILayout.Slider("随机音高偏移", tuning.RandomPitchOffset,
                0f, GameSfxTuning.MaximumRandomPitchOffset);
            float volumeMultiplier = EditorGUILayout.Slider("响度倍率", tuning.VolumeMultiplier,
                GameSfxTuning.MinimumVolumeMultiplier, GameSfxTuning.MaximumVolumeMultiplier);
            if (EditorGUI.EndChangeCheck())
            {
                tuningDraft.Value = new GameSfxTuning(basePitch, randomPitchOffset, volumeMultiplier);
            }

            EditorGUILayout.LabelField("未保存修改", tuningDraft.IsDirty ? "是" : "否", EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("应用到运行时"))
                {
                    service.ApplyNonUiTuning(tuningDraft.Value);
                }

                using (new EditorGUI.DisabledScope(!tuningDraft.IsDirty))
                {
                    if (GUILayout.Button("保存"))
                    {
                        service.SaveNonUiTuning(tuningDraft.Value);
                        tuningDraft.Load(service.LoadSavedNonUiTuning());
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("使用默认值"))
                {
                    tuningDraft.Value = GameSfxTuning.Default;
                }

                if (GUILayout.Button("还原"))
                {
                    GameSfxTuning saved = service.LoadSavedNonUiTuning();
                    tuningDraft.Load(saved);
                    service.ApplyNonUiTuning(saved);
                }
            }

            EditorGUILayout.HelpBox("影响背包物品、飞机受击、子弹和胜利音效；普通按钮点击不受影响。调整后先“应用到运行时”试听，确认后再点击“保存”。", MessageType.None);

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

        private void SyncTuningDraft(GameSfxService service)
        {
            if (service == lastService && tuningDraft.HasValue)
            {
                return;
            }

            lastService = service;
            tuningDraft.Load(service.LoadSavedNonUiTuning());
        }
    }
}
