using BackpackHero.Battle;
using BackpackHero.Debugging;
using NUnit.Framework;
using UnityEngine;

public sealed class BackpackHealthBarOffsetTests
{
    [TestCase(BattleFaction.Player, 1.4f)]
    [TestCase(BattleFaction.Enemy, -1.4f)]
    public void RuntimeSettings_ApplyMirroredVerticalOffsetWithoutChangingHorizontalOffset(
        BattleFaction faction,
        float expectedY)
    {
        GameObject runtimeObject = null;
        GameObject backpackObject = new("Backpack");
        GameObject healthBarObject = new("HealthBar");
        BackpackVisualDebugRuntime runtime = BackpackVisualDebugRuntime.Instance;
        BackpackVisualSettings previousSettings = runtime != null
            ? runtime.Settings
            : BackpackVisualSettings.Default;

        try
        {
            if (runtime == null)
            {
                runtimeObject = new(nameof(BackpackVisualDebugRuntime));
                runtime = runtimeObject.AddComponent<BackpackVisualDebugRuntime>();
            }

            backpackObject.SetActive(false);
            backpackObject.transform.position = new Vector3(3f, 5f, 7f);
            backpackObject.AddComponent<Health>();
            FactionMember factionMember =
                backpackObject.AddComponent<FactionMember>();
            factionMember.SetFaction(faction);
            backpackObject.AddComponent<BattleBackpackTarget2D>();

            healthBarObject.transform.SetParent(backpackObject.transform, false);
            WorldSpaceHealthBarFollower2D follower =
                healthBarObject.AddComponent<WorldSpaceHealthBarFollower2D>();
            follower.SetWorldOffset(new Vector3(.25f, .5f, -.75f));

            backpackObject.SetActive(true);
            runtime.SetSettings(BackpackVisualSettings.Default
                .WithBackpackHealthBarVerticalOffset(1.4f));

            Assert.That(healthBarObject.transform.position,
                Is.EqualTo(new Vector3(3.25f, 5f + expectedY, 6.25f)));
        }
        finally
        {
            if (runtime != null && runtimeObject == null)
            {
                runtime.SetSettings(previousSettings);
            }

            Object.DestroyImmediate(backpackObject);
            if (runtimeObject != null)
            {
                Object.DestroyImmediate(runtimeObject);
            }
        }
    }
}
