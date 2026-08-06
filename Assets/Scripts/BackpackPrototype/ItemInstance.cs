using UnityEngine;
using BackpackHero.Debugging;

namespace BackpackPrototype
{
    public sealed class ItemInstance
    {
        public const int DefaultLevel = 1;
        public const int MaximumLevel = 2;

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
            activeCooldownDuration = Mathf.Max(
                0.01f,
                Data.CooldownDuration *
                GamePacingDebugRuntime
                    .GetWhiteboardCooldownMultiplier());
            RemainingCooldown = activeCooldownDuration;
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

        public bool TryUpgrade()
        {
            if (!CanUpgrade)
            {
                return false;
            }

            Level++;
            return true;
        }
    }
}
