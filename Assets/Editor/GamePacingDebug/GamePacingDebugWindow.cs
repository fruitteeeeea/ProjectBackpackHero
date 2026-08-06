using BackpackHero.Debugging;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    /// <summary>玩法设计页：草稿编辑、运行时应用和资产持久化明确分离。</summary>
    internal sealed class GamePacingDebugWindow : ScriptableObject
    {
        private readonly DebugDraft<GamePacingMultipliers> draft = new();
        private GamePacingDebugSettings settingsTarget;
        private GamePacingDebugRuntime lastRuntime;
        private float damageFloatingTextMagicNumber;
        private bool isDamageFloatingTextMagicNumberDirty;
        private string validationMessage;

        internal void DrawTab()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("全局游戏节奏调试", EditorStyles.boldLabel);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后，面板会自动连接全局调试运行时。", MessageType.Info);
                return;
            }

            GamePacingDebugRuntime runtime = GamePacingDebugRuntime.Instance;
            if (runtime == null)
            {
                EditorGUILayout.HelpBox("正在等待全局节奏调试运行时启动。", MessageType.Warning);
                return;
            }

            SyncRuntime(runtime);
            DrawSettingsTarget(runtime);
            DrawMultipliers();
            DrawValidation();
            DrawPersistence(runtime);
            DrawGameSpeed(runtime);
        }

        private void SyncRuntime(GamePacingDebugRuntime runtime)
        {
            if (runtime == lastRuntime)
            {
                return;
            }

            lastRuntime = runtime;
            settingsTarget = runtime.DefaultSettings;
            draft.Load(settingsTarget != null ? settingsTarget.GetValues() : runtime.Multipliers);
            damageFloatingTextMagicNumber =
                runtime.DamageFloatingTextMagicNumber;
            isDamageFloatingTextMagicNumberDirty = false;
        }

        private void DrawSettingsTarget(GamePacingDebugRuntime runtime)
        {
            EditorGUILayout.Space(6f);
            GamePacingDebugSettings nextTarget = (GamePacingDebugSettings)EditorGUILayout.ObjectField(
                "保存目标", settingsTarget, typeof(GamePacingDebugSettings), false);
            if (nextTarget != settingsTarget)
            {
                settingsTarget = nextTarget;
                draft.Load(settingsTarget != null ? settingsTarget.GetValues() : runtime.Multipliers);
                damageFloatingTextMagicNumber =
                    settingsTarget != null
                        ? settingsTarget.DamageFloatingTextMagicNumber
                        : runtime.DamageFloatingTextMagicNumber;
                isDamageFloatingTextMagicNumberDirty = false;
                validationMessage = null;
            }

            EditorGUILayout.LabelField(
                "资产路径",
                settingsTarget != null ? AssetDatabase.GetAssetPath(settingsTarget) : "未选择（请使用“另存为”创建配置）",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField("未保存修改", HasUnsavedChanges ? "是" : "否", EditorStyles.miniLabel);
        }

        private void DrawMultipliers()
        {
            EditorGUILayout.Space(8f);
            GamePacingMultipliers current = draft.Value;
            EditorGUILayout.LabelField("白板倍率", EditorStyles.boldLabel);
            float whiteboardCooldown = DrawMultiplier(
                "物品CD冷却倍率", current.WhiteboardCooldown);
            float aircraftSpeed = DrawMultiplier(
                "飞机飞行速度", current.AircraftSpeed);
            float projectileDamage = DrawMultiplier(
                "子弹伤害", current.ProjectileDamage);
            float aircraftHealth = DrawMultiplier(
                "飞机血量", current.AircraftHealth);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("背包血量", EditorStyles.boldLabel);
            float playerBackpackHealth = DrawMultiplier(
                "玩家背包血量", current.PlayerBackpackHealth);
            float enemyBackpackHealth = DrawMultiplier(
                "敌人背包血量", current.EnemyBackpackHealth);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("整体强度", EditorStyles.boldLabel);
            float playerOverallStrength = DrawMultiplier(
                "玩家整体强度", current.PlayerOverallStrength);
            float enemyOverallStrength = DrawMultiplier(
                "敌人整体强度", current.EnemyOverallStrength);

            GamePacingMultipliers changed = new(
                aircraftSpeed,
                projectileDamage,
                aircraftHealth,
                playerBackpackHealth,
                enemyBackpackHealth,
                playerOverallStrength,
                enemyOverallStrength,
                whiteboardCooldown);
            if (!AreEqual(current, changed))
            {
                draft.Value = changed;
                validationMessage = null;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("视觉显示", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "伤害飘字显示倍率只影响文字，不参与实际伤害结算。",
                MessageType.None);
            float nextMagicNumber = EditorGUILayout.FloatField(
                "伤害飘字魔法数字",
                damageFloatingTextMagicNumber);
            nextMagicNumber = Mathf.Max(0f, nextMagicNumber);
            if (!Mathf.Approximately(
                    nextMagicNumber,
                    damageFloatingTextMagicNumber))
            {
                damageFloatingTextMagicNumber = nextMagicNumber;
                isDamageFloatingTextMagicNumberDirty = true;
                validationMessage = null;
            }
        }

        private static float DrawMultiplier(string label, float value) =>
            EditorGUILayout.Slider(label, value, GamePacingMultipliers.MinimumMultiplier,
                GamePacingMultipliers.MaximumMultiplier);

        private void DrawValidation()
        {
            if (!string.IsNullOrEmpty(validationMessage))
            {
                EditorGUILayout.HelpBox(validationMessage, MessageType.Error);
            }
        }

        private void DrawPersistence(GamePacingDebugRuntime runtime)
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("配置操作", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("应用到运行时"))
                {
                    if (ValidateDraft())
                    {
                        if (settingsTarget != null)
                        {
                            runtime.SetDefaultSettings(settingsTarget);
                        }

                        runtime.SetMultipliers(draft.Value);
                        runtime.SetDamageFloatingTextMagicNumber(
                            damageFloatingTextMagicNumber);
                    }
                }

                using (new EditorGUI.DisabledScope(settingsTarget == null || !HasUnsavedChanges))
                {
                    if (GUILayout.Button("保存"))
                    {
                        SaveToTarget(settingsTarget);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("另存为"))
                {
                    SaveAs();
                }

                if (GUILayout.Button("还原"))
                {
                    draft.Load(settingsTarget != null ? settingsTarget.GetValues() : runtime.Multipliers);
                    damageFloatingTextMagicNumber =
                        settingsTarget != null
                            ? settingsTarget.DamageFloatingTextMagicNumber
                            : runtime.DamageFloatingTextMagicNumber;
                    isDamageFloatingTextMagicNumberDirty = false;
                    validationMessage = null;
                }
            }
        }

        private void DrawGameSpeed(GamePacingDebugRuntime runtime)
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("临时调试控制", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox("游戏速度只在当前 Play Mode 生效，不会保存到玩法配置。", MessageType.None);
                float speed = EditorGUILayout.Slider("游戏速度", runtime.GameSpeed, 0.5f, 2f);
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

        private bool ValidateDraft()
        {
            GamePacingMultipliers values = draft.Value;
            if (!IsInRange(values.AircraftSpeed) || !IsInRange(values.ProjectileDamage) ||
                !IsInRange(values.AircraftHealth) || !IsInRange(values.PlayerBackpackHealth) ||
                !IsInRange(values.EnemyBackpackHealth) || !IsInRange(values.PlayerOverallStrength) ||
                !IsInRange(values.EnemyOverallStrength) || !IsInRange(values.WhiteboardCooldown))
            {
                validationMessage = "所有节奏倍率必须在允许范围内。";
                return false;
            }

            validationMessage = null;
            return true;
        }

        private static bool IsInRange(float value) => value >= GamePacingMultipliers.MinimumMultiplier &&
            value <= GamePacingMultipliers.MaximumMultiplier;

        private bool HasUnsavedChanges =>
            draft.IsDirty || isDamageFloatingTextMagicNumberDirty;

        private void SaveAs()
        {
            if (!ValidateDraft())
            {
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "另存为游戏节奏配置", "GamePacingDebugSettings", "asset", "选择玩法配置位置");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            GamePacingDebugSettings newSettings = CreateInstance<GamePacingDebugSettings>();
            newSettings.SetValues(draft.Value);
            newSettings.SetDamageFloatingTextMagicNumber(
                damageFloatingTextMagicNumber);
            AssetDatabase.CreateAsset(newSettings, path);
            AssetDatabase.SaveAssets();
            settingsTarget = newSettings;
            draft.Load(newSettings.GetValues());
            GamePacingDebugRuntime.Instance?.SetDefaultSettings(newSettings);
        }

        private void SaveToTarget(GamePacingDebugSettings target)
        {
            if (target == null || !ValidateDraft())
            {
                return;
            }

            Undo.RecordObject(target, "保存游戏节奏配置");
            target.SetValues(draft.Value);
            target.SetDamageFloatingTextMagicNumber(
                damageFloatingTextMagicNumber);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            draft.Load(target.GetValues());
            isDamageFloatingTextMagicNumberDirty = false;
        }

        private static bool AreEqual(GamePacingMultipliers left, GamePacingMultipliers right) =>
            Mathf.Approximately(left.AircraftSpeed, right.AircraftSpeed) &&
            Mathf.Approximately(left.ProjectileDamage, right.ProjectileDamage) &&
            Mathf.Approximately(left.AircraftHealth, right.AircraftHealth) &&
            Mathf.Approximately(left.PlayerBackpackHealth, right.PlayerBackpackHealth) &&
            Mathf.Approximately(left.EnemyBackpackHealth, right.EnemyBackpackHealth) &&
            Mathf.Approximately(left.PlayerOverallStrength, right.PlayerOverallStrength) &&
            Mathf.Approximately(left.EnemyOverallStrength, right.EnemyOverallStrength) &&
            Mathf.Approximately(left.WhiteboardCooldown, right.WhiteboardCooldown);
    }
}
