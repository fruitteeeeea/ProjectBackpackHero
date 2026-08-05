using System;
using UnityEditor;
using UnityEngine;

internal readonly struct DebugValueDraft : IEquatable<DebugValueDraft>
{
    public DebugValueDraft(int speed, int health, int damage)
    {
        Speed = speed;
        Health = health;
        Damage = damage;
    }

    public int Speed { get; }
    public int Health { get; }
    public int Damage { get; }

    public bool Equals(DebugValueDraft other) =>
        Speed == other.Speed && Health == other.Health && Damage == other.Damage;
}

/// <summary>程序测试参数页：草稿与运行时、资产保存明确分离。</summary>
internal sealed class DebugValueWindow : ScriptableObject
{
    private const string PresetDirectory = "Assets/DebugPresets";

    private readonly DebugDraft<DebugValueDraft> draft = new();
    private DebugValueTestPreset preset;
    private DebugValueController lastTarget;
    private string validationMessage;

    internal void DrawTab()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("运行时数值调试", EditorStyles.boldLabel);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("请先进入 Play Mode。窗口将在运行时实例启动后自动打开。", MessageType.Info);
            return;
        }

        DebugValueController target = DebugValueRuntimeBridge.Current;
        if (target == null)
        {
            EditorGUILayout.HelpBox("DebugValueController 尚未启动，或目标已因场景切换而丢失。", MessageType.Warning);
            return;
        }

        SyncTarget(target);
        DrawPresetTarget(target);
        DrawDraftFields();
        DrawValidation();
        DrawActions(target);
    }

    private void SyncTarget(DebugValueController target)
    {
        if (target == lastTarget)
        {
            return;
        }

        lastTarget = target;
        if (!draft.HasValue)
        {
            draft.Load(ReadTarget(target));
        }
    }

    private void DrawPresetTarget(DebugValueController target)
    {
        EditorGUILayout.Space(6f);
        DebugValueTestPreset nextPreset = (DebugValueTestPreset)EditorGUILayout.ObjectField(
            "测试预设", preset, typeof(DebugValueTestPreset), false);
        if (nextPreset != preset)
        {
            preset = nextPreset;
            draft.Load(preset != null ? ReadPreset(preset) : ReadTarget(target));
            validationMessage = null;
        }

        EditorGUILayout.LabelField(
            "保存目标",
            preset != null ? AssetDatabase.GetAssetPath(preset) : "未选择（请使用“另存为”创建预设）",
            EditorStyles.miniLabel);
        EditorGUILayout.LabelField("未保存修改", draft.IsDirty ? "是" : "否", EditorStyles.miniLabel);
    }

    private void DrawDraftFields()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("编辑草稿", EditorStyles.boldLabel);
        DebugValueDraft value = draft.Value;
        int speed = EditorGUILayout.IntField("Speed", value.Speed);
        int health = EditorGUILayout.IntField("Health", value.Health);
        int damage = EditorGUILayout.IntField("Damage", value.Damage);
        if (speed != value.Speed || health != value.Health || damage != value.Damage)
        {
            draft.Value = new DebugValueDraft(speed, health, damage);
            validationMessage = null;
        }
    }

    private void DrawValidation()
    {
        if (!string.IsNullOrEmpty(validationMessage))
        {
            EditorGUILayout.HelpBox(validationMessage, MessageType.Error);
        }
    }

    private void DrawActions(DebugValueController target)
    {
        EditorGUILayout.Space(10f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("应用到运行时"))
            {
                if (ValidateDraft())
                {
                    ApplyToRuntime(draft.Value);
                }
            }

            using (new EditorGUI.DisabledScope(preset == null || !draft.IsDirty))
            {
                if (GUILayout.Button("保存"))
                {
                    SaveToPreset(preset);
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
                draft.Load(preset != null ? ReadPreset(preset) : ReadTarget(target));
                validationMessage = null;
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("运行时当前值", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Speed", target.Speed.ToString());
        EditorGUILayout.LabelField("Health", target.Health.ToString());
        EditorGUILayout.LabelField("Damage", target.Damage.ToString());
    }

    private bool ValidateDraft()
    {
        DebugValueDraft value = draft.Value;
        if (value.Speed < 0 || value.Health < 0 || value.Damage < 0)
        {
            validationMessage = "Speed、Health 和 Damage 必须为非负整数。";
            return false;
        }

        validationMessage = null;
        return true;
    }

    private void ApplyToRuntime(DebugValueDraft value)
    {
        DebugValueRuntimeBridge.SetSpeed(value.Speed);
        DebugValueRuntimeBridge.SetHealth(value.Health);
        DebugValueRuntimeBridge.SetDamage(value.Damage);
    }

    private void SaveAs()
    {
        if (!ValidateDraft())
        {
            return;
        }

        EnsurePresetDirectory();
        string path = EditorUtility.SaveFilePanelInProject(
            "保存数值测试预设", "DebugValueTestPreset", "asset", "选择测试预设位置", PresetDirectory);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        DebugValueTestPreset newPreset = CreateInstance<DebugValueTestPreset>();
        newPreset.SetValues(draft.Value.Speed, draft.Value.Health, draft.Value.Damage);
        AssetDatabase.CreateAsset(newPreset, path);
        AssetDatabase.SaveAssets();
        preset = newPreset;
        draft.Load(ReadPreset(preset));
    }

    private void SaveToPreset(DebugValueTestPreset targetPreset)
    {
        if (targetPreset == null || !ValidateDraft())
        {
            return;
        }

        Undo.RecordObject(targetPreset, "保存数值测试预设");
        targetPreset.SetValues(draft.Value.Speed, draft.Value.Health, draft.Value.Damage);
        EditorUtility.SetDirty(targetPreset);
        AssetDatabase.SaveAssets();
        draft.Load(ReadPreset(targetPreset));
    }

    private static void EnsurePresetDirectory()
    {
        if (!AssetDatabase.IsValidFolder(PresetDirectory))
        {
            AssetDatabase.CreateFolder("Assets", "DebugPresets");
        }
    }

    private static DebugValueDraft ReadTarget(DebugValueController target) =>
        new(target.Speed, target.Health, target.Damage);

    private static DebugValueDraft ReadPreset(DebugValueTestPreset source) =>
        new(source.Speed, source.Health, source.Damage);
}
