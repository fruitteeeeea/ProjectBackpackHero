using BackpackHero.Battle;
using BackpackHero.Input;
using UnityEngine;

namespace BackpackPrototype
{
    public enum RandomFlightCurveMode
    {
        Off,
        Peaceful,
        Intense,
    }

    /// <summary>
    /// 调试用的玩家自动航线。手势输入会取消当前一次自动调整，
    /// 直到下一次随机决策才会重新接管曲线。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerRandomFlightCurveController : MonoBehaviour
    {
        private const float PeacefulMinimumInterval = 1.5f;
        private const float PeacefulMaximumInterval = 2.5f;
        private const float IntenseMinimumInterval = .8f;
        private const float IntenseMaximumInterval = 1.5f;
        private const float PeacefulSpeed = .5f;
        private const float IntenseSpeed = 1f;

        [SerializeField]
        private RandomFlightCurveMode mode;

        [SerializeField, Min(0f)]
        private float baseAdjustmentSpeed = 1f;

        private PlayerBackpackDebugBridge bridge;
        private PlayerBackpackSystem player;
        private HorizontalSwipeCurveInput subscribedInput;
        private float nextOperationTime;
        private float startCurveValue;
        private float targetCurveValue;
        private float adjustmentDuration;
        private float adjustmentElapsed;
        private bool isAdjusting;

        public RandomFlightCurveMode Mode => mode;
        public float BaseAdjustmentSpeed
        {
            get => baseAdjustmentSpeed;
            set => baseAdjustmentSpeed = Mathf.Max(0f, value);
        }

        public bool IsAdjusting => isAdjusting;

        public static float GetAdjustmentSpeed(
            RandomFlightCurveMode selectedMode,
            float baseSpeed)
        {
            float profileSpeed = selectedMode == RandomFlightCurveMode.Peaceful
                ? PeacefulSpeed
                : selectedMode == RandomFlightCurveMode.Intense
                    ? IntenseSpeed
                    : 0f;
            return profileSpeed * Mathf.Max(0f, baseSpeed);
        }

        public static Vector2 GetOperationInterval(
            RandomFlightCurveMode selectedMode)
        {
            return selectedMode == RandomFlightCurveMode.Peaceful
                ? new Vector2(PeacefulMinimumInterval, PeacefulMaximumInterval)
                : selectedMode == RandomFlightCurveMode.Intense
                    ? new Vector2(IntenseMinimumInterval, IntenseMaximumInterval)
                    : Vector2.zero;
        }

        public static bool ShouldBlockEnemy(float randomValue)
        {
            return randomValue < .75f;
        }

        public static float GetAdjustmentDuration(
            float startValue,
            float targetValue,
            float adjustmentSpeed)
        {
            float distance = Mathf.Abs(targetValue - startValue);
            return adjustmentSpeed > Mathf.Epsilon
                ? distance / adjustmentSpeed
                : 0f;
        }

        public static float EvaluateOutCubic(float progress)
        {
            float inverse = 1f - Mathf.Clamp01(progress);
            return 1f - inverse * inverse * inverse;
        }

        public void SetMode(RandomFlightCurveMode value)
        {
            mode = value;
            isAdjusting = false;
            ScheduleNextOperation();
        }

        private void OnDisable()
        {
            SubscribeToInput(null);
        }

        private void Update()
        {
            RefreshBindings();
            if (mode == RandomFlightCurveMode.Off ||
                !BattleFlowController.IsCombatPhase ||
                player == null)
            {
                isAdjusting = false;
                return;
            }

            if (Time.time >= nextOperationTime)
            {
                ChooseNextOperation();
                ScheduleNextOperation();
            }

            if (!isAdjusting)
            {
                return;
            }

            if (adjustmentDuration <= Mathf.Epsilon)
            {
                isAdjusting = false;
                return;
            }

            adjustmentElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(
                adjustmentElapsed / adjustmentDuration);
            float adjusted = Mathf.Lerp(
                startCurveValue,
                targetCurveValue,
                EvaluateOutCubic(progress));
            player.SetFlightCurveValue(adjusted);
            isAdjusting = progress < 1f;
        }

        private void RefreshBindings()
        {
            bridge ??= GetComponent<PlayerBackpackDebugBridge>();
            PlayerBackpackSystem newPlayer = bridge != null
                ? bridge.Target
                : null;
            if (player != newPlayer)
            {
                player = newPlayer;
                SubscribeToInput(null);
            }

            SubscribeToInput(player != null ? player.FlightCurveInput : null);
        }

        private void SubscribeToInput(HorizontalSwipeCurveInput input)
        {
            if (subscribedInput == input)
            {
                return;
            }

            if (subscribedInput != null)
            {
                subscribedInput.ValueChanged -= HandlePlayerCurveInput;
            }

            subscribedInput = input;
            if (subscribedInput != null)
            {
                subscribedInput.ValueChanged += HandlePlayerCurveInput;
            }
        }

        private void HandlePlayerCurveInput(float _)
        {
            // 外部手势优先；下一次定时决策才恢复自动控制。
            isAdjusting = false;
        }

        private void ChooseNextOperation()
        {
            if (!TryGetTargetCurveValue(out targetCurveValue))
            {
                isAdjusting = false;
                return;
            }

            startCurveValue = player.FighterSpawner != null
                ? player.FighterSpawner.CurrentCurveValue
                : 0f;
            adjustmentElapsed = 0f;
            float speed = GetAdjustmentSpeed(
                mode,
                baseAdjustmentSpeed);
            adjustmentDuration = GetAdjustmentDuration(
                startCurveValue,
                targetCurveValue,
                speed);

            if (speed <= Mathf.Epsilon)
            {
                isAdjusting = false;
                return;
            }

            isAdjusting = adjustmentDuration > Mathf.Epsilon;

            if (!isAdjusting)
            {
                player.SetFlightCurveValue(targetCurveValue);
            }
        }

        private bool TryGetTargetCurveValue(out float value)
        {
            value = 0f;
            Fighter2D[] fighters = FindObjectsByType<Fighter2D>(
                FindObjectsSortMode.None);
            Vector3 origin = player.FighterSpawner != null
                ? player.FighterSpawner.SpawnPosition
                : player.transform.position;
            bool blockNearest = ShouldBlockEnemy(Random.value);
            Fighter2D nearest = null;
            float nearestDistance = float.PositiveInfinity;
            float total = 0f;
            int count = 0;

            foreach (Fighter2D fighter in fighters)
            {
                if (fighter == null || !fighter.IsAlive ||
                    fighter.Faction != BattleFaction.Enemy ||
                    !fighter.TryGetComponent(out BattleCurveFollower2D follower) ||
                    !follower.IsFollowingCurve)
                {
                    continue;
                }

                count++;
                total += fighter.BattleCurveValue;
                float distance = (fighter.transform.position - origin).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = fighter;
                }
            }

            if (count == 0)
            {
                return false;
            }

            value = blockNearest && nearest != null
                ? nearest.BattleCurveValue
                : total / count;
            value = Mathf.Clamp(value, -1f, 1f);
            return true;
        }

        private void ScheduleNextOperation()
        {
            Vector2 interval = GetOperationInterval(mode);
            nextOperationTime = mode == RandomFlightCurveMode.Off
                ? float.PositiveInfinity
                : Time.time + Random.Range(interval.x, interval.y);
        }
    }
}
