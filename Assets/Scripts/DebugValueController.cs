using TMPro;
using UnityEngine;

public class DebugValueController : MonoBehaviour
{
    [Header("UI Labels")]
    [SerializeField] private TMP_Text speedLabel;
    [SerializeField] private TMP_Text healthLabel;
    [SerializeField] private TMP_Text damageLabel;

    [Header("Runtime Values")]
    [SerializeField] private int speed = 10;
    [SerializeField] private int health = 100;
    [SerializeField] private int damage = 20;

    public int Speed => speed;
    public int Health => health;
    public int Damage => damage;

    private void Awake()
    {
        RefreshLabels();
    }

    private void OnEnable()
    {
        DebugValueRuntimeBridge.Register(this);
    }

    private void OnDisable()
    {
        DebugValueRuntimeBridge.Unregister(this);
    }

    private void OnValidate()
    {
        RefreshLabels();
    }

    public void SetSpeed(int value)
    {
        speed = Mathf.Max(0, value);
        RefreshLabels();
    }

    public void SetHealth(int value)
    {
        health = Mathf.Max(0, value);
        RefreshLabels();
    }

    public void SetDamage(int value)
    {
        damage = Mathf.Max(0, value);
        RefreshLabels();
    }

    public void ResetValues()
    {
        speed = 10;
        health = 100;
        damage = 20;

        RefreshLabels();
    }

    private void RefreshLabels()
    {
        if (speedLabel != null)
        {
            speedLabel.text = $"Speed: {speed}";
        }

        if (healthLabel != null)
        {
            healthLabel.text = $"Health: {health}";
        }

        if (damageLabel != null)
        {
            damageLabel.text = $"Damage: {damage}";
        }
    }
}
