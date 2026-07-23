using UnityEngine;

public class HealthDebug : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Health targetHealth;

    [Header("Test Settings")]
    [SerializeField, Min(1f)]
    private float changeAmount = 25f;

    [ContextMenu("Test/Decrease Health")]
    private void TestDecreaseHealth()
    {
        if (targetHealth == null)
        {
            Debug.LogWarning("HealthDebug 没有绑定 Target Health。", this);
            return;
        }

        targetHealth.DecreaseHealth(changeAmount);
    }

    [ContextMenu("Test/Increase Health")]
    private void TestIncreaseHealth()
    {
        if (targetHealth == null)
        {
            Debug.LogWarning("HealthDebug 没有绑定 Target Health。", this);
            return;
        }

        targetHealth.IncreaseHealth(changeAmount);
    }

    [ContextMenu("Test/Set Health To Zero")]
    private void TestSetHealthToZero()
    {
        if (targetHealth == null)
        {
            Debug.LogWarning("HealthDebug 没有绑定 Target Health。", this);
            return;
        }

        targetHealth.SetHealth(0f);
    }

    [ContextMenu("Test/Reset Health")]
    private void TestResetHealth()
    {
        if (targetHealth == null)
        {
            Debug.LogWarning("HealthDebug 没有绑定 Target Health。", this);
            return;
        }

        targetHealth.ResetHealth();
    }

    private void Reset()
    {
        targetHealth = GetComponent<Health>();
    }
}