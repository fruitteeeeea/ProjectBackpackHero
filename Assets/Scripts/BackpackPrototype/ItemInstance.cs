using UnityEngine;
using BackpackHero.Debugging;

namespace BackpackPrototype
{
    public sealed class ItemInstance
    {
        public const int DefaultLevel = 1;
        /// <summary>
        /// The level of an item instance exists only for the current Sample Scene run.
        /// PlayerItemSystem stores the separate out-of-match progression level.
        /// </summary>
        public const int MaximumLevel = 3;

        public ItemInstance(
            string id,
            ItemData data,
            Vector2Int anchorCell,
            int level = DefaultLevel)
        {
            Id = id;
            Data = data;
            AnchorCell = anchorCell;
            Level = Mathf.Clamp(
                level,
                DefaultLevel,
                MaximumLevel);
        }

        public string Id { get; }
        public ItemData Data { get; }
        public Vector2Int AnchorCell { get; set; }
        public int Level { get; private set; }
        public bool CanUpgrade => Level < MaximumLevel;

        public bool IsCoolingDown { get; private set; }
        public float RemainingCooldown { get; private set; }
        private float activeCooldownDuration;
        private float? runtimeCooldownDuration;

        public float EffectiveCooldownDuration
        {
            get
            {
                if (runtimeCooldownDuration.HasValue)
                {
                    return runtimeCooldownDuration.Value;
                }

                if (Data == null)
                {
                    return 0f;
                }

                return Data.ItemType == ItemType.Aircraft
                    ? Data.GetAircraftCooldownDurationForLevel(Level)
                    : Data.CooldownDuration;
            }
        }

        public float CooldownProgress
        {
            get
            {
                if (!IsCoolingDown || activeCooldownDuration <= 0f)
                {
                    return 1f;
                }

                return Mathf.Clamp01(
                    1f -
                    RemainingCooldown /
                    activeCooldownDuration);
            }
        }

        public void BeginCooldown()
        {
            if (Data == null ||
                !Data.CanEnterCooldown)
            {
                ResetCooldown();
                return;
            }

            IsCoolingDown = true;
            activeCooldownDuration =
                CalculateActiveCooldownDuration();
            RemainingCooldown = activeCooldownDuration;
        }

        public float GetEquipmentEffectCooldown(
            EquipmentEffectDefinition effect)
        {
            if (effect == null)
            {
                return 0f;
            }

            float reduction =
                Data != null
                    ? Data.GetEquipmentEffectIntervalReductionForLevel(
                        Level)
                    : 0f;

            if (effect is
                ProjectileEquipmentEffectDefinition projectile)
            {
                return Mathf.Max(
                    ProjectileEquipmentEffectDefinition
                        .MinimumCooldown,
                    projectile.Cooldown - reduction);
            }

            if (effect is
                LaserLinkEquipmentEffectDefinition laser)
            {
                return Mathf.Max(
                    LaserLinkEquipmentEffectDefinition
                        .MinimumCooldown,
                    laser.Cooldown - reduction);
            }

            return 0f;
        }

        public bool TickCooldown(float deltaTime)
        {
            if (!IsCoolingDown ||
                Data == null ||
                !Data.CanEnterCooldown)
            {
                return false;
            }

            RemainingCooldown =
                Mathf.Max(
                    0f,
                    RemainingCooldown -
                    Mathf.Max(0f, deltaTime));

            if (RemainingCooldown > 0f)
            {
                return false;
            }

            IsCoolingDown = false;
            activeCooldownDuration = 0f;
            return true;
        }

        public void ResetCooldown()
        {
            IsCoolingDown = false;
            RemainingCooldown = 0f;
            activeCooldownDuration = 0f;
        }

        /// <summary>
        /// 仅覆盖本运行时物品的冷却配置，不会修改ItemData资产。
        /// </summary>
        public void SetRuntimeCooldownDuration(float duration)
        {
            runtimeCooldownDuration = Mathf.Max(0.01f, duration);
        }

        public void ClearRuntimeCooldownDuration()
        {
            runtimeCooldownDuration = null;
        }

        public bool TryUpgrade()
        {
            if (!CanUpgrade)
            {
                return false;
            }

            float previousActiveCooldownDuration =
                activeCooldownDuration;
            Level++;

            if (IsCoolingDown &&
                previousActiveCooldownDuration > 0f)
            {
                float remainingProgress = Mathf.Clamp01(
                    RemainingCooldown /
                    previousActiveCooldownDuration);
                activeCooldownDuration =
                    CalculateActiveCooldownDuration();
                RemainingCooldown =
                    activeCooldownDuration *
                    remainingProgress;
            }

            return true;
        }

        private float CalculateActiveCooldownDuration()
        {
            return Mathf.Max(
                0.01f,
                EffectiveCooldownDuration *
                GamePacingDebugRuntime
                    .GetWhiteboardCooldownMultiplier() /
                StyleTendencyDebugRuntime
                    .GetItemCooldownSpeedMultiplier());
        }
    }
}
