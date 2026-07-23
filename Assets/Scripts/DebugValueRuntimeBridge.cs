using System;

/// <summary>
/// Runtime-safe access point used by debugging tools. This class deliberately
/// has no UnityEditor dependency, so it can also be included in player builds.
/// </summary>
public static class DebugValueRuntimeBridge
{
    private static DebugValueController current;

    public static event Action TargetAvailable;
    public static event Action TargetUnavailable;

    public static bool HasTarget => current != null;

    public static DebugValueController Current
    {
        get
        {
            if (current == null)
            {
                current = null;
            }

            return current;
        }
    }

    public static void Register(DebugValueController controller)
    {
        if (controller == null || current == controller)
        {
            return;
        }

        current = controller;
        TargetAvailable?.Invoke();
    }

    public static void Unregister(DebugValueController controller)
    {
        if (controller == null || current != controller)
        {
            return;
        }

        current = null;
        TargetUnavailable?.Invoke();
    }

    public static void SetSpeed(int value) => Current?.SetSpeed(value);
    public static void SetHealth(int value) => Current?.SetHealth(value);
    public static void SetDamage(int value) => Current?.SetDamage(value);
    public static void ResetValues() => Current?.ResetValues();
}
