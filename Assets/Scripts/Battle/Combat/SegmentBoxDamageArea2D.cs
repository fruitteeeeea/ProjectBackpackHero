using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 沿一条世界空间线段生成的旋转矩形范围。
    /// </summary>
    public sealed class SegmentBoxDamageArea2D :
        DamageArea2D
    {
        [Header("Shape")]
        [SerializeField, Min(0.001f)]
        private float width = 0.16f;

        [SerializeField, Min(0f)]
        private float startPadding;

        [SerializeField, Min(0f)]
        private float endPadding;

        [Header("Debug")]
        [SerializeField]
        private bool drawDamageGizmo = true;

        private Vector2 start;
        private Vector2 end;
        private Vector2 center;
        private Vector2 size =
            new Vector2(0.001f, 0.16f);
        private float angle;

        public Vector2 Start => start;
        public Vector2 End => end;
        public float Width => width;
        public float StartPadding => startPadding;
        public float EndPadding => endPadding;
        public override Vector2 WorldCenter => center;
        public override Vector2 WorldSize => size;
        public override float WorldAngle => angle;

        public void Configure(
            Vector2 segmentStart,
            Vector2 segmentEnd,
            float newWidth,
            float newStartPadding,
            float newEndPadding)
        {
            width = Mathf.Max(0.001f, newWidth);
            startPadding = Mathf.Max(
                0f,
                newStartPadding);
            endPadding = Mathf.Max(
                0f,
                newEndPadding);

            Vector2 segment =
                segmentEnd - segmentStart;

            float length = segment.magnitude;
            Vector2 direction =
                length > Mathf.Epsilon
                    ? segment / length
                    : Vector2.right;

            start =
                segmentStart -
                direction * startPadding;

            end =
                segmentEnd +
                direction * endPadding;

            center = (start + end) * 0.5f;
            size =
                new Vector2(
                    Mathf.Max(
                        0.001f,
                        length +
                        startPadding +
                        endPadding),
                    width);

            angle =
                Mathf.Atan2(
                    direction.y,
                    direction.x) *
                Mathf.Rad2Deg;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            width = Mathf.Max(0.001f, width);
            startPadding = Mathf.Max(
                0f,
                startPadding);
            endPadding = Mathf.Max(
                0f,
                endPadding);
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
                    center,
                    Quaternion.Euler(
                        0f,
                        0f,
                        angle),
                    Vector3.one);

            Gizmos.color =
                new Color(
                    1f,
                    0.1f,
                    0.1f,
                    0.8f);

            Gizmos.DrawWireCube(
                Vector3.zero,
                size);

            Gizmos.matrix = previousMatrix;
        }
#endif
    }
}
