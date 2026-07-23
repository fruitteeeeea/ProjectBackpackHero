using UnityEngine;
using UnityEngine.UI;

public sealed class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Health targetHealth;

    [SerializeField]
    private Image fillImage;

    private Health subscribedHealth;

    private void OnEnable()
    {
        SubscribeToTarget();
    }

    private void OnDisable()
    {
        UnsubscribeFromTarget();
    }

    /// <summary>
    /// 运行时设置血条所显示的Health。
    /// 生成飞机后会调用这个方法。
    /// </summary>
    public void SetTarget(Health health)
    {
        if (targetHealth == health &&
            subscribedHealth == health)
        {
            if (targetHealth != null)
            {
                UpdateFill(
                    targetHealth.NormalizedHealth);
            }

            return;
        }

        UnsubscribeFromTarget();

        targetHealth = health;

        if (isActiveAndEnabled)
        {
            SubscribeToTarget();
        }
        else if (targetHealth != null)
        {
            UpdateFill(
                targetHealth.NormalizedHealth);
        }
    }

    private void SubscribeToTarget()
    {
        UnsubscribeFromTarget();

        if (targetHealth == null)
        {
            return;
        }

        subscribedHealth = targetHealth;
        subscribedHealth.HealthChanged += UpdateFill;

        UpdateFill(
            subscribedHealth.NormalizedHealth);
    }

    private void UnsubscribeFromTarget()
    {
        if (subscribedHealth == null)
        {
            return;
        }

        subscribedHealth.HealthChanged -= UpdateFill;
        subscribedHealth = null;
    }

    private void UpdateFill(
        float normalizedHealth)
    {
        if (fillImage == null)
        {
            return;
        }

        fillImage.fillAmount =
            Mathf.Clamp01(normalizedHealth);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount =
                Mathf.Clamp01(
                    fillImage.fillAmount);
        }
    }
#endif
}