using UnityEditor;
using UnityEngine;

internal sealed class DebugValueWindow : ScriptableObject
{
    private int speedInput;
    private int healthInput;
    private int damageInput;
    private DebugValueController lastTarget;

    private void SyncInputsIfTargetChanged()
    {
        var target = DebugValueRuntimeBridge.Current;
        if (target == lastTarget)
        {
            return;
        }

        lastTarget = target;
        CopyValuesFromTarget(target);
    }

    private void CopyValuesFromTarget(DebugValueController target)
    {
        if (target == null)
        {
            return;
        }

        speedInput = target.Speed;
        healthInput = target.Health;
        damageInput = target.Damage;
    }

    internal void DrawTab()
    {
        SyncInputsIfTargetChanged();
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("运行时数据调试", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("请先进入 Play Mode。窗口将在运行时实例启动后自动打开。", MessageType.Info);
            return;
        }

        var target = DebugValueRuntimeBridge.Current;
        if (target == null)
        {
            EditorGUILayout.HelpBox("DebugValueController 尚未启动，或目标已因场景切换而丢失。", MessageType.Warning);
            return;
        }

        EditorGUILayout.ObjectField("Target", target, typeof(DebugValueController), true);
        EditorGUILayout.Space(8f);

        DrawValueControl("Speed", target.Speed, ref speedInput, 1, DebugValueRuntimeBridge.SetSpeed);
        DrawValueControl("Health", target.Health, ref healthInput, 10, DebugValueRuntimeBridge.SetHealth);
        DrawValueControl("Damage", target.Damage, ref damageInput, 1, DebugValueRuntimeBridge.SetDamage);

        EditorGUILayout.Space(12f);
        if (GUILayout.Button("Reset Values", GUILayout.Height(30f)))
        {
            DebugValueRuntimeBridge.ResetValues();
            CopyValuesFromTarget(DebugValueRuntimeBridge.Current);
        }
    }

    private static void DrawValueControl(
        string label,
        int currentValue,
        ref int input,
        int step,
        System.Action<int> setter)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"当前 {label}：{currentValue}");
        input = EditorGUILayout.IntField("新数值", input);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button($"-{step}"))
        {
            setter(currentValue - step);
            input = Mathf.Max(0, currentValue - step);
        }

        if (GUILayout.Button("应用"))
        {
            setter(input);
            input = Mathf.Max(0, input);
        }

        if (GUILayout.Button($"+{step}"))
        {
            setter(currentValue + step);
            input = currentValue + step;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }
}
