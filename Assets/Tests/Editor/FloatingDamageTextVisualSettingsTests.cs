using BackpackHero.Debugging;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class FloatingDamageTextVisualSettingsTests
{
    private const string DefaultSettingsPath =
        "Assets/Resources/FloatingDamageText/" +
        "FloatingDamageTextDebugSettings.asset";

    [Test]
    public void Values_ClampFontSizeAndRoundTripFont()
    {
        FloatingDamageTextDebugSettings asset =
            ScriptableObject.CreateInstance<FloatingDamageTextDebugSettings>();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Fonts/BebasNeue-Regular SDF.asset");
        try
        {
            asset.SetValues(new FloatingDamageTextVisualSettings(font, -2f));

            FloatingDamageTextVisualSettings values = asset.GetValues();
            Assert.That(values.Font, Is.EqualTo(font));
            Assert.That(values.FontSize, Is.EqualTo(
                FloatingDamageTextVisualSettings.MinimumFontSize));
        }
        finally
        {
            Object.DestroyImmediate(asset);
        }
    }

    [Test]
    public void DefaultResource_UsesCurrentFloatingTextBaseline()
    {
        FloatingDamageTextDebugSettings asset =
            AssetDatabase.LoadAssetAtPath<FloatingDamageTextDebugSettings>(
                DefaultSettingsPath);
        Assert.That(asset, Is.Not.Null);

        FloatingDamageTextVisualSettings values = asset.GetValues();
        Assert.That(values.Font, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(values.Font), Is.EqualTo(
            "Assets/TextMesh Pro/Fonts/BebasNeue-Regular SDF.asset"));
        Assert.That(values.FontSize, Is.EqualTo(4f));
    }

    [Test]
    public void Values_EqualityIncludesFontAndFontSize()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Fonts/BebasNeue-Regular SDF.asset");
        FloatingDamageTextVisualSettings first = new(font, 4f);

        Assert.That(first.Equals(new FloatingDamageTextVisualSettings(font, 4f)),
            Is.True);
        Assert.That(first.Equals(new FloatingDamageTextVisualSettings(font, 5f)),
            Is.False);
    }
}
