using UnityEngine;

/// <summary>程序测试面板中数值调试页使用的可保存测试预设。</summary>
[CreateAssetMenu(fileName = "DebugValueTestPreset", menuName = "Debug/Debug Value Test Preset")]
public sealed class DebugValueTestPreset : ScriptableObject
{
    [SerializeField, Min(0)] private int speed = 10;
    [SerializeField, Min(0)] private int health = 100;
    [SerializeField, Min(0)] private int damage = 20;

    public int Speed => speed;
    public int Health => health;
    public int Damage => damage;

    public void SetValues(int newSpeed, int newHealth, int newDamage)
    {
        speed = Mathf.Max(0, newSpeed);
        health = Mathf.Max(0, newHealth);
        damage = Mathf.Max(0, newDamage);
    }
}
