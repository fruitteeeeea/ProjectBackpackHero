using BackpackHero.Debugging;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    /// <summary>全局游戏节奏的 Play Mode 调试面板。</summary>
    internal sealed class GamePacingDebugWindow : ScriptableObject
    {
        private const string DefaultSettingsAssetPath =
            "Assets/Resources/GamePacingDebugSettings.asset";

        private bool showSaveDefaultButton;
        private GamePacingDebugRuntime boundRuntime;

        private void BindDefaultSettingsAsset()
        {
            GamePacingDebugRuntime runtime =
                GamePacingDebugRuntime.Instance;
            if (runtime == null || runtime == boundRuntime)
            {
                return;
            }

            // 该资产可能由版本控制新同步到项目中；先强制导入，
            // 再通过AssetDatabase取得真实对象引用。
            AssetDatabase.ImportAsset(
                DefaultSettingsAssetPath,
                ImportAssetOptions.ForceUpdate |
                ImportAssetOptions.ForceSynchronousImport);

            GamePacingDebugSettings defaultSettings =
                AssetDatabase.LoadAssetAtPath<
                    GamePacingDebugSettings>(
                    DefaultSettingsAssetPath);

            if (defaultSettings == null)
            {
                Debug.LogError(
                    "无法加载全局节奏默认配置资产：" +
                    DefaultSettingsAssetPath);
                return;
            }

            runtime.SetDefaultSettings(defaultSettings);
            boundRuntime = runtime;
        }

        internal void DrawTab()
        {
            BindDefaultSettingsAsset();
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "全局游戏节奏调试",
                EditorStyles.boldLabel);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "进入 Play Mode 后，面板会自动连接全局调试运行时。",
                    MessageType.Info);
                return;
            }

            GamePacingDebugRuntime runtime =
                GamePacingDebugRuntime.Instance;
            if (runtime == null)
            {
                EditorGUILayout.HelpBox(
                    "正在等待全局节奏调试运行时启动。",
                    MessageType.Warning);
                return;
            }

            if (runtime.DefaultSettings == null)
            {
                BindDefaultSettingsAsset();
            }

            DrawMultipliers(runtime);
            EditorGUILayout.Space(10f);
            DrawPersistence(runtime);
            EditorGUILayout.Space(10f);
            DrawGameSpeed(runtime);
        }

        private static void DrawMultipliers(
            GamePacingDebugRuntime runtime)
        {
            EditorGUILayout.LabelField(
                "战斗倍率",
                EditorStyles.boldLabel);

            GamePacingMultipliers current =
                runtime.Multipliers;
            float aircraftSpeed = DrawMultiplier(
                "飞机飞行速度",
                current.AircraftSpeed);
            float projectileDamage = DrawMultiplier(
                "子弹伤害",
                current.ProjectileDamage);
            float aircraftHealth = DrawMultiplier(
                "飞机血量",
                current.AircraftHealth);
            float playerBackpackHealth = DrawMultiplier(
                "玩家背包血量",
                current.PlayerBackpackHealth);
            float enemyBackpackHealth = DrawMultiplier(
                "敌人背包血量",
                current.EnemyBackpackHealth);
            EditorGUILayout.Space(4f);
            float playerOverallStrength = DrawMultiplier(
                "玩家整体强度",
                current.PlayerOverallStrength);
            float enemyOverallStrength = DrawMultiplier(
                "敌人整体强度",
                current.EnemyOverallStrength);

            GamePacingMultipliers changed =
                new(
                    aircraftSpeed,
                    projectileDamage,
                    aircraftHealth,
                    playerBackpackHealth,
                    enemyBackpackHealth,
                    playerOverallStrength,
                    enemyOverallStrength);

            if (!AreEqual(current, changed))
            {
                runtime.SetMultipliers(changed);
            }
        }

        private static float DrawMultiplier(
            string label,
            float value)
        {
            return EditorGUILayout.Slider(
                label,
                value,
                GamePacingMultipliers.MinimumMultiplier,
                GamePacingMultipliers.MaximumMultiplier);
        }

        private void DrawPersistence(
            GamePacingDebugRuntime runtime)
        {
            EditorGUILayout.LabelField(
                "配置资产",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                EditorGUILayout.ObjectField(
                    "默认配置资产",
                    runtime.DefaultSettings,
                    typeof(GamePacingDebugSettings),
                    false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("加载"))
                    {
                        runtime.LoadSavedValues();
                    }

                    if (GUILayout.Button("恢复默认"))
                    {
                        runtime.RestoreRuntimeDefaults();
                    }
                }

                showSaveDefaultButton =
                    EditorGUILayout.ToggleLeft(
                        "显示默认按钮",
                        showSaveDefaultButton);

                if (showSaveDefaultButton &&
                    GUILayout.Button("保存至默认"))
                {
                    if (runtime.SaveCurrentValuesAsDefault())
                    {
                        EditorUtility.SetDirty(
                            runtime.DefaultSettings);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
        }

        private static void DrawGameSpeed(
            GamePacingDebugRuntime runtime)
        {
            EditorGUILayout.LabelField(
                "临时调试控制",
                EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(
                    "游戏速度只在当前 Play Mode 生效，退出或关闭面板后恢复为 1。",
                    MessageType.None);

                float speed = EditorGUILayout.Slider(
                    "游戏速度",
                    runtime.GameSpeed,
                    0.5f,
                    2f);
                if (!Mathf.Approximately(speed, runtime.GameSpeed))
                {
                    runtime.SetGameSpeed(speed);
                }

                if (GUILayout.Button("重置游戏速度（1x）"))
                {
                    runtime.ResetGameSpeed();
                }
            }
        }

        private static bool AreEqual(
            GamePacingMultipliers left,
            GamePacingMultipliers right)
        {
            return Mathf.Approximately(
                       left.AircraftSpeed,
                       right.AircraftSpeed) &&
                   Mathf.Approximately(
                       left.ProjectileDamage,
                       right.ProjectileDamage) &&
                   Mathf.Approximately(
                       left.AircraftHealth,
                       right.AircraftHealth) &&
                   Mathf.Approximately(
                       left.PlayerBackpackHealth,
                       right.PlayerBackpackHealth) &&
                   Mathf.Approximately(
                       left.EnemyBackpackHealth,
                       right.EnemyBackpackHealth) &&
                   Mathf.Approximately(
                       left.PlayerOverallStrength,
                       right.PlayerOverallStrength) &&
                   Mathf.Approximately(
                       left.EnemyOverallStrength,
                       right.EnemyOverallStrength);
        }
    }
}
