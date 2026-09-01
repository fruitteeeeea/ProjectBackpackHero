using UnityEngine;

public sealed class WorldSpaceHealthBarFollower2D :
    MonoBehaviour
{
    [Header("Target")]
    [SerializeField]
    private Transform target;

    [Header("World Offset")]
    [SerializeField]
    private Vector3 worldOffset =
        new Vector3(0f, 0.5f, 0f);

    [Header("Orientation")]
    [SerializeField]
    private bool keepWorldRotation = true;

    public void SetTarget(
        Transform newTarget)
    {
        target = newTarget;
        UpdateTransform();
    }

    public void SetWorldOffset(
        Vector3 newOffset)
    {
        worldOffset = newOffset;
        UpdateTransform();
    }

    public void SetWorldOffsetY(
        float newY)
    {
        worldOffset.y = newY;
        UpdateTransform();
    }

    private void LateUpdate()
    {
        UpdateTransform();
    }

    private void UpdateTransform()
    {
        if (target == null)
        {
            return;
        }

        transform.position =
            target.position + worldOffset;

        if (keepWorldRotation)
        {
            transform.rotation =
                Quaternion.identity;
        }
    }
}
