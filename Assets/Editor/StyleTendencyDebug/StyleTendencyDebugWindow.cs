using BackpackHero.Debugging;
using BackpackHero.Battle;
using BackpackPrototype;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    /// <summary>玩法设计页：编辑、应用并持久化风格倾向配置。</summary>
    internal sealed class StyleTendencyDebugWindow : ScriptableObject
    {
        private readonly DebugDraft<StyleTendencyMultipliers> draft = new();
        private StyleTendencyDebugSettings settingsTarget;
        private StyleTendencyDebugRuntime lastRuntime;
        private string validationMessage;

        internal void DrawTab()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("全局风格倾向调试", EditorStyles.boldLabel);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后，面板会自动连接全局风格倾向运行时。", MessageType.Info);
                return;
            }

            StyleTendencyDebugRuntime runtime = StyleTendencyDebugRuntime.Instance;
            if (runtime == null)
            {
                EditorGUILayout.HelpBox("正在等待全局风格倾向运行时启动。", MessageType.Warning);
                return;
            }

            SyncRuntime(runtime);
            DrawSettingsTarget(runtime);
            DrawMultipliers();
            DrawValidation();
            DrawPersistence(runtime);
            DrawPhaseControl();
        }

        private void SyncRuntime(StyleTendencyDebugRuntime runtime)
        {
            if (runtime == lastRuntime)
            {
                return;
            }

            lastRuntime = runtime;
            settingsTarget = runtime.DefaultSettings;
            draft.Load(settingsTarget != null
                ? settingsTarget.GetValues()
                : runtime.Multipliers);
        }

        private void DrawSettingsTarget(StyleTendencyDebugRuntime runtime)
        {
            EditorGUILayout.Space(6f);
            StyleTendencyDebugSettings nextTarget =
                (StyleTendencyDebugSettings)EditorGUILayout.ObjectField(
                    "保存目标", settingsTarget,
                    typeof(StyleTendencyDebugSettings), false);
            if (nextTarget != settingsTarget)
            {
                settingsTarget = nextTarget;
                draft.Load(settingsTarget != null
                    ? settingsTarget.GetValues()
                    : runtime.Multipliers);
                validationMessage = null;
            }

            EditorGUILayout.LabelField("资产路径", settingsTarget != null
                ? AssetDatabase.GetAssetPath(settingsTarget)
                : "未选择（请使用“另存为”创建配置）", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("未保存修改", draft.IsDirty ? "是" : "否", EditorStyles.miniLabel);
        }

        private void DrawMultipliers()
        {
            EditorGUILayout.Space(8f);
            StyleTendencyMultipliers current = draft.Value;
            int cooldownTypeIndex = EditorGUILayout.Popup(
                "物品冷却方式",
                current.CooldownItemType == CooldownItemType.Aircraft ? 0 : 1,
                new[] { "飞机物品冷却", "装备物品冷却" });
            CooldownItemType cooldownItemType = cooldownTypeIndex == 0
                ? CooldownItemType.Aircraft
                : CooldownItemType.Equipment;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("全局属性倍率", EditorStyles.boldLabel);
            float itemCooldownSpeed = DrawMultiplier(
                "物品冷却速度", current.ItemCooldownSpeed);
            float targetingArcAngle = DrawMultiplier(
                "飞机索敌角度", current.AircraftTargetingArcAngle);
            float attackRange = DrawMultiplier(
                "飞机攻击范围", current.AircraftAttackRange);
            float attackSpeed = EditorGUILayout.Slider(
                "飞机射速", current.AircraftAttackSpeed,
                StyleTendencyMultipliers.MinimumAircraftAttackSpeedMultiplier,
                StyleTendencyMultipliers.MaximumAircraftAttackSpeedMultiplier);
            float aircraftFlightSpeed = EditorGUILayout.Slider(
                "飞机飞行速度", current.AircraftFlightSpeed,
                StyleTendencyMultipliers.MinimumAircraftFlightSpeedMultiplier,
                StyleTendencyMultipliers.MaximumAircraftFlightSpeedMultiplier);
            float aircraftLifetime = EditorGUILayout.Slider(
                "飞机存活时间", current.AircraftLifetime,
                StyleTendencyMultipliers.MinimumAircraftLifetimeMultiplier,
                StyleTendencyMultipliers.MaximumAircraftLifetimeMultiplier);
            float projectileSpeed = EditorGUILayout.Slider(
                "子弹速度", current.ProjectileSpeed,
                StyleTendencyMultipliers.MinimumProjectileSpeedMultiplier,
                StyleTendencyMultipliers.MaximumProjectileMultiplier);
            float projectileLifetime = EditorGUILayout.Slider(
                "子弹存活时间", current.ProjectileLifetime,
                StyleTendencyMultipliers.MinimumProjectileLifetimeMultiplier,
                StyleTendencyMultipliers.MaximumProjectileMultiplier);
            StyleTendencyMultipliers changed = new(
                itemCooldownSpeed,
                targetingArcAngle,
                attackRange,
                attackSpeed,
                cooldownItemType,
                aircraftLifetime,
                projectileSpeed,
                projectileLifetime,
                aircraftFlightSpeed);
            if (!AreEqual(current, changed))
            {
                draft.Value = changed;
                validationMessage = null;
            }
        }

        private static float DrawMultiplier(string label, float value) =>
            EditorGUILayout.Slider(label, value,
                StyleTendencyMultipliers.MinimumMultiplier,
                StyleTendencyMultipliers.MaximumMultiplier);

        private void DrawValidation()
        {
            if (!string.IsNullOrEmpty(validationMessage))
            {
                EditorGUILayout.HelpBox(validationMessage, MessageType.Error);
            }
        }

        private void DrawPersistence(StyleTendencyDebugRuntime runtime)
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("配置操作", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("应用到运行时") && ValidateDraft())
                {
                    if (settingsTarget != null)
                    {
                        runtime.SetDefaultSettings(settingsTarget);
                    }

                    runtime.SetMultipliers(draft.Value);
                    BattleFlowController.EnsureInstance().TogglePhase();
                }

                using (new EditorGUI.DisabledScope(
                           settingsTarget == null || !draft.IsDirty))
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
                    SaveAs(runtime);
                }

                if (GUILayout.Button("还原"))
                {
                    draft.Load(settingsTarget != null
                        ? settingsTarget.GetValues()
                        : runtime.Multipliers);
                    validationMessage = null;
                }
            }
        }

        private static void DrawPhaseControl()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("阶段控制", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    $"当前阶段：{BattleFlowController.CurrentPhase}",
                    EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("切换阶段"))
                {
                    BattleFlowController.EnsureInstance().TogglePhase();
                }
            }
        }

        private bool ValidateDraft()
        {
            StyleTendencyMultipliers values = draft.Value;
            if (!IsInRange(values.ItemCooldownSpeed) ||
                !IsInRange(values.AircraftTargetingArcAngle) ||
                !IsInRange(values.AircraftAttackRange) ||
                !IsAircraftAttackSpeedInRange(values.AircraftAttackSpeed) ||
                !IsAircraftFlightSpeedInRange(values.AircraftFlightSpeed) ||
                !IsAircraftLifetimeInRange(values.AircraftLifetime) ||
                !IsProjectileSpeedInRange(values.ProjectileSpeed) ||
                !IsProjectileLifetimeInRange(values.ProjectileLifetime))
            {
                validationMessage = "前三项风格倍率必须在 0.5 到 3 之间；飞机射速和飞行速度必须在各自范围内；飞机存活时间必须在 0.5 到 2 之间；子弹速度必须在 0.5 到 1 之间，子弹存活时间必须在 0.1 到 1 之间。";
                return false;
            }

            validationMessage = null;
            return true;
        }

        private static bool IsInRange(float value) =>
            value >= StyleTendencyMultipliers.MinimumMultiplier &&
            value <= StyleTendencyMultipliers.MaximumMultiplier;

        private static bool IsAircraftAttackSpeedInRange(float value) =>
            value >= StyleTendencyMultipliers
                .MinimumAircraftAttackSpeedMultiplier &&
            value <= StyleTendencyMultipliers
                .MaximumAircraftAttackSpeedMultiplier;

        private static bool IsAircraftLifetimeInRange(float value) =>
            value >= StyleTendencyMultipliers
                .MinimumAircraftLifetimeMultiplier &&
            value <= StyleTendencyMultipliers
                .MaximumAircraftLifetimeMultiplier;

        private static bool IsAircraftFlightSpeedInRange(float value) =>
            value >= StyleTendencyMultipliers
                .MinimumAircraftFlightSpeedMultiplier &&
            value <= StyleTendencyMultipliers
                .MaximumAircraftFlightSpeedMultiplier;

        private static bool IsProjectileSpeedInRange(float value) =>
            value >= StyleTendencyMultipliers
                .MinimumProjectileSpeedMultiplier &&
            value <= StyleTendencyMultipliers
                .MaximumProjectileMultiplier;

        private static bool IsProjectileLifetimeInRange(float value) =>
            value >= StyleTendencyMultipliers
                .MinimumProjectileLifetimeMultiplier &&
            value <= StyleTendencyMultipliers
                .MaximumProjectileMultiplier;

        private void SaveAs(StyleTendencyDebugRuntime runtime)
        {
            if (!ValidateDraft())
            {
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "另存为风格倾向配置", "StyleTendencyDebugSettings", "asset",
                "选择玩法配置位置");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            StyleTendencyDebugSettings newSettings =
                CreateInstance<StyleTendencyDebugSettings>();
            newSettings.SetValues(draft.Value);
            AssetDatabase.CreateAsset(newSettings, path);
            AssetDatabase.SaveAssets();
            settingsTarget = newSettings;
            draft.Load(newSettings.GetValues());
            runtime.SetDefaultSettings(newSettings);
            runtime.SetMultipliers(draft.Value);
        }

        private void SaveToTarget(StyleTendencyDebugSettings target)
        {
            if (target == null || !ValidateDraft())
            {
                return;
            }

            Undo.RecordObject(target, "保存风格倾向配置");
            target.SetValues(draft.Value);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            draft.Load(target.GetValues());
        }

        private static bool AreEqual(
            StyleTendencyMultipliers left,
            StyleTendencyMultipliers right) =>
            Mathf.Approximately(left.ItemCooldownSpeed, right.ItemCooldownSpeed) &&
            Mathf.Approximately(left.AircraftTargetingArcAngle,
                right.AircraftTargetingArcAngle) &&
            Mathf.Approximately(left.AircraftAttackRange,
                right.AircraftAttackRange) &&
            Mathf.Approximately(left.AircraftAttackSpeed,
                right.AircraftAttackSpeed) &&
            Mathf.Approximately(left.AircraftFlightSpeed,
                right.AircraftFlightSpeed) &&
            left.CooldownItemType == right.CooldownItemType &&
            Mathf.Approximately(left.AircraftLifetime,
                right.AircraftLifetime) &&
            Mathf.Approximately(left.ProjectileSpeed,
                right.ProjectileSpeed) &&
            Mathf.Approximately(left.ProjectileLifetime,
                right.ProjectileLifetime);
    }
}
