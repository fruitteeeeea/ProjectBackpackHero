using System.Collections.Generic;
using UnityEngine;

namespace BackpackHero.Battle
{
    [DisallowMultipleComponent]
    public sealed class FighterLaserLink2D : MonoBehaviour
    {
        private const int MaximumLinkedTargets = 2;

        private readonly List<Fighter2D> candidates = new();
        private Fighter2D fighter;
        private FighterCombat2D combat;
        private BattleAttack2D attackPrefab;

        private void Awake()
        {
            fighter = GetComponent<Fighter2D>();
            combat = GetComponent<FighterCombat2D>();
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

        public void Configure(BattleAttack2D newAttackPrefab)
        {
            attackPrefab = newAttackPrefab;
        }

        private void HandleDefaultAttackFired()
        {
            if (attackPrefab == null || fighter?.Definition == null || combat == null)
            {
                return;
            }

            candidates.Clear();
            foreach (Fighter2D candidate in FindObjectsByType<Fighter2D>(
                         FindObjectsInactive.Exclude,
                         FindObjectsSortMode.None))
            {
                if (candidate != fighter && candidate.IsAlive &&
                    candidate.Faction == fighter.Faction &&
                    candidate.Definition == fighter.Definition)
                {
                    candidates.Add(candidate);
                }
            }

            int targetCount = Mathf.Min(MaximumLinkedTargets, candidates.Count);
            for (int index = 0; index < targetCount; index++)
            {
                int chosen = Random.Range(index, candidates.Count);
                (candidates[index], candidates[chosen]) =
                    (candidates[chosen], candidates[index]);
                combat.FireAttackAtPoint(
                    attackPrefab,
                    candidates[index].transform.position);
            }
        }
    }
}
