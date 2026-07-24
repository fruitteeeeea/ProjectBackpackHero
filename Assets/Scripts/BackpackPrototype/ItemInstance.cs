using UnityEngine;

namespace BackpackPrototype
{
    public sealed class ItemInstance
    {
        public ItemInstance(
            string id,
            ItemData data,
            Vector2Int anchorCell)
        {
            Id = id;
            Data = data;
            AnchorCell = anchorCell;
        }

        public string Id { get; }
        public ItemData Data { get; }
        public Vector2Int AnchorCell { get; set; }

        public bool IsCoolingDown { get; private set; }
        public float RemainingCooldown { get; private set; }

        public float CooldownProgress
        {
            get
            {
                if (Data == null ||
                    !Data.CanEnterCooldown ||
                    Data.CooldownDuration <= 0f)
                {
                    return 1f;
                }

                return Mathf.Clamp01(
                    1f -
                    RemainingCooldown /
                    Data.CooldownDuration);
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
            RemainingCooldown =
                Mathf.Max(0.01f, Data.CooldownDuration);
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
            return true;
        }

        public void ResetCooldown()
        {
            IsCoolingDown = false;
            RemainingCooldown = 0f;
        }
    }
}
