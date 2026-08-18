using System.Collections.Generic;
using BackpackPrototype;
using UnityEngine;

namespace BackpackHero.Battle
{
    [DisallowMultipleComponent]
    public sealed class FighterEquipmentLaserLink2D : MonoBehaviour
    {
        private const int MaximumLinkedTargets = 6;

        private readonly List<Fighter2D> candidates = new();
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
        }

        private void HandleDefaultAttackFired()
        {
            if (fighter == null || combat == null || equipmentEffects == null)
            {
                return;
            }

            foreach ((LaserLinkEquipmentEffectDefinition effect, ItemInstance item) in
                     equipmentEffects.LaserLinkEffects)
            {
                FireLinks(effect, item);
            }
        }

        private void FireLinks(LaserLinkEquipmentEffectDefinition effect, ItemInstance item)
        {
            if (effect?.LaserAttackPrefab == null)
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

            int targetCount = Mathf.Min(MaximumLinkedTargets, candidates.Count);
            for (int index = 0; index < targetCount; index++)
            {
                combat.FireAttackAtPoint(
                    effect.LaserAttackPrefab,
                    candidates[index].transform.position,
                    ProjectileVisualSource.Equipment,
                    item);
            }
        }
    }
}
