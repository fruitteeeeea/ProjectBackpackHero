using BackpackHero.Debugging;
using BackpackPrototype;
using UnityEngine;

namespace BackpackHero.Battle
{
    [DisallowMultipleComponent]
    public sealed class FighterDeathExplosion2D : MonoBehaviour
    {
        private Fighter2D fighter;
        private BattleAttack2D attackPrefab;
        private bool resolved;

        private void Awake()
        {
            fighter = GetComponent<Fighter2D>();
            Health health = GetComponent<Health>();
            if (health != null)
            {
                health.Died += HandleDied;
            }
        }

        private void OnDestroy()
        {
            Health health = GetComponent<Health>();
            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        public void Configure(BattleAttack2D newAttackPrefab)
        {
            attackPrefab = newAttackPrefab;
            resolved = false;
        }

        private void HandleDied()
        {
            if (resolved || attackPrefab == null || fighter?.Definition == null)
            {
                return;
            }

            resolved = true;
            BattleAttack2D attack = Instantiate(
                attackPrefab,
                transform.position,
                Quaternion.identity);
            ProjectileImpactEffect2D impact =
                attack.GetComponent<ProjectileImpactEffect2D>();
            if (impact == null)
            {
                Debug.LogError(
                    $"{attackPrefab.name} is missing ProjectileImpactEffect2D.",
                    attackPrefab);
                Destroy(attack.gameObject);
                return;
            }

            float damage = fighter.Definition.ProjectileDamage *
                fighter.EffectiveDamageMultiplier *
                GamePacingDebugRuntime.GetProjectileDamageMultiplier(
                    fighter.Faction) *
                LevelDifficultyRuntime.GetProjectileDamageMultiplier(
                    fighter.Faction);
            impact.ResolveImpact(null, transform.position, fighter.Faction, damage,
                new BattleDamageSource(fighter.DamageSourceItem));
            Destroy(attack.gameObject);
        }
    }
}
