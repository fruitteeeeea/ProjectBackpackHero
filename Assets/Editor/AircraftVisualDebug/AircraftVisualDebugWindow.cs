using BackpackHero.Debugging;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace BackpackHero.EditorTools
{
    /// <summary>程序测试页：编辑、应用并持久化飞机视觉开关。</summary>
    internal sealed class AircraftVisualDebugWindow : ScriptableObject
    {
        private const string HitParticlesPath =
            "Assets/Prefabs/VFX/Particles/VFX_Particles_AircraftExplosion.prefab";
        private const string DeathExplosionParticlesPath =
            "Assets/Prefabs/VFX/Particles/VFX_Particles_AircraftExplosion 2.prefab";
        private const string DeathFlashParticlesPath =
            "Assets/Prefabs/VFX/Particles/VFX_Particles_DeathFlash.prefab";

        private readonly DebugDraft<AircraftVisualSettings> draft = new();
        private AircraftVisualDebugSettings settingsTarget;
        private AircraftVisualDebugRuntime lastRuntime;
        private readonly DebugDraft<FloatingDamageTextVisualSettings>
            floatingTextDraft = new();
        private FloatingDamageTextDebugSettings floatingTextSettingsTarget;
        private FloatingDamageTextDebugRuntime lastFloatingTextRuntime;
        private Vector2 scrollPosition;

        internal void DrawTab()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("全局飞机视觉动效", EditorStyles.boldLabel);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "进入 Play Mode 后，面板会连接全局飞机视觉控制器。",
                    MessageType.Info);
                return;
            }

            AircraftVisualDebugRuntime runtime =
                AircraftVisualDebugRuntime.Instance;
            if (runtime == null)
            {
                EditorGUILayout.HelpBox(
                    "正在等待全局飞机视觉控制器启动。",
                    MessageType.Warning);
                return;
            }

            SyncRuntime(runtime);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawOverrideToggle();
            DrawSettingsTarget();
            DrawToggles();
            DrawPersistence(runtime);
            DrawParticleAssets();
            DrawFloatingTextSettings();
            EditorGUILayout.EndScrollView();
        }

        private void DrawFloatingTextSettings()
        {
            FloatingDamageTextDebugRuntime runtime =
                FloatingDamageTextDebugRuntime.Instance;
            if (runtime == null)
            {
                EditorGUILayout.HelpBox(
                    "正在等待伤害飘字调试控制器启动。",
                    MessageType.Warning);
                return;
            }

            SyncFloatingTextRuntime(runtime);
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("伤害飘字", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "字体与字号独立于“启用飞机视觉调试覆写”总开关。",
                MessageType.None);

            FloatingDamageTextDebugSettings nextTarget =
                (FloatingDamageTextDebugSettings)EditorGUILayout.ObjectField(
                    "保存目标",
                    floatingTextSettingsTarget,
                    typeof(FloatingDamageTextDebugSettings),
                    false);
            if (nextTarget != floatingTextSettingsTarget)
            {
                floatingTextSettingsTarget = nextTarget;
                floatingTextDraft.Load(floatingTextSettingsTarget != null
                    ? floatingTextSettingsTarget.GetValues()
                    : runtime.Settings);
            }

            EditorGUILayout.LabelField(
                "资产路径",
                floatingTextSettingsTarget != null
                    ? AssetDatabase.GetAssetPath(floatingTextSettingsTarget)
                    : "未选择（请使用“另存为”创建配置）",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                "未保存修改",
                floatingTextDraft.IsDirty ? "是" : "否",
                EditorStyles.miniLabel);

            FloatingDamageTextVisualSettings current = floatingTextDraft.Value;
            TMP_FontAsset font = (TMP_FontAsset)EditorGUILayout.ObjectField(
                "飘字字体",
                current.Font,
                typeof(TMP_FontAsset),
                false);
            float fontSize = Mathf.Max(
                FloatingDamageTextVisualSettings.MinimumFontSize,
                EditorGUILayout.FloatField("字体大小", current.FontSize));
            floatingTextDraft.Value = new FloatingDamageTextVisualSettings(
                font,
                fontSize);

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("应用到运行时"))
                {
                    if (floatingTextSettingsTarget != null)
                    {
                        runtime.SetDefaultSettings(floatingTextSettingsTarget);
                    }

                    runtime.SetSettings(floatingTextDraft.Value);
                }

                using (new EditorGUI.DisabledScope(
                           floatingTextSettingsTarget == null ||
                           !floatingTextDraft.IsDirty))
                {
                    if (GUILayout.Button("保存"))
                    {
                        SaveFloatingTextToTarget(floatingTextSettingsTarget);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("另存为"))
                {
                    SaveFloatingTextAs(runtime);
                }

                if (GUILayout.Button("还原"))
                {
                    floatingTextDraft.Load(floatingTextSettingsTarget != null
                        ? floatingTextSettingsTarget.GetValues()
                        : runtime.Settings);
                }
            }
        }

        private void SyncFloatingTextRuntime(
            FloatingDamageTextDebugRuntime runtime)
        {
            if (runtime == lastFloatingTextRuntime)
            {
                return;
            }

            lastFloatingTextRuntime = runtime;
            floatingTextSettingsTarget = runtime.DefaultSettings;
            floatingTextDraft.Load(floatingTextSettingsTarget != null
                ? floatingTextSettingsTarget.GetValues()
                : runtime.Settings);
        }

        private void SaveFloatingTextAs(FloatingDamageTextDebugRuntime runtime)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "另存为伤害飘字配置",
                "FloatingDamageTextDebugSettings",
                "asset",
                "选择伤害飘字配置位置");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            FloatingDamageTextDebugSettings newSettings =
                CreateInstance<FloatingDamageTextDebugSettings>();
            newSettings.SetValues(floatingTextDraft.Value);
            AssetDatabase.CreateAsset(newSettings, path);
            AssetDatabase.SaveAssets();
            floatingTextSettingsTarget = newSettings;
            floatingTextDraft.Load(newSettings.GetValues());
            runtime.SetDefaultSettings(newSettings);
            runtime.SetSettings(floatingTextDraft.Value);
        }

        private void SaveFloatingTextToTarget(
            FloatingDamageTextDebugSettings target)
        {
            Undo.RecordObject(target, "保存伤害飘字配置");
            target.SetValues(floatingTextDraft.Value);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            floatingTextDraft.Load(target.GetValues());
        }

        private void SyncRuntime(AircraftVisualDebugRuntime runtime)
        {
            if (runtime == lastRuntime)
            {
                return;
            }

            lastRuntime = runtime;
            settingsTarget = runtime.DefaultSettings;
            draft.Load(settingsTarget != null
                ? settingsTarget.GetValues()
                : runtime.Settings);
        }

        private void DrawSettingsTarget()
        {
            EditorGUILayout.Space(6f);
            AircraftVisualDebugSettings nextTarget =
                (AircraftVisualDebugSettings)EditorGUILayout.ObjectField(
                    "保存目标",
                    settingsTarget,
                    typeof(AircraftVisualDebugSettings),
                    false);
            if (nextTarget != settingsTarget)
            {
                settingsTarget = nextTarget;
                draft.Load(settingsTarget != null
                    ? settingsTarget.GetValues()
                    : AircraftVisualSettings.Default);
            }

            EditorGUILayout.LabelField(
                "资产路径",
                settingsTarget != null
                    ? AssetDatabase.GetAssetPath(settingsTarget)
                    : "未选择（请使用“另存为”创建配置）",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                "未保存修改",
                draft.IsDirty ? "是" : "否",
                EditorStyles.miniLabel);
        }

        private void DrawOverrideToggle()
        {
            EditorGUILayout.Space(6f);
            bool enabled = EditorGUILayout.Toggle(
                "启用飞机视觉调试覆写",
                draft.Value.AircraftVisualOverridesEnabled);
            draft.Value = draft.Value.WithAircraftVisualOverridesEnabled(
                enabled);
        }

        private void DrawToggles()
        {
            AircraftVisualSettings current = draft.Value;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("发射", EditorStyles.boldLabel);
            bool attackScaleTween = EditorGUILayout.Toggle(
                "缩放 Tween（MMF_Scale）", current.AttackScaleTween);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("受击", EditorStyles.boldLabel);
            bool hitRotationShakeTween = EditorGUILayout.Toggle(
                "旋转抖动 Tween（MMF_RotationShake）",
                current.HitRotationShakeTween);
            bool hitParticles = EditorGUILayout.Toggle(
                "受击粒子（MMF_InstantiateObject）",
                current.HitParticles);
            bool hitWhiteFlash = EditorGUILayout.Toggle(
                "受击闪白",
                current.HitWhiteFlash);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("死亡", EditorStyles.boldLabel);
            bool deathRotationTween = EditorGUILayout.Toggle(
                "旋转 Tween（MMF_Rotation）",
                current.DeathRotationTween);
            bool deathScaleTween = EditorGUILayout.Toggle(
                "缩放 Tween（MMF_Scale）",
                current.DeathScaleTween);
            bool deathExplosionParticles = EditorGUILayout.Toggle(
                "爆炸粒子",
                current.DeathExplosionParticles);
            bool deathFlashParticles = EditorGUILayout.Toggle(
                "闪光粒子",
                current.DeathFlashParticles);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("飞行", EditorStyles.boldLabel);
            bool aircraftLifetimeEnabled = EditorGUILayout.Toggle(
                "飞机 Lifetime（时间寿命）",
                current.AircraftLifetimeEnabled);
            bool highlightOvertimePenaltyProjectile = EditorGUILayout.Toggle(
                "高亮超时强制退场子弹",
                current.HighlightOvertimePenaltyProjectile);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("爆炸子弹", EditorStyles.boldLabel);
            float explosiveImpactRangeMultiplier = EditorGUILayout.Slider(
                "爆炸范围倍率（AOE + 环形粒子）",
                current.ExplosiveImpactRangeMultiplier,
                AircraftVisualSettings.MinimumExplosiveImpactRangeMultiplier,
                AircraftVisualSettings.MaximumExplosiveImpactRangeMultiplier);

            draft.Value = new AircraftVisualSettings(
                current.AircraftVisualOverridesEnabled,
                attackScaleTween,
                hitRotationShakeTween,
                hitParticles,
                hitWhiteFlash,
                deathRotationTween,
                deathScaleTween,
                deathExplosionParticles,
                deathFlashParticles,
                aircraftLifetimeEnabled,
                highlightOvertimePenaltyProjectile,
                current.HitParticlesStartSpeed,
                current.HitParticlesTexture,
                current.HitParticlesStartSize,
                current.HitParticlesBurstCount,
                current.DeathExplosionParticlesStartSpeed,
                current.DeathExplosionParticlesTexture,
                current.DeathExplosionParticlesStartSize,
                current.DeathExplosionParticlesBurstCount,
                current.DeathFlashParticlesStartSpeed,
                current.DeathFlashParticlesTexture,
                current.DeathFlashParticlesStartSize,
                current.DeathFlashParticlesBurstCount,
                explosiveImpactRangeMultiplier);
        }

        private void DrawPersistence(AircraftVisualDebugRuntime runtime)
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("配置操作", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("应用到运行时"))
                {
                    if (settingsTarget != null)
                    {
                        runtime.SetDefaultSettings(settingsTarget);
                    }

                    runtime.SetSettings(draft.Value);
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
                        : runtime.Settings);
                }
            }
        }

        private void DrawParticleAssets()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("粒子资源与参数", EditorStyles.boldLabel);
            AircraftVisualSettings current = draft.Value;
            float hitStartSpeed = current.HitParticlesStartSpeed;
            Texture2D hitTexture = current.HitParticlesTexture;
            float hitStartSize = current.HitParticlesStartSize;
            int hitBurstCount = current.HitParticlesBurstCount;
            float deathExplosionStartSpeed =
                current.DeathExplosionParticlesStartSpeed;
            Texture2D deathExplosionTexture =
                current.DeathExplosionParticlesTexture;
            float deathExplosionStartSize =
                current.DeathExplosionParticlesStartSize;
            int deathExplosionBurstCount =
                current.DeathExplosionParticlesBurstCount;
            float deathFlashStartSpeed =
                current.DeathFlashParticlesStartSpeed;
            Texture2D deathFlashTexture = current.DeathFlashParticlesTexture;
            float deathFlashStartSize = current.DeathFlashParticlesStartSize;
            int deathFlashBurstCount = current.DeathFlashParticlesBurstCount;

            DrawParticleAsset(
                "受击",
                HitParticlesPath,
                ref hitStartSpeed,
                ref hitStartSize,
                ref hitBurstCount,
                ref hitTexture);
            DrawParticleAsset(
                "死亡爆炸",
                DeathExplosionParticlesPath,
                ref deathExplosionStartSpeed,
                ref deathExplosionStartSize,
                ref deathExplosionBurstCount,
                ref deathExplosionTexture);
            DrawParticleAsset(
                "死亡闪光",
                DeathFlashParticlesPath,
                ref deathFlashStartSpeed,
                ref deathFlashStartSize,
                ref deathFlashBurstCount,
                ref deathFlashTexture);

            draft.Value = new AircraftVisualSettings(
                current.AircraftVisualOverridesEnabled,
                current.AttackScaleTween,
                current.HitRotationShakeTween,
                current.HitParticles,
                current.HitWhiteFlash,
                current.DeathRotationTween,
                current.DeathScaleTween,
                current.DeathExplosionParticles,
                current.DeathFlashParticles,
                current.AircraftLifetimeEnabled,
                current.HighlightOvertimePenaltyProjectile,
                hitStartSpeed,
                hitTexture,
                hitStartSize,
                hitBurstCount,
                deathExplosionStartSpeed,
                deathExplosionTexture,
                deathExplosionStartSize,
                deathExplosionBurstCount,
                deathFlashStartSpeed,
                deathFlashTexture,
                deathFlashStartSize,
                deathFlashBurstCount,
                current.ExplosiveImpactRangeMultiplier);
        }

        private static void DrawParticleAsset(
            string label,
            string path,
            ref float startSpeed,
            ref float startSize,
            ref int burstCount,
            ref Texture2D texture)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("粒子预制体", asset, typeof(Object), false);
            }

            EditorGUILayout.SelectableLabel(
                path,
                EditorStyles.miniLabel,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            startSpeed = Mathf.Max(0f, EditorGUILayout.FloatField(
                "初速度", startSpeed));
            startSize = Mathf.Max(0f, EditorGUILayout.FloatField(
                "初始大小", startSize));
            burstCount = Mathf.Max(0, EditorGUILayout.IntField(
                "Burst 数量", burstCount));
            texture = (Texture2D)EditorGUILayout.ObjectField(
                "贴图覆盖（留空使用原贴图）",
                texture,
                typeof(Texture2D),
                false);
        }

        private void SaveAs(AircraftVisualDebugRuntime runtime)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "另存为飞机视觉配置",
                "AircraftVisualDebugSettings",
                "asset",
                "选择飞机视觉配置位置");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            AircraftVisualDebugSettings newSettings =
                CreateInstance<AircraftVisualDebugSettings>();
            newSettings.SetValues(draft.Value);
            AssetDatabase.CreateAsset(newSettings, path);
            AssetDatabase.SaveAssets();
            settingsTarget = newSettings;
            draft.Load(newSettings.GetValues());
            runtime.SetDefaultSettings(newSettings);
            runtime.SetSettings(draft.Value);
        }

        private void SaveToTarget(AircraftVisualDebugSettings target)
        {
            Undo.RecordObject(target, "保存飞机视觉配置");
            target.SetValues(draft.Value);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            draft.Load(target.GetValues());
        }
    }
}
