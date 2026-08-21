using BackpackHero.Battle;
using NUnit.Framework;
using UnityEngine;

public sealed class BackpackHealthLockTests
{
    [Test]
    public void LockedBackpack_RejectsDamageUntilUnlocked()
    {
        GameObject backpackObject = new("Backpack");
        GameObject hurtBoxObject = new("HurtBox");

        try
        {
            backpackObject.SetActive(false);
            Health health = backpackObject.AddComponent<Health>();
            FactionMember faction =
                backpackObject.AddComponent<FactionMember>();
            BattleBackpackTarget2D backpack =
                backpackObject.AddComponent<BattleBackpackTarget2D>();

            hurtBoxObject.transform.SetParent(
                backpackObject.transform,
                false);
            hurtBoxObject.AddComponent<BoxCollider2D>().isTrigger = true;
            HurtBox2D hurtBox =
                hurtBoxObject.AddComponent<HurtBox2D>();

            backpackObject.SetActive(true);
            faction.SetFaction(BattleFaction.Enemy);
            hurtBox.ConfigureLayer(BattleFaction.Enemy);
            health.Initialize(10f);

            backpack.SetHealthLocked(true);

            Assert.That(backpack.IsHealthLocked, Is.True);
            Assert.That(hurtBox.ReceiveHit(
                3f, BattleFaction.Player), Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(10f));

            backpack.SetHealthLocked(false);

            Assert.That(hurtBox.ReceiveHit(
                3f, BattleFaction.Player), Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(7f));
        }
        finally
        {
            Object.DestroyImmediate(backpackObject);
            Object.DestroyImmediate(hurtBoxObject);
        }
    }
}
