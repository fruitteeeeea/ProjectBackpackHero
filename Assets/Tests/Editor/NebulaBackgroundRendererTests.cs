using BackpackHero.Background;
using NUnit.Framework;
using UnityEditor;

public sealed class NebulaBackgroundRendererTests
{
    private const string RendererPath =
        "Assets/Scripts/Background/NebulaBackgroundRenderer.cs";

    [Test]
    public void OvertimeVisual_TransitionsOnlyTheNebulaColor()
    {
        MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(RendererPath);

        Assert.That(script, Is.Not.Null);
        string source = script.text;
        StringAssert.Contains("runtimeMaterial.SetColor(ColorBId", source);
        StringAssert.DoesNotContain("Shader.PropertyToID(\"_Speed\")", source);
        StringAssert.DoesNotContain("SetFloat(SpeedId", source);
        StringAssert.DoesNotContain("overtimeSpeedTransitionDuration", source);
        Assert.That(typeof(NebulaBackgroundRenderer).GetField(
            "overtimeSpeedTransitionDuration",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic), Is.Null);
    }
}
