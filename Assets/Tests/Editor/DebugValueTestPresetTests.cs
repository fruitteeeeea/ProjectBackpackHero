using NUnit.Framework;
using UnityEngine;

public sealed class DebugValueTestPresetTests
{
    [Test]
    public void SetValues_ClampsNegativeValuesToZero()
    {
        DebugValueTestPreset preset = ScriptableObject.CreateInstance<DebugValueTestPreset>();
        try
        {
            preset.SetValues(-1, -10, -100);

            Assert.That(preset.Speed, Is.Zero);
            Assert.That(preset.Health, Is.Zero);
            Assert.That(preset.Damage, Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(preset);
        }
    }

    [Test]
    public void SetValues_PreservesValidValues()
    {
        DebugValueTestPreset preset = ScriptableObject.CreateInstance<DebugValueTestPreset>();
        try
        {
            preset.SetValues(12, 250, 35);

            Assert.That(preset.Speed, Is.EqualTo(12));
            Assert.That(preset.Health, Is.EqualTo(250));
            Assert.That(preset.Damage, Is.EqualTo(35));
        }
        finally
        {
            Object.DestroyImmediate(preset);
        }
    }
}
