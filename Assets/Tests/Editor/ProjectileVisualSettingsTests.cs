using BackpackHero.Battle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ProjectileVisualSettingsTests
{
    private GameObject createdObject;

    [TearDown]
    public void TearDown()
    {
        if (createdObject != null)
        {
            Object.DestroyImmediate(createdObject);
        }
    }

    [Test]
    public void DefaultAsset_ContainsRequestedFactionAndTrailValues()
    {
        ProjectileVisualSettings settings = LoadSettings();

        Assert.That(
            settings.GetFactionColor(BattleFaction.Player),
            Is.EqualTo(new Color(0.55f, 0.85f, 1f, 1f)));
        Assert.That(
            settings.GetFactionColor(BattleFaction.Enemy),
            Is.EqualTo(new Color(1f, 0.55f, 0.55f, 1f)));
        Assert.That(
            settings.GetTrailDuration(
                ProjectileVisualSource.FighterDefault),
            Is.EqualTo(0.07f));
        Assert.That(
            settings.GetTrailDuration(
                ProjectileVisualSource.Equipment),
            Is.EqualTo(0.14f));
        Assert.That(
            settings.GetTrailDuration(
                ProjectileVisualSource.OvertimePenalty),
            Is.EqualTo(0.2f));
        Assert.That(settings.GetProjectileColor(
            BattleFaction.Player,
            ProjectileVisualSource.OvertimePenalty),
            Is.EqualTo(new Color(1f, 0.1f, 0.1f, 1f)));

        Gradient equipmentGradient = settings.CreateTrailGradient(
            BattleFaction.Player,
            ProjectileVisualSource.Equipment);

        Assert.That(equipmentGradient.colorKeys[0].color,
            Is.EqualTo(new Color(
                0.8666667f,
                0.7882353f,
                0.23137255f,
                1f)));
        Assert.That(equipmentGradient.colorKeys[1].time,
            Is.EqualTo(0.35f));
        Assert.That(equipmentGradient.colorKeys[1].color,
            Is.EqualTo(settings.GetFactionColor(BattleFaction.Player)));
        Assert.That(equipmentGradient.alphaKeys[0].alpha,
            Is.EqualTo(0.49019608f));
        Assert.That(equipmentGradient.alphaKeys[1].alpha, Is.Zero);
    }

    [Test]
    public void Controller_AppliesFactionColorAndEquipmentTrail()
    {
        ProjectileVisualSettings settings = LoadSettings();
        createdObject = new GameObject("Projectile Visual Test");
        ProjectileVisualController2D controller =
            createdObject.AddComponent<ProjectileVisualController2D>();

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(createdObject.transform);
        SpriteRenderer spriteRenderer =
            visual.AddComponent<SpriteRenderer>();
        TrailRenderer trailRenderer =
            visual.AddComponent<TrailRenderer>();

        SerializedObject serializedController =
            new SerializedObject(controller);
        serializedController.FindProperty("settings")
            .objectReferenceValue = settings;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        controller.Apply(
            BattleFaction.Enemy,
            ProjectileVisualSource.Equipment);

        Assert.That(spriteRenderer.color,
            Is.EqualTo(settings.GetFactionColor(BattleFaction.Enemy)));
        Assert.That(trailRenderer.time, Is.EqualTo(0.14f));
        Assert.That(trailRenderer.widthCurve.keys.Length, Is.EqualTo(2));
        Assert.That(trailRenderer.colorGradient.colorKeys[0].color,
            Is.EqualTo(new Color(
                0.8666667f,
                0.7882353f,
                0.23137255f,
                1f)));
        Assert.That(trailRenderer.colorGradient.colorKeys[1].time,
            Is.EqualTo(0.35f));
        Assert.That(trailRenderer.colorGradient.colorKeys[1].color,
            Is.EqualTo(settings.GetFactionColor(BattleFaction.Enemy)));
    }

    [Test]
    public void LaunchContext_UsesDefaultVisualSourceUnlessSpecified()
    {
        BattleAttackLaunchContext defaultContext =
            BattleAttackLaunchContext.WithoutAimPoint(
                BattleFaction.Player,
                1f,
                1f,
                -1f,
                Vector2.zero,
                Vector2.up,
                Vector2.zero,
                Vector2.up);
        BattleAttackLaunchContext equipmentContext =
            BattleAttackLaunchContext.WithoutAimPoint(
                BattleFaction.Player,
                1f,
                1f,
                -1f,
                Vector2.zero,
                Vector2.up,
                Vector2.zero,
                Vector2.up,
                ProjectileVisualSource.Equipment);

        Assert.That(defaultContext.VisualSource,
            Is.EqualTo(ProjectileVisualSource.FighterDefault));
        Assert.That(equipmentContext.VisualSource,
            Is.EqualTo(ProjectileVisualSource.Equipment));
    }

    [Test]
    public void LaunchContext_CanDisableBackpackDamage()
    {
        BattleAttackLaunchContext blockedContext =
            BattleAttackLaunchContext.WithAimPoint(
                BattleFaction.Player,
                1f,
                1f,
                -1f,
                Vector2.zero,
                Vector2.up,
                Vector2.zero,
                Vector2.up,
                Vector2.right,
                canDamageBackpack: false);

        Assert.That(blockedContext.CanDamageBackpack,
            Is.False);
    }

    private static ProjectileVisualSettings LoadSettings()
    {
        ProjectileVisualSettings settings =
            AssetDatabase.LoadAssetAtPath<ProjectileVisualSettings>(
                "Assets/Data/Battle/ProjectileVisualSettings.asset");

        Assert.That(settings, Is.Not.Null);
        return settings;
    }
}
