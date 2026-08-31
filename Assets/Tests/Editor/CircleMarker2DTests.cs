using BackpackHero.Input;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class CircleMarker2DTests
{
    private const string MarkerSpritePath =
        "Assets/Feel/NiceVibrations/Demo/Common/Sprites/" +
        "NVPaginationDot.png";

    [Test]
    public void ConfiguredMarker_AssignsSpriteColorScaleAndSortingOrder()
    {
        Sprite markerSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(MarkerSpritePath);
        Assert.That(markerSprite, Is.Not.Null);

        var markerObject = new GameObject("Circle Marker");
        markerObject.SetActive(false);

        try
        {
            CircleMarker2D marker =
                markerObject.AddComponent<CircleMarker2D>();
            var serializedMarker = new SerializedObject(marker);
            serializedMarker.FindProperty("markerSprite")
                .objectReferenceValue = markerSprite;
            serializedMarker.ApplyModifiedPropertiesWithoutUndo();

            markerObject.SetActive(true);
            marker.Color = Color.cyan;
            marker.Diameter = 2f;

            SpriteRenderer renderer =
                markerObject.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sprite, Is.SameAs(markerSprite));
            Assert.That(renderer.color, Is.EqualTo(Color.cyan));
            Assert.That(renderer.sortingOrder, Is.EqualTo(1));
            Assert.That(markerObject.transform.localScale,
                Is.EqualTo(new Vector3(2f, 2f, 1f)));
            LogAssert.NoUnexpectedReceived();
        }
        finally
        {
            Object.DestroyImmediate(markerObject);
        }
    }
}
