using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 固定在攻击Prefab局部空间中的矩形范围。
    /// 可供后续火焰、毒雾等区域攻击复用。
    /// </summary>
    public sealed class FixedBoxDamageArea2D :
        DamageArea2D
    {
        [SerializeField]
        private Vector2 localCenter;

        [SerializeField]
        private Vector2 localSize =
            Vector2.one;

        [SerializeField]
        private float localAngle;

        [SerializeField]
        private bool drawDamageGizmo = true;

        public override Vector2 WorldCenter =>
            transform.TransformPoint(localCenter);

        public override Vector2 WorldSize
        {
            get
            {
                Vector3 scale =
                    transform.lossyScale;

                return new Vector2(
                    localSize.x *
                    Mathf.Abs(scale.x),
                    localSize.y *
                    Mathf.Abs(scale.y));
            }
        }

        public override float WorldAngle =>
            transform.eulerAngles.z + localAngle;

        public void Configure(
            Vector2 center,
            Vector2 size,
            float angle)
        {
            localCenter = center;
            localSize =
                new Vector2(
                    Mathf.Max(0.001f, size.x),
                    Mathf.Max(0.001f, size.y));
            localAngle = angle;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            localSize =
                new Vector2(
                    Mathf.Max(
                        0.001f,
                        localSize.x),
                    Mathf.Max(
                        0.001f,
                        localSize.y));
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDamageGizmo)
            {
                return;
            }

            Matrix4x4 previousMatrix =
                Gizmos.matrix;

            Gizmos.matrix =
                Matrix4x4.TRS(
                    WorldCenter,
                    Quaternion.Euler(
                        0f,
                        0f,
                        WorldAngle),
                    Vector3.one);

            Gizmos.color =
                new Color(
                    1f,
                    0.45f,
                    0.1f,
                    0.8f);

            Gizmos.DrawWireCube(
                Vector3.zero,
                WorldSize);

            Gizmos.matrix = previousMatrix;
        }
#endif
    }
}
