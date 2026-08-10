using UnityEngine;

namespace BackpackPrototype
{
    /// <summary>
    /// Keeps a UI item's chosen world-space point fixed while its root rotates.
    /// </summary>
    public static class ItemPlacementRotation
    {
        public static Vector3 CalculateCorrectedWorldPosition(
            Vector3 currentWorldPosition,
            Vector3 fixedWorldPoint,
            Vector3 rotatedWorldPoint)
        {
            return currentWorldPosition +
                fixedWorldPoint - rotatedWorldPoint;
        }
    }
}
