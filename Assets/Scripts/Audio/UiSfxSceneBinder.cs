using UnityEngine;

namespace BackpackHero.Audio
{
    /// <summary>Provides an explicit scene-level click-clip reference to the global UI binder.</summary>
    public sealed class UiSfxSceneBinder : MonoBehaviour
    {
        [SerializeField] private AudioClip buttonClickClip;
        public AudioClip ButtonClickClip => buttonClickClip;
    }
}
