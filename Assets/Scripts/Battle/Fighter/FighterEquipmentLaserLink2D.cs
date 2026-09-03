using System.Collections.Generic;
using BackpackPrototype;
using UnityEngine;

namespace BackpackHero.Battle
{
    [DisallowMultipleComponent]
    public sealed class FighterEquipmentLaserLink2D : MonoBehaviour
    {

        private readonly List<Fighter2D> candidates = new();
        private readonly Dictionary<
            (LaserLinkEquipmentEffectDefinition Effect, ItemInstance Item),
            float> nextFireTimes = new();
        private Fighter2D fighter;
        private FighterCombat2D combat;
        private FighterEquipmentEffects2D equipmentEffects;

        private void Awake()
        {
            fighter = GetComponent<Fighter2D>();
            combat = GetComponent<FighterCombat2D>();
            equipmentEffects = GetComponent<FighterEquipmentEffects2D>();
        }

        private void OnEnable()
        {
            if (combat != null)
            {
                combat.DefaultAttackFired += HandleDefaultAttackFired;
            }
        }

        private void OnDisable()
        {
            if (combat != null)
            {
                combat.DefaultAttackFired -= HandleDefaultAttackFired;
            }

            nextFireTimes.Clear();
        }

        private void HandleDefaultAttackFired()
        {
            if (fighter == null || combat == null || equipmentEffects == null)
            {
                return;
            }

            foreach ((LaserLinkEquipmentEffectDefinition effect, ItemInstance item, float cooldownModifier, float projectileStatModifier, int maximumLinkedTargets) in
                     equipmentEffects.LaserLinkEffects)
            {
                FireLinks(effect, item, cooldownModifier, projectileStatModifier,
                    maximumLinkedTargets);
            }
        }

        private void FireLinks(
            LaserLinkEquipmentEffectDefinition effect,
            ItemInstance item,
            float cooldownModifier,
            float projectileStatModifier, int maximumLinkedTargets)
        {
            if (effect?.LaserAttackPrefab == null)
            {
                return;
            }

            var effectKey = (effect, item);
            if (nextFireTimes.TryGetValue(effectKey, out float nextFireTime) &&
                Time.time < nextFireTime)
            {
                return;
            }

            candidates.Clear();
            foreach (Fighter2D candidate in FindObjectsByType<Fighter2D>(
                         FindObjectsInactive.Exclude,
                         FindObjectsSortMode.None))
            {
                FighterEquipmentEffects2D candidateEffects =
                    candidate != null
                        ? candidate.GetComponent<FighterEquipmentEffects2D>()
                        : null;

                if (candidate != fighter && candidate != null &&
                    candidate.IsAlive && candidate.Faction == fighter.Faction &&
                    candidateEffects != null &&
                    candidateEffects.HasLaserLinkEffect(effect))
                {
                    candidates.Add(candidate);
                }
            }

            candidates.Sort((left, right) =>
            {
                float leftDistance = (left.transform.position - transform.position)
                    .sqrMagnitude;
                float rightDistance = (right.transform.position - transform.position)
                    .sqrMagnitude;
                int distanceComparison = rightDistance.CompareTo(leftDistance);
                return distanceComparison != 0
                    ? distanceComparison
                    : left.GetEntityId().CompareTo(right.GetEntityId());
            });

            int targetCount = Mathf.Min(maximumLinkedTargets, candidates.Count);
            if (targetCount == 0)
            {
                return;
            }

            float modifier = Mathf.Max(
                ItemData.MinimumEquipmentItemModifier,
                cooldownModifier);
            float effectiveCooldown = item != null
                ? item.GetEquipmentEffectCooldown(effect)
                : effect.Cooldown;
            nextFireTimes[effectKey] =
                Time.time + effectiveCooldown / modifier;
            for (int index = 0; index < targetCount; index++)
            {
                combat.FireAttackAtPoint(
                    effect.LaserAttackPrefab,
                    candidates[index].transform.position,
                    ProjectileVisualSource.Equipment,
                    item,
                    projectileStatModifier);
            }
        }
    }
}
