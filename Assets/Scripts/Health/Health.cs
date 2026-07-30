using System;
using UnityEngine;

public sealed class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField, Min(1f)]
    private float maxHealth = 150f;

    [SerializeField, Min(0f)]
    private float currentHealth = 150f;

    public float MaxHealth =>
        maxHealth;

    public float CurrentHealth =>
        currentHealth;

    public bool IsDead =>
        currentHealth <= 0f;

    public float NormalizedHealth
    {
        get
        {
            if (maxHealth <= 0f)
            {
                return 0f;
            }

            return currentHealth / maxHealth;
        }
    }

    /// <summary>
    /// 血量变化时触发。
    /// 参数是0到1之间的血量比例。
    /// </summary>
    public event Action<float> HealthChanged;

    /// <summary>
    /// 受到伤害且实际扣除了生命值时触发。
    /// 参数是攻击结算传入的原始伤害值，不会因目标剩余生命而截断。
    /// </summary>
    public event Action<float> Damaged;

    /// <summary>
    /// 血量第一次从大于0降到0时触发。
    /// </summary>
    public event Action Died;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);

        currentHealth = Mathf.Clamp(
            currentHealth,
            0f,
            maxHealth);
    }

    /// <summary>
    /// 使用指定最大生命值初始化，并恢复为满血。
    /// 生成飞机时会调用这个方法。
    /// </summary>
    public void Initialize(float newMaxHealth)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        currentHealth = maxHealth;

        HealthChanged?.Invoke(NormalizedHealth);
    }

    /// <summary>
    /// 更新最大生命并恢复满血。全局节奏调试修改生命倍率时使用。
    /// </summary>
    public void SetMaximumHealthAndFill(float newMaxHealth)
    {
        Initialize(newMaxHealth);
    }

    public void DecreaseHealth(float amount)
    {
        if (amount <= 0f || IsDead)
        {
            return;
        }

        float healthBeforeDamage = currentHealth;
        SetHealth(currentHealth - amount);

        if (!Mathf.Approximately(
                healthBeforeDamage,
                currentHealth))
        {
            Damaged?.Invoke(amount);
        }
    }

    public void IncreaseHealth(float amount)
    {
        if (amount <= 0f ||
            currentHealth >= maxHealth)
        {
            return;
        }

        SetHealth(currentHealth + amount);
    }

    public void SetHealth(float value)
    {
        float newHealth = Mathf.Clamp(
            value,
            0f,
            maxHealth);

        if (Mathf.Approximately(
                newHealth,
                currentHealth))
        {
            return;
        }

        bool wasAlive = currentHealth > 0f;

        currentHealth = newHealth;

        HealthChanged?.Invoke(NormalizedHealth);

        if (wasAlive && IsDead)
        {
            Died?.Invoke();
        }
    }

    public void ResetHealth()
    {
        SetHealth(maxHealth);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);

        currentHealth = Mathf.Clamp(
            currentHealth,
            0f,
            maxHealth);
    }
#endif
}
