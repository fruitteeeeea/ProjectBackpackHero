using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 在子弹生成时把阵营颜色和来源对应的拖尾样式应用到视觉组件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileVisualController2D : MonoBehaviour
    {
        [SerializeField] private ProjectileVisualSettings settings;

        public void Apply(
            BattleFaction faction,
            ProjectileVisualSource source)
        {
            if (settings == null)
            {
                return;
            }

            Color factionColor = settings.GetFactionColor(faction);

            foreach (SpriteRenderer spriteRenderer in
                     GetComponentsInChildren<SpriteRenderer>(true))
            {
                spriteRenderer.color = factionColor;
            }

            foreach (TrailRenderer trailRenderer in
                     GetComponentsInChildren<TrailRenderer>(true))
            {
                trailRenderer.time = settings.GetTrailDuration(source);
                trailRenderer.widthCurve = settings.TrailWidthCurve;
                trailRenderer.colorGradient =
                    settings.CreateTrailGradient(faction, source);
                trailRenderer.Clear();
            }
        }
    }
}
