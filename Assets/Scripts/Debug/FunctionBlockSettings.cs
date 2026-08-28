using UnityEngine;

namespace BackpackHero.Debugging
{
    /// <summary>Controls temporary feature visibility and entry points in the main menu.</summary>
    [CreateAssetMenu(fileName = "FunctionBlock", menuName = "Backpack Hero/Function Block Settings")]
    public sealed class FunctionBlockSettings : ScriptableObject
    {
        [SerializeField] private bool blockMilestone = true;
        [SerializeField] private bool blockPack = true;

        public bool BlockMilestone => blockMilestone;
        public bool BlockPack => blockPack;

        public void SetValues(bool milestoneBlocked, bool packBlocked)
        {
            blockMilestone = milestoneBlocked;
            blockPack = packBlocked;
        }
    }
}
