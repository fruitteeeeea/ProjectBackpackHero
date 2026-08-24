using UnityEngine;
using UnityEngine.UI;

/// <summary>Source-compatible image selector required by the migrated UI prefabs.</summary>
public sealed class ImageLoader : MonoBehaviour
{
    public Image image;
    public Sprite[] sprites;

    public void Select(int index)
    {
        if (index < 0 || index >= sprites.Length) return;
        image ??= GetComponent<Image>();
        image.sprite = sprites[index];
    }
}
