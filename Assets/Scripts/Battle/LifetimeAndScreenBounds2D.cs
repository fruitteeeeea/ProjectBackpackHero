using System;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 控制2D飞行物的生命周期和屏幕边界销毁。
    /// </summary>
    public sealed class LifetimeAndScreenBounds2D : MonoBehaviour
    {
        [Header("Lifetime")]
        [Tooltip("飞行物最多存在多少秒。设为0表示不使用时间销毁。")]
        [SerializeField, Min(0f)]
        private float lifetime = 10f;

        [SerializeField]
        private bool destroyWhenLifetimeExpires = true;

        [Header("Screen Bounds")]
        [SerializeField]
        private bool destroyWhenLeavingScreen = true;

        [Tooltip("屏幕外额外保留的Viewport范围，避免物体刚碰到边缘就消失。")]
        [SerializeField, Min(0f)]
        private float viewportMargin = 0.05f;

        private Camera targetCamera;
        private float configuredLifetime;
        private float elapsedTime;
        private bool hasEnteredScreen;

        private bool hasLifetimeExpired;

        public float Lifetime => lifetime;
        /// <summary>Prefab或场景配置的初始寿命，不会受运行时倍率写入影响。</summary>
        public float ConfiguredLifetime => configuredLifetime;
        public float RemainingLifetime =>
            Mathf.Max(0f, lifetime - elapsedTime);
        public bool HasLifetimeExpired => hasLifetimeExpired;
        public bool DestroyWhenLifetimeExpires =>
            destroyWhenLifetimeExpires;

        /// <summary>
        /// 在寿命首次到期时触发。即使调用方选择自行处理到期结果，
        /// 事件也只会触发一次。
        /// </summary>
        public event Action LifetimeExpired;

        private void Awake()
        {
            configuredLifetime = lifetime;
        }

        private void OnEnable()
        {
            RestartLifetime();
        }

        private void Update()
        {
            UpdateLifetime(Time.deltaTime);

            if (destroyWhenLeavingScreen)
            {
                UpdateScreenBounds();
            }
        }

        /// <summary>
        /// 重置生命周期。
        /// 以后使用对象池重新启用对象时也可以调用。
        /// </summary>
        public void RestartLifetime()
        {
            elapsedTime = 0f;
            hasLifetimeExpired = false;
            hasEnteredScreen = false;
            targetCamera = Camera.main;
        }

        public void SetLifetime(float newLifetime)
        {
            lifetime = Mathf.Max(0f, newLifetime);
        }

        /// <summary>
        /// 设置寿命到期后的默认行为。关闭后仍会正常计时并发出
        /// LifetimeExpired，由持有者决定后续退场方式。
        /// </summary>
        public void SetDestroyWhenLifetimeExpires(bool value)
        {
            destroyWhenLifetimeExpires = value;

            if (destroyWhenLifetimeExpires && hasLifetimeExpired)
            {
                DestroySelf();
            }
        }

        private void UpdateLifetime(float deltaTime)
        {
            if (lifetime <= 0f || hasLifetimeExpired)
            {
                return;
            }

            elapsedTime += deltaTime;

            if (elapsedTime >= lifetime)
            {
                hasLifetimeExpired = true;
                LifetimeExpired?.Invoke();

                if (destroyWhenLifetimeExpires)
                {
                    DestroySelf();
                }
            }
        }

        private void UpdateScreenBounds()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;

                if (targetCamera == null)
                {
                    return;
                }
            }

            Vector3 viewportPosition =
                targetCamera.WorldToViewportPoint(
                    transform.position);

            bool isInFrontOfCamera =
                viewportPosition.z > 0f;

            bool isInsideScreen =
                isInFrontOfCamera &&
                viewportPosition.x >= 0f &&
                viewportPosition.x <= 1f &&
                viewportPosition.y >= 0f &&
                viewportPosition.y <= 1f;

            if (isInsideScreen)
            {
                hasEnteredScreen = true;
                return;
            }

            if (!hasEnteredScreen)
            {
                return;
            }

            bool isOutsideDestroyBounds =
                !isInFrontOfCamera ||
                viewportPosition.x < -viewportMargin ||
                viewportPosition.x > 1f + viewportMargin ||
                viewportPosition.y < -viewportMargin ||
                viewportPosition.y > 1f + viewportMargin;

            if (isOutsideDestroyBounds)
            {
                DestroySelf();
            }
        }

        private void DestroySelf()
        {
            Destroy(gameObject);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            lifetime = Mathf.Max(0f, lifetime);
            viewportMargin =
                Mathf.Max(0f, viewportMargin);
        }
#endif
    }
}
