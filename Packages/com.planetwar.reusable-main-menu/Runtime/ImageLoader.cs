using UnityEngine;
using UnityEngine.UI;
public class ImageLoader : MonoBehaviour
{
    public Image image;
    public Sprite[] sprites;
    public void Select(int index)
    {
        if (index < 0 || sprites == null || index >= sprites.Length) return;
        image = image == null ? GetComponent<Image>() : image;
        if (image != null) image.sprite = sprites[index];
    }
}
