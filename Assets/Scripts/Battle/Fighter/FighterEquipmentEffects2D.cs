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
                float newCooldown,
                ItemInstance newItem,
                float equipmentItemModifier)
            {
                ProjectilePrefab = newProjectilePrefab;
                Cooldown = newCooldown / Mathf.Max(
                    ItemData.MinimumEquipmentItemModifier,
                    equipmentItemModifier);
                Item = newItem;
                EquipmentItemModifier = Mathf.Max(
                    ItemData.MinimumEquipmentItemModifier,
                    equipmentItemModifier);
            }

            public BattleAttack2D ProjectilePrefab { get; }
            public float Cooldown { get; }
            public ItemInstance Item { get; }
            public float EquipmentItemModifier { get; }
            public float RemainingCooldown { get; set; }
        }

        private readonly List<ProjectileEffectRuntime>
            projectileEffects = new();

        private readonly List<(LaserLinkEquipmentEffectDefinition Effect, ItemInstance Item, float EquipmentItemModifier)>
            laserLinkEffects = new();

        private float remainingSharedCooldown;
        private int nextProjectileEffectIndex;

        /// <summary>兼容既有调用方的装备子弹请求事件。</summary>
        public event Action<BattleAttack2D> ProjectileShotRequested;

        /// <summary>携带触发该攻击的装备实例，供伤害归因使用。</summary>
        public event Action<BattleAttack2D, ItemInstance, float> ProjectileShotRequestedWithSource;

        public int ProjectileEffectCount =>
            projectileEffects.Count;

        public IReadOnlyCollection<(LaserLinkEquipmentEffectDefinition Effect, ItemInstance Item, float EquipmentItemModifier)>
            LaserLinkEffects => laserLinkEffects;

        public bool HasLaserLinkEffect(
            LaserLinkEquipmentEffectDefinition effect) =>
            effect != null && laserLinkEffects.Exists(x => x.Effect == effect);

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
                        laserLinkEffects.Add((laserLink, null, 1f));
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
                            projectile.Cooldown,
                            null,
                            1f));
                }
            }

            ResetCooldowns();
        }

        public void Configure(
            IEnumerable<(EquipmentEffectDefinition Effect, ItemInstance Item)> effects,
            float equipmentItemModifier = 1f)
        {
            projectileEffects.Clear();
            laserLinkEffects.Clear();
            if (effects != null)
                foreach ((EquipmentEffectDefinition effect, ItemInstance item) in effects)
                    if (effect is ProjectileEquipmentEffectDefinition projectile && projectile.ProjectilePrefab != null)
                        projectileEffects.Add(new ProjectileEffectRuntime(
                            projectile.ProjectilePrefab,
                            item != null
                                ? item.GetEquipmentEffectCooldown(projectile)
                                : projectile.Cooldown,
                            item,
                            equipmentItemModifier));
                    else if (effect is LaserLinkEquipmentEffectDefinition laser && laser.LaserAttackPrefab != null)
                        laserLinkEffects.Add((
                            laser,
                            item,
                            Mathf.Max(
                                ItemData.MinimumEquipmentItemModifier,
                                equipmentItemModifier)));
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
                ProjectileShotRequested?.Invoke(effect.ProjectilePrefab);
                ProjectileShotRequestedWithSource?.Invoke(
                    effect.ProjectilePrefab,
                    effect.Item,
                    effect.EquipmentItemModifier);
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
