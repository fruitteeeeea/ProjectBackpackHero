using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 控制飞机专属的飞行行为。
    /// 当前负责生成时的速度爆发，后续会接入Battle Curve。
    /// </summary>
    [RequireComponent(typeof(DirectionalMover2D))]
    public sealed class FighterFlight2D : MonoBehaviour
    {
        [Header("Spawn Speed Burst")]
        [Tooltip("速度爆发持续时间，单位为秒。")]
        [SerializeField, Min(0f)]
        private float burstDuration = 0.5f;

        [Tooltip("横轴是爆发进度0~1，纵轴是基础速度倍率。")]
        [SerializeField]
        private AnimationCurve burstSpeedCurve =
            AnimationCurve.Linear(0f, 1.5f, 1f, 1f);

        private DirectionalMover2D mover;
        private float burstElapsedTime;
        private bool isBurstActive;

        private void Awake()
        {
            mover = GetComponent<DirectionalMover2D>();
        }

        private void OnEnable()
        {
            RestartBurst();
        }

        private void Update()
        {
            UpdateSpeedBurst(Time.deltaTime);
        }

        /// <summary>
        /// 重新开始生成速度爆发。
        /// 对象池再次启用飞机时也可以调用此方法。
        /// </summary>
        public void RestartBurst()
        {
            burstElapsedTime = 0f;
            isBurstActive = true;

            if (mover == null)
            {
                mover = GetComponent<DirectionalMover2D>();
            }

            ApplyBurstMultiplier(0f);
        }

        private void UpdateSpeedBurst(float deltaTime)
        {
            if (!isBurstActive)
            {
                return;
            }

            if (burstDuration <= 0f)
            {
                FinishBurst();
                return;
            }

            burstElapsedTime += deltaTime;

            float normalizedTime =
                Mathf.Clamp01(burstElapsedTime / burstDuration);

            ApplyBurstMultiplier(normalizedTime);

            if (normalizedTime >= 1f)
            {
                FinishBurst();
            }
        }

        private void ApplyBurstMultiplier(float normalizedTime)
        {
            float multiplier =
                burstSpeedCurve.Evaluate(normalizedTime);

            mover.SetSpeedMultiplier(multiplier);
        }

        private void FinishBurst()
        {
            isBurstActive = false;

            // 使用曲线最后一刻的值，不把结束倍率写死为1。
            ApplyBurstMultiplier(1f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            burstDuration = Mathf.Max(0f, burstDuration);

            if (burstSpeedCurve == null ||
                burstSpeedCurve.length == 0)
            {
                burstSpeedCurve =
                    AnimationCurve.Linear(0f, 1.5f, 1f, 1f);
            }
        }
#endif
    }
}