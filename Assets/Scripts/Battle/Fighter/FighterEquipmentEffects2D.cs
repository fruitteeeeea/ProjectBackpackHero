using System;
using System.Collections.Generic;
using BackpackPrototype;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 管理一架飞机从相邻装备获得的运行时效果。
    /// 当前支持子弹效果；所有装备子弹共享发射闸门以保证错峰。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FighterEquipmentEffects2D : MonoBehaviour
    {
        public const float SharedProjectileInterval = 0.1f;

        private sealed class ProjectileEffectRuntime
        {
            public ProjectileEffectRuntime(
                BattleAttack2D newProjectilePrefab,
                float newCooldown)
            {
                ProjectilePrefab = newProjectilePrefab;
                Cooldown = Mathf.Max(
                    ProjectileEquipmentEffectDefinition
                        .MinimumCooldown,
                    newCooldown);
            }

            public BattleAttack2D ProjectilePrefab { get; }
            public float Cooldown { get; }
            public float RemainingCooldown { get; set; }
        }

        private readonly List<ProjectileEffectRuntime>
            projectileEffects = new();

        private readonly HashSet<LaserLinkEquipmentEffectDefinition>
            laserLinkEffects = new();

        private float remainingSharedCooldown;
        private int nextProjectileEffectIndex;

        public event Action<BattleAttack2D> ProjectileShotRequested;

        public int ProjectileEffectCount =>
            projectileEffects.Count;

        public IReadOnlyCollection<LaserLinkEquipmentEffectDefinition>
            LaserLinkEffects => laserLinkEffects;

        public bool HasLaserLinkEffect(
            LaserLinkEquipmentEffectDefinition effect) =>
            effect != null && laserLinkEffects.Contains(effect);

        /// <summary>
        /// 以传入顺序建立效果，因此重复装备会保留各自的独立状态。
        /// </summary>
        public void Configure(
            IEnumerable<EquipmentEffectDefinition> effects)
        {
            projectileEffects.Clear();
            laserLinkEffects.Clear();

            if (effects != null)
            {
                foreach (EquipmentEffectDefinition effect in effects)
                {
                    if (effect is LaserLinkEquipmentEffectDefinition laserLink &&
                        laserLink.LaserAttackPrefab != null)
                    {
                        laserLinkEffects.Add(laserLink);
                        continue;
                    }

                    if (effect is not
                        ProjectileEquipmentEffectDefinition projectile ||
                        projectile.ProjectilePrefab == null)
                    {
                        continue;
                    }

                    projectileEffects.Add(
                        new ProjectileEffectRuntime(
                            projectile.ProjectilePrefab,
                            projectile.Cooldown));
                }
            }

            ResetCooldowns();
        }

        /// <summary>
        /// 每帧最多请求一颗装备子弹。已就绪效果从上次发射项之后轮询。
        /// </summary>
        public bool Tick(float deltaTime)
        {
            if (projectileEffects.Count == 0)
            {
                return false;
            }

            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            remainingSharedCooldown = Mathf.Max(
                0f,
                remainingSharedCooldown - safeDeltaTime);

            foreach (ProjectileEffectRuntime effect in projectileEffects)
            {
                effect.RemainingCooldown = Mathf.Max(
                    0f,
                    effect.RemainingCooldown - safeDeltaTime);
            }

            if (remainingSharedCooldown > 0f)
            {
                return false;
            }

            for (int offset = 0;
                 offset < projectileEffects.Count;
                 offset++)
            {
                int index =
                    (nextProjectileEffectIndex + offset) %
                    projectileEffects.Count;
                ProjectileEffectRuntime effect =
                    projectileEffects[index];

                if (effect.RemainingCooldown > 0f)
                {
                    continue;
                }

                effect.RemainingCooldown = effect.Cooldown;
                remainingSharedCooldown =
                    SharedProjectileInterval;
                nextProjectileEffectIndex =
                    (index + 1) % projectileEffects.Count;
                ProjectileShotRequested?.Invoke(
                    effect.ProjectilePrefab);
                return true;
            }

            return false;
        }

        public void ResetCooldowns()
        {
            remainingSharedCooldown = 0f;
            nextProjectileEffectIndex = 0;

            foreach (ProjectileEffectRuntime effect in projectileEffects)
            {
                effect.RemainingCooldown = 0f;
            }
        }
    }
}
