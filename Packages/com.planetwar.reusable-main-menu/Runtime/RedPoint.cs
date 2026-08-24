using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared marker for menu notification dots.
///
/// Notification rules have not been migrated yet, so dots are globally hidden by
/// default. Their GameObjects and prefab bindings remain intact and can be
/// restored later through <see cref="SetVisible"/>.
/// </summary>
public class RedPoint : MonoBehaviour
{
    static readonly HashSet<RedPoint> Instances = new HashSet<RedPoint>();
    static bool visible;

    /// <summary>Whether red-point visuals are globally allowed to display.</summary>
    public static bool Visible => visible;

    /// <summary>
    /// Shows or hides every red point that was active before global hiding.
    /// Inactive prefab dots stay inactive when visibility is restored.
    /// </summary>
    public static void SetVisible(bool value)
    {
        if (visible == value) return;
        visible = value;

        foreach (RedPoint point in new List<RedPoint>(Instances))
        {
            if (point == null)
            {
                Instances.Remove(point);
                continue;
            }

            point.gameObject.SetActive(value);
        }
    }

    void OnEnable()
    {
        Instances.Add(this);
        if (!visible) gameObject.SetActive(false);
    }

    void OnDestroy() => Instances.Remove(this);
}
