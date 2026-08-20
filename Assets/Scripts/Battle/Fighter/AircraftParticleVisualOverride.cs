using System.Collections.Generic;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>将全局调试参数应用到一次性粒子实例，而不修改共享材质。</summary>
    public sealed class AircraftParticleVisualOverride : MonoBehaviour
    {
        private readonly List<Material> runtimeMaterials = new();

        public static void Apply(
            GameObject particles,
            float startSpeed,
            float startSize,
            int burstCount,
            Texture2D texture)
        {
            if (particles == null)
            {
                return;
            }

            AircraftParticleVisualOverride cleanup =
                particles.GetComponent<AircraftParticleVisualOverride>() ??
                particles.AddComponent<AircraftParticleVisualOverride>();
            float safeStartSpeed = Mathf.Max(0f, startSpeed);
            float safeStartSize = Mathf.Max(0f, startSize);
            int safeBurstCount = Mathf.Max(0, burstCount);

            foreach (ParticleSystem particleSystem in
                     particles.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.startSpeed = safeStartSpeed;
                main.startSize = safeStartSize;

                ParticleSystem.EmissionModule emission = particleSystem.emission;
                int burstCountInSystem = emission.burstCount;
                if (burstCountInSystem > 0)
                {
                    ParticleSystem.Burst[] bursts =
                        new ParticleSystem.Burst[burstCountInSystem];
                    emission.GetBursts(bursts);
                    for (int index = 0; index < bursts.Length; index++)
                    {
                        ParticleSystem.Burst burst = bursts[index];
                        burst.count = safeBurstCount;
                        bursts[index] = burst;
                    }

                    emission.SetBursts(bursts);
                }

                if (texture == null || !particleSystem.TryGetComponent(
                        out ParticleSystemRenderer renderer))
                {
                    continue;
                }

                Material sourceMaterial = renderer.sharedMaterial;
                if (sourceMaterial == null)
                {
                    continue;
                }

                Material instanceMaterial = new(sourceMaterial);
                bool textureApplied = false;
                if (instanceMaterial.HasProperty("_MainTex"))
                {
                    instanceMaterial.SetTexture("_MainTex", texture);
                    textureApplied = true;
                }

                if (instanceMaterial.HasProperty("_BaseMap"))
                {
                    instanceMaterial.SetTexture("_BaseMap", texture);
                    textureApplied = true;
                }

                if (!textureApplied)
                {
                    Destroy(instanceMaterial);
                    continue;
                }

                renderer.material = instanceMaterial;
                cleanup.runtimeMaterials.Add(instanceMaterial);
            }
        }

        private void OnDestroy()
        {
            foreach (Material material in runtimeMaterials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }

            runtimeMaterials.Clear();
        }
    }
}
