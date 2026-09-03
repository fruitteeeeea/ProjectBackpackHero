using System.Collections;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 让战斗物体继续沿原方向移动，同时关闭战斗碰撞并渐隐销毁。
    /// </summary>
    public sealed class BattleFadeOut2D : MonoBehaviour
    {
        [SerializeField, Min(0.05f)]
        private float duration = 0.5f;

        private bool hasStarted;
        private SpriteRenderer[] renderers;
        private Color[] originalColors;

        public bool IsFading => hasStarted;

        private void Awake()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        public static void Begin(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            BattleFadeOut2D fade =
                target.GetComponent<BattleFadeOut2D>();

            if (fade == null)
            {
                fade = target.AddComponent<BattleFadeOut2D>();
            }

            fade.StartFade();
        }

        public void StartFade()
        {
            if (hasStarted)
            {
                return;
            }

            hasStarted = true;
            CacheRenderersAndColors();

            FighterCombat2D combat =
                GetComponent<FighterCombat2D>();
            if (combat != null)
            {
                combat.enabled = false;
            }

            foreach (Collider2D battleCollider in
                     GetComponentsInChildren<Collider2D>(true))
            {
                battleCollider.enabled = false;
            }

            foreach (ParticleSystem particles in
                     GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmitting);
            }

            StartCoroutine(FadeAndDestroy());
        }

        private IEnumerator FadeAndDestroy()
        {
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.05f, duration);

            while (elapsed < safeDuration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                float alpha =
                    1f - Mathf.Clamp01(elapsed / safeDuration);

                for (int index = 0;
                     index < renderers.Length;
                     index++)
                {
                    if (renderers[index] == null)
                    {
                        continue;
                    }

                    Color color = originalColors[index];
                    color.a *= alpha;
                    renderers[index].color = color;
                }
            }

            Destroy(gameObject);
        }

        private void CacheRenderersAndColors()
        {
            if (renderers == null)
            {
                renderers = GetComponentsInChildren<SpriteRenderer>(true);
            }

            originalColors = new Color[renderers.Length];

            for (int index = 0; index < renderers.Length; index++)
            {
                originalColors[index] = renderers[index].color;
            }
        }
    }
}
