using DG.Tweening;
using MoreMountains.Feedbacks;
using BackpackHero.Debugging;
using System;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 集中触发飞机的 FEEL 反馈。
    /// MMF_Player 由 Prefab Inspector 手动添加和配置，未配置时安全跳过。
    /// </summary>
    public sealed class FighterFeedbacks : MonoBehaviour
    {
        private static readonly int FlashAmountProperty =
            Shader.PropertyToID("_FlashAmount");

        private const float HitWhiteFlashDuration = 0.12f;

        [Header("MMF Players")]
        [SerializeField]
        private MMF_Player attackFeedback;

        [SerializeField]
        private MMF_Player hitFeedback;

        [SerializeField]
        private MMF_Player deathFeedback;

        [Header("Death VFX")]
        [SerializeField]
        private GameObject explosionParticlesPrefab;

        [SerializeField]
        private ParticleSystem deathFlashParticlesPrefab;

        [SerializeField]
        private Color playerExplosionColor =
            new Color(0.55f, 0.85f, 1f, 1f);

        [SerializeField]
        private Color enemyExplosionColor =
            new Color(1f, 0.55f, 0.55f, 1f);

        private FighterDamageFloatingText floatingDamageText;
        private SpriteRenderer aircraftSpriteRenderer;
        private MaterialPropertyBlock spritePropertyBlock;
        private Tween hitWhiteFlashTween;
        private float hitWhiteFlashAmount;

        [Header("Death")]
        [SerializeField, Min(0f)]
        private float deathCleanupDelay = 0.55f;

        public float DeathCleanupDelay => deathCleanupDelay;

        public void PlayAttack()
        {
            AircraftVisualSettings settings =
                AircraftVisualDebugRuntime.CurrentSettings;
            if (!settings.AircraftVisualOverridesEnabled)
            {
                attackFeedback?.PlayFeedbacks();
                return;
            }

            PlayFeedbacks(
                attackFeedback,
                feedback => feedback is not MMF_Scale ||
                            settings.AttackScaleTween,
                transform.position,
                1f);
        }

        public void PlayHit(
            float damage,
            BattleFaction faction)
        {
            AircraftVisualSettings settings =
                AircraftVisualDebugRuntime.CurrentSettings;
            if (!settings.AircraftVisualOverridesEnabled)
            {
                hitFeedback?.PlayFeedbacks(transform.position, damage);
                floatingDamageText?.Play(damage, faction);
                return;
            }

            MMF_InstantiateObject hitParticlesFeedback =
                FindFeedback<MMF_InstantiateObject>(hitFeedback);
            GameObject previousParticles = hitParticlesFeedback != null
                ? hitParticlesFeedback.InstantiatedGameObject
                : null;
            PlayFeedbacks(
                hitFeedback,
                feedback =>
                    (feedback is not MMF_RotationShake ||
                     settings.HitRotationShakeTween) &&
                    (feedback is not MMF_InstantiateObject ||
                     settings.HitParticles),
                transform.position,
                damage);

            if (settings.HitParticles &&
                hitParticlesFeedback != null &&
                hitParticlesFeedback.InstantiatedGameObject != null &&
                hitParticlesFeedback.InstantiatedGameObject != previousParticles)
            {
                AircraftParticleVisualOverride.Apply(
                    hitParticlesFeedback.InstantiatedGameObject,
                    settings.HitParticlesStartSpeed,
                    settings.HitParticlesStartSize,
                    settings.HitParticlesBurstCount,
                    settings.HitParticlesTexture);
            }

            if (settings.HitWhiteFlash)
            {
                PlayHitWhiteFlash();
            }

            floatingDamageText?.Play(damage, faction);
        }

        public void PlayDeath(BattleFaction faction)
        {
            AircraftVisualSettings settings =
                AircraftVisualDebugRuntime.CurrentSettings;
            if (!settings.AircraftVisualOverridesEnabled)
            {
                deathFeedback?.PlayFeedbacks();
                PlayOriginalDeathParticles(faction);
                return;
            }

            if (!settings.DeathRotationTween &&
                !settings.DeathScaleTween &&
                aircraftSpriteRenderer != null)
            {
                aircraftSpriteRenderer.enabled = false;
            }

            PlayFeedbacks(
                deathFeedback,
                feedback =>
                    (feedback is not MMF_Rotation ||
                     settings.DeathRotationTween) &&
                    (feedback is not MMF_Scale ||
                     settings.DeathScaleTween),
                transform.position,
                1f);

            Color factionColor =
                faction == BattleFaction.Player
                    ? playerExplosionColor
                    : enemyExplosionColor;

            if (settings.DeathExplosionParticles)
            {
                PlayTintedParticles(
                    explosionParticlesPrefab,
                    factionColor,
                    settings.DeathExplosionParticlesStartSpeed,
                    settings.DeathExplosionParticlesStartSize,
                    settings.DeathExplosionParticlesBurstCount,
                    settings.DeathExplosionParticlesTexture);
            }

            if (settings.DeathFlashParticles)
            {
                PlayTintedParticles(
                    deathFlashParticlesPrefab,
                    factionColor,
                    settings.DeathFlashParticlesStartSpeed,
                    settings.DeathFlashParticlesStartSize,
                    settings.DeathFlashParticlesBurstCount,
                    settings.DeathFlashParticlesTexture);
            }
        }

        /// <summary>
        /// FEEL 会在一次播放中读取每项 Feedback 的 Active 状态。
        /// 临时屏蔽对应项后立即还原，避免把调试状态写回 Prefab 或影响
        /// 未受本控制器管理的 Feedback。
        /// </summary>
        private static void PlayFeedbacks(
            MMF_Player player,
            Func<MMF_Feedback, bool> isEnabled,
            Vector3 position,
            float intensity)
        {
            if (player == null || player.FeedbacksList == null)
            {
                return;
            }

            int count = player.FeedbacksList.Count;
            bool[] originalStates = new bool[count];
            for (int index = 0; index < count; index++)
            {
                MMF_Feedback feedback = player.FeedbacksList[index];
                if (feedback == null)
                {
                    continue;
                }

                originalStates[index] = feedback.Active;
                feedback.Active = feedback.Active && isEnabled(feedback);
            }

            player.PlayFeedbacks(position, intensity);

            for (int index = 0; index < count; index++)
            {
                MMF_Feedback feedback = player.FeedbacksList[index];
                if (feedback != null)
                {
                    feedback.Active = originalStates[index];
                }
            }
        }

        private static T FindFeedback<T>(MMF_Player player)
            where T : MMF_Feedback
        {
            if (player?.FeedbacksList == null)
            {
                return null;
            }

            foreach (MMF_Feedback feedback in player.FeedbacksList)
            {
                if (feedback is T typedFeedback)
                {
                    return typedFeedback;
                }
            }

            return null;
        }

        private void PlayHitWhiteFlash()
        {
            if (aircraftSpriteRenderer == null)
            {
                return;
            }

            hitWhiteFlashTween?.Kill();
            SetHitWhiteFlashAmount(1f);
            hitWhiteFlashTween = DOTween.To(
                    () => hitWhiteFlashAmount,
                    SetHitWhiteFlashAmount,
                    0f,
                    HitWhiteFlashDuration)
                .SetEase(Ease.OutQuad)
                .SetTarget(this);
        }

        private void SetHitWhiteFlashAmount(float amount)
        {
            hitWhiteFlashAmount = Mathf.Clamp01(amount);
            if (aircraftSpriteRenderer == null)
            {
                return;
            }

            spritePropertyBlock ??= new MaterialPropertyBlock();
            aircraftSpriteRenderer.GetPropertyBlock(spritePropertyBlock);
            spritePropertyBlock.SetFloat(
                FlashAmountProperty,
                hitWhiteFlashAmount);
            aircraftSpriteRenderer.SetPropertyBlock(spritePropertyBlock);
        }

        private void ClearHitWhiteFlash()
        {
            hitWhiteFlashTween?.Kill();
            hitWhiteFlashTween = null;
            SetHitWhiteFlashAmount(0f);
        }

        private void PlayTintedParticles(
            GameObject particlesPrefab,
            Color color,
            float startSpeed,
            float startSize,
            int burstCount,
            Texture2D texture)
        {
            if (particlesPrefab == null)
            {
                return;
            }

            GameObject particles = Instantiate(
                particlesPrefab,
                transform.position,
                particlesPrefab.transform.rotation);

            foreach (ParticleSystem particleSystem in
                     particles.GetComponentsInChildren<ParticleSystem>())
            {
                ParticleSystem.MainModule main =
                    particleSystem.main;

                main.startColor = color;
            }

            AircraftParticleVisualOverride.Apply(
                particles,
                startSpeed,
                startSize,
                burstCount,
                texture);
        }

        private void PlayOriginalDeathParticles(BattleFaction faction)
        {
            Color factionColor =
                faction == BattleFaction.Player
                    ? playerExplosionColor
                    : enemyExplosionColor;
            PlayTintedParticles(explosionParticlesPrefab, factionColor);
            PlayTintedParticles(deathFlashParticlesPrefab, factionColor);
        }

        private void PlayTintedParticles(
            GameObject particlesPrefab,
            Color color)
        {
            if (particlesPrefab == null)
            {
                return;
            }

            GameObject particles = Instantiate(
                particlesPrefab,
                transform.position,
                particlesPrefab.transform.rotation);
            foreach (ParticleSystem particleSystem in
                     particles.GetComponentsInChildren<ParticleSystem>())
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = color;
            }
        }

        private void PlayTintedParticles(
            ParticleSystem particlesPrefab,
            Color color)
        {
            if (particlesPrefab == null)
            {
                return;
            }

            ParticleSystem particles = Instantiate(
                particlesPrefab,
                transform.position,
                particlesPrefab.transform.rotation);
            ParticleSystem.MainModule main = particles.main;
            main.startColor = color;
        }

        private void PlayTintedParticles(
            ParticleSystem particlesPrefab,
            Color color,
            float startSpeed,
            float startSize,
            int burstCount,
            Texture2D texture)
        {
            if (particlesPrefab == null)
            {
                return;
            }

            ParticleSystem particles = Instantiate(
                particlesPrefab,
                transform.position,
                particlesPrefab.transform.rotation);

            ParticleSystem.MainModule main = particles.main;
            main.startColor = color;
            AircraftParticleVisualOverride.Apply(
                particles.gameObject,
                startSpeed,
                startSize,
                burstCount,
                texture);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            deathCleanupDelay = Mathf.Max(0f, deathCleanupDelay);
        }
#endif

        private void Awake()
        {
            floatingDamageText =
                GetComponent<FighterDamageFloatingText>();
            aircraftSpriteRenderer =
                GetComponentInChildren<SpriteRenderer>(true);
        }

        private void OnDisable()
        {
            AircraftVisualDebugRuntime.SettingsChanged -=
                HandleVisualSettingsChanged;
            ClearHitWhiteFlash();
        }

        private void OnEnable()
        {
            AircraftVisualDebugRuntime.SettingsChanged +=
                HandleVisualSettingsChanged;
            if (aircraftSpriteRenderer != null)
            {
                aircraftSpriteRenderer.enabled = true;
            }
        }

        private void OnDestroy()
        {
            ClearHitWhiteFlash();
        }

        private void HandleVisualSettingsChanged(
            AircraftVisualSettings settings)
        {
            if (!settings.AircraftVisualOverridesEnabled)
            {
                ClearHitWhiteFlash();
            }
        }
    }
}
