using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BackpackHero.EditorTools
{
    /// <summary>
    /// Creates the persisted TMP asset used by the item-level badge.  Persisting
    /// the pre-populated digits avoids relying on a transient dynamic atlas.
    /// </summary>
    [InitializeOnLoad]
    internal static class LevelBadgeMilkerFontAssetInstaller
    {
        private const string SourceFontPath = "Assets/Resources/Fonts/Milker.otf";
        private const string FontAssetPath =
            "Assets/Resources/Fonts/Milker SDF.asset";
        private const string LevelDigits = "0123456789";

        static LevelBadgeMilkerFontAssetInstaller()
        {
            EditorApplication.delayCall += CreateFontAssetIfNeeded;
        }

        [MenuItem("Tools/Backpack Hero/Create Milker Level Badge Font")]
        private static void CreateFontAssetIfNeeded()
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                FontAssetPath);
            if (existing != null)
            {
                return;
            }

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                Debug.LogError("Milker level-badge source font could not be loaded.");
                return;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont,
                90, 9, GlyphRenderMode.SDFAA, 256, 256,
                AtlasPopulationMode.Dynamic, true);
            string missing = string.Empty;
            if (fontAsset == null ||
                !fontAsset.TryAddCharacters(LevelDigits, out missing))
            {
                Debug.LogError($"Unable to create Milker TMP level-badge font. " +
                    $"Missing glyphs: {missing}");
                return;
            }

            fontAsset.name = "Milker SDF";
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            AddSubAsset(fontAsset.material, fontAsset);
            foreach (Texture2D atlasTexture in fontAsset.atlasTextures)
            {
                AddSubAsset(atlasTexture, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(FontAssetPath);
            Debug.Log("Created Milker TMP font asset for item level badges.");
        }

        private static void AddSubAsset(Object asset, Object parent)
        {
            if (asset != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset)))
            {
                AssetDatabase.AddObjectToAsset(asset, parent);
            }
        }
    }
}
