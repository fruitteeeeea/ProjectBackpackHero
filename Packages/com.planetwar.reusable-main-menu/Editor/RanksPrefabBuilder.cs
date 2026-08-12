using System;
using PlanetWar.ReusableMainMenu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetWar.ReusableMainMenu.Editor
{
    public static class RanksPrefabBuilder
    {
        private const string Package = "Packages/com.planetwar.reusable-main-menu";
        private const string PrefabPath = Package + "/Prefabs/MainMenu.prefab";
        private const string OriginalRankPagePath = Package + "/Prefabs/RankPageOriginal.prefab";
        private const string OriginalHangarPagePath = Package + "/Prefabs/HangarOriginal/UICardView.prefab";
        private const string OriginalHangarItemPath = Package + "/Prefabs/HangarOriginal/ItemCard.prefab";
        private const string OriginalHangarEquipItemPath = Package + "/Prefabs/HangarOriginal/ItemCardEquip.prefab";
        private const string Art = Package + "/Art/Ranks/";
        private static TMP_FontAsset textFont;

        [InitializeOnLoadMethod]
        private static void BuildMissingRanksAfterImport()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab != null && (prefab.transform.Find("UIRankList") == null || prefab.transform.Find("UICardView") == null)) Rebuild();
            };
        }

        [MenuItem("PlanetWar/Reusable Main Menu/Rebuild Static Ranks")]
        public static void Rebuild()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                textFont = root.GetComponentInChildren<TMP_Text>(true)?.font;
                if (textFont == null) throw new InvalidOperationException("MainMenu.prefab must contain a TMP font reference.");
                var existing = root.transform.Find("UIRankList");
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                BuildFromOriginal(root);
                BuildHangarFromOriginal(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildFromOriginal(GameObject root)
        {
                var original = PrefabUtility.LoadPrefabContents(OriginalRankPagePath);
                if (original == null) throw new InvalidOperationException("RankPageOriginal.prefab is missing.");
                var page = UnityEngine.Object.Instantiate(original);
                PrefabUtility.UnloadPrefabContents(original);
                page.name = "UIRankList";
                page.transform.SetParent(root.transform, false);
                page.SetActive(false);
                RemoveMissingScripts(page);

                // The original page is used intact: its background, currency bar, rank frame, crown,
                // title, viewport and item coordinates are the source of truth for this package.
                var scrollRoot = Find(page.transform, "Scroll View");
                var scroll = scrollRoot.GetComponent<ScrollRect>();
                var templateTransform = Find(scroll.content, "item");
                var template = templateTransform.GetComponent<RankRowView>() ?? templateTransform.gameObject.AddComponent<RankRowView>();
                ConfigureOriginalRow(template);
                template.gameObject.SetActive(false);

                var title = Find(page.transform, "title").GetComponent<TMP_Text>();
                var rankCoin = Find(page.transform, "resCoin");
                var rankDiamond = Find(page.transform, "resDiam");
                var topHud = CreateUi("RanksTopSafeArea", page.transform, Vector2.zero, Vector2.zero);
                var topHudRect = topHud.GetComponent<RectTransform>();
                topHudRect.anchorMin = new Vector2(0f, 1f); topHudRect.anchorMax = new Vector2(1f, 1f);
                topHudRect.pivot = new Vector2(.5f, 1f); topHudRect.anchoredPosition = Vector2.zero; topHudRect.sizeDelta = Vector2.zero;
                topHud.AddComponent<TopSafeAreaInset>();
                if (rankCoin != null) rankCoin.SetParent(topHud.transform, true);
                if (rankDiamond != null) rankDiamond.SetParent(topHud.transform, true);
                var entries = CreateEntries();
                var view = page.GetComponent<RanksView>() ?? page.AddComponent<RanksView>();
                Set(view, "arenaTitle", title);
                Set(view, "scrollRect", scroll);
                Set(view, "itemTemplate", template);
                Set(view, "entries", entries);
                Set(view, "goldText", Find(rankCoin, "Text (TMP)")?.GetComponent<TMP_Text>());
                Set(view, "diamondText", Find(rankDiamond, "Text (TMP)")?.GetComponent<TMP_Text>());
                var battleTop = Find(root.transform, "Top");
                Set(view, "battleGoldText", battleTop != null ? Find(battleTop, "resCoin")?.GetComponentInChildren<TMP_Text>(true) : null);
                Set(view, "battleDiamondText", battleTop != null ? Find(battleTop, "resDiam")?.GetComponentInChildren<TMP_Text>(true) : null);
                var rowHeight = new SerializedObject(view).FindProperty("rowHeight"); rowHeight.floatValue = 100f; new SerializedObject(view).ApplyModifiedPropertiesWithoutUndo();

                var controller = root.GetComponent<MainMenuPageController>() ?? root.AddComponent<MainMenuPageController>();
                Set(controller, "mainPage", root.transform.Find("UIMain").gameObject); Set(controller, "ranksPage", view); var indicator = root.GetComponentInChildren<MainBottmChoose>(true); Set(controller, "tabIndicator", indicator);
                var bottom = root.GetComponentInChildren<UIMainBottom>(true);
                if (bottom != null) page.transform.SetSiblingIndex(bottom.transform.GetSiblingIndex());
                var buttons = bottom != null ? bottom.GetComponentsInChildren<Button>(true) : new Button[0];
                Array.Sort(buttons, (left, right) => left.transform.position.x.CompareTo(right.transform.position.x));
                Set(controller, "tabTargets", Array.ConvertAll(buttons, button => button.transform));
        }

        private static void BuildHangarFromOriginal(GameObject root)
        {
            var existing = root.transform.Find("UICardView");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            var original = PrefabUtility.LoadPrefabContents(OriginalHangarPagePath);
            if (original == null) throw new InvalidOperationException("Hangar original prefab is missing.");
            var page = UnityEngine.Object.Instantiate(original);
            PrefabUtility.UnloadPrefabContents(original);
            page.name = "UICardView";
            page.transform.SetParent(root.transform, false);
            page.SetActive(false);
            RemoveMissingScripts(page);

            // Keep the original page layout and authored item shells. This migration deliberately
            // does not reproduce its Addressables/data-driven card population.
            var view = page.GetComponent<HangarView>() ?? page.AddComponent<HangarView>();
            CreateStaticPreviewItems(page.transform, view);
            var mask = CreateMask(page.transform, view);
            var rankCoin = Find(page.transform, "resCoin");
            var rankDiamond = Find(page.transform, "resDiam");
            var topHud = CreateUi("HangarTopSafeArea", page.transform, Vector2.zero, Vector2.zero);
            var topHudRect = topHud.GetComponent<RectTransform>();
            topHudRect.anchorMin = new Vector2(0f, 1f); topHudRect.anchorMax = new Vector2(1f, 1f);
            topHudRect.pivot = new Vector2(.5f, 1f); topHudRect.anchoredPosition = Vector2.zero; topHudRect.sizeDelta = Vector2.zero;
            topHud.AddComponent<TopSafeAreaInset>();
            if (rankCoin != null) rankCoin.SetParent(topHud.transform, true);
            if (rankDiamond != null) rankDiamond.SetParent(topHud.transform, true);
            Set(view, "placeholderMask", mask);
            Set(view, "goldText", Find(rankCoin, "Text (TMP)")?.GetComponent<TMP_Text>());
            Set(view, "diamondText", Find(rankDiamond, "Text (TMP)")?.GetComponent<TMP_Text>());
            var battleTop = Find(root.transform, "Top");
            Set(view, "battleGoldText", battleTop != null ? Find(battleTop, "resCoin")?.GetComponentInChildren<TMP_Text>(true) : null);
            Set(view, "battleDiamondText", battleTop != null ? Find(battleTop, "resDiam")?.GetComponentInChildren<TMP_Text>(true) : null);
            foreach (var button in page.GetComponentsInChildren<HangarItemPlaceholderButton>(true)) button.SetHangar(view);

            var controller = root.GetComponent<MainMenuPageController>() ?? root.AddComponent<MainMenuPageController>();
            Set(controller, "hangarPage", view);
            var bottom = root.GetComponentInChildren<UIMainBottom>(true);
            if (bottom != null) page.transform.SetSiblingIndex(bottom.transform.GetSiblingIndex());
        }

        private static void CreateStaticPreviewItems(Transform page, HangarView view)
        {
            var equipParent = Find(page, "equipWeapon");
            var content = Find(Find(page, "Scroll View"), "Content");
            var equipTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(OriginalHangarEquipItemPath);
            var itemTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(OriginalHangarItemPath);
            if (equipParent != null && equipTemplate != null)
                CreateStaticPreviewItem(equipTemplate, equipParent, view, "HangarEquipPreview_01");
            if (content == null || itemTemplate == null) return;
            for (var i = 0; i < 6; i++)
                CreateStaticPreviewItem(itemTemplate, content, view, $"HangarItemPreview_{i + 1:00}");
        }

        private static void CreateStaticPreviewItem(GameObject template, Transform parent, HangarView view, string name)
        {
            var item = UnityEngine.Object.Instantiate(template, parent, false);
            item.name = name;
            RemoveMissingScripts(item);
            var placeholder = item.GetComponent<HangarItemPlaceholderButton>() ?? item.AddComponent<HangarItemPlaceholderButton>();
            placeholder.SetHangar(view);
        }

        private static GameObject CreateMask(Transform parent, HangarView view)
        {
            var mask = CreateUi("HangarPlaceholderMask", parent, Vector2.zero, Vector2.zero);
            var rect = mask.GetComponent<RectTransform>();
            Stretch(rect); rect.SetAsLastSibling();
            var image = mask.AddComponent<Image>(); image.color = new Color(0f, 0f, 0f, .62f);
            var dismiss = mask.AddComponent<HangarMaskDismiss>(); dismiss.SetHangar(view);
            var label = Text("Placeholder", mask.transform, "Item details coming soon", 30f, Vector2.zero, new Vector2(560f, 72f));
            label.alignment = TextAlignmentOptions.Center;
            mask.SetActive(false);
            return mask;
        }

        private static RankEntry[] CreateEntries()
        {
            var names = new[] { "Starweaver", "Millet Plant Glow", "Daisy Sunwhisper", "Noah Sunbloom", "Ion Frostweaver", "Orange Glow", "Iron Glow", "Sequoia", "Nova Skydancer", "River Ember", "Luna Cloudsong", "Aster Moonfall", "Player", "Sage Brightstar", "Echo Wildfire", "Robin Mistwalker", "Sky Silvermoon", "Piper Sunray", "Rowan Starfall", "Aquamarine" };
            var flags = new[] { Sprite("country0.png"), Sprite("country1.png"), Sprite("country2.png"), Sprite("country3.png"), Sprite("country4.png"), Sprite("country5.png") };
            var entries = new RankEntry[names.Length];
            for (var i = 0; i < entries.Length; i++) entries[i] = new RankEntry { displayName = names[i], score = 792 - i * 8, countryFlag = flags[i % flags.Length], isCurrentPlayer = i == 12 };
            return entries;
        }

        private static void ConfigureOriginalRow(RankRowView row)
        {
            var root = row.transform;
            // Original ItemRankList switches its "bg" ImageLoader between bg_paihang_1 and bg_paihang_2.
            Set(row, "background", Find(root, "bg")?.GetComponent<Image>());
            Set(row, "normalBackground", Sprite("bg_paihang_1.png"));
            Set(row, "currentPlayerBackground", Sprite("bg_paihang_2.png"));
            Set(row, "countryFlag", Find(root, "iconCountry")?.GetComponent<Image>());
            Set(row, "medal", Find(root, "topRank")?.GetComponentInChildren<Image>(true));
            Set(row, "medalSprites", new[] { Sprite("icon_paihang_jiangpai_1.png"), Sprite("icon_paihang_jiangpai_2.png"), Sprite("icon_paihang_jiangpai_3.png") });
            Set(row, "rankLabel", Find(root, "textRank")?.GetComponent<TMP_Text>());
            Set(row, "nameLabel", Find(root, "name")?.GetComponent<TMP_Text>());
            Set(row, "scoreLabel", Find(root, "score")?.GetComponent<TMP_Text>());
        }

        private static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var result = Find(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private static void RemoveMissingScripts(GameObject root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
        }

        /* Legacy handcrafted builder retained below for reference, but no longer used. */
        private static void BuildLegacy(GameObject root)
        {
                // Keep UIMainBase visible as the page backdrop. bg_paihang_beijing_4 is a small decorative tile,
                // not a full-screen background, so it must never be stretched over the rank page.
                var page = CreateUi("UIRankList", root.transform, Vector2.zero, new Vector2(720, 1280)); page.SetActive(false);
                // Author back-to-front. The Sliced frame is decorative only and must not cover title, crown, or rows.
                var frame = CreateImage("RankFrame", page.transform, Sprite("bg_paihang_3.png"), new Vector2(0, -20), new Vector2(608, 930)); frame.type = UnityEngine.UI.Image.Type.Sliced;
                var scrollRoot = CreateUi("Scroll View", page.transform, new Vector2(0, -31), new Vector2(540, 805));
                var scroll = scrollRoot.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 28f;
                var viewport = CreateUi("Viewport", scrollRoot.transform, Vector2.zero, new Vector2(0, -10)); Stretch(viewport.GetComponent<RectTransform>()); viewport.AddComponent<Image>().color = new Color(1, 1, 1, .001f); viewport.AddComponent<Mask>().showMaskGraphic = false;
                var content = CreateUi("Content", viewport.transform, Vector2.zero, new Vector2(0, 2000)); var contentRt = content.GetComponent<RectTransform>(); contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1); contentRt.pivot = new Vector2(.5f, 1);
                scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.content = contentRt;
                var rows = new RankRowView[20]; var entries = new RankEntry[20];
                var names = new[] { "Starweaver", "Millet Plant Glow", "Daisy Sunwhisper", "Noah Sunbloom", "Ion Frostweaver", "Orange Glow", "Iron Glow", "Sequoia", "Nova Skydancer", "River Ember", "Luna Cloudsong", "Aster Moonfall", "Milo Nightwind", "Sage Brightstar", "Echo Wildfire", "Robin Mistwalker", "Sky Silvermoon", "Piper Sunray", "Rowan Starfall", "Player" };
                var flags = new[] { Sprite("country0.png"), Sprite("country1.png"), Sprite("country2.png"), Sprite("country3.png"), Sprite("country4.png"), Sprite("country5.png") };
                for (var i = 0; i < rows.Length; i++) { rows[i] = CreateRow(content.transform, i); entries[i] = new RankEntry { displayName = names[i], score = 520 - i * 7, countryFlag = flags[i % flags.Length], isCurrentPlayer = i == 12 }; }
                var title = Text("ArenaTitle", page.transform, "Arena 3", 38, new Vector2(0, 432), new Vector2(400, 58)); title.fontStyle = FontStyles.Bold;
                var crown = CreateImage("Crown", page.transform, Sprite("icon_paihang_wangguan.png"), new Vector2(0, 489), new Vector2(116, 82)); crown.preserveAspect = true;
                var view = page.AddComponent<RanksView>(); Set(view, "arenaTitle", title); Set(view, "scrollRect", scroll); Set(view, "rows", rows); Set(view, "entries", entries);
                var controller = root.GetComponent<MainMenuPageController>() ?? root.AddComponent<MainMenuPageController>(); Set(controller, "mainPage", root.transform.Find("UIMain").gameObject); Set(controller, "ranksPage", view); var indicator = root.GetComponentInChildren<MainBottmChoose>(true); Set(controller, "tabIndicator", indicator);
                var bottom = root.GetComponentInChildren<UIMainBottom>(true);
                if (bottom != null) page.transform.SetSiblingIndex(bottom.transform.GetSiblingIndex());
                var buttons = bottom != null ? bottom.GetComponentsInChildren<Button>(true) : new Button[0];
                Array.Sort(buttons, (left, right) => left.transform.position.x.CompareTo(right.transform.position.x));
                var targets = Array.ConvertAll(buttons, button => button.transform);
                Set(controller, "tabTargets", targets);
        }

        private static RankRowView CreateRow(Transform parent, int index)
        {
            var row = CreateUi($"RankRow_{index + 1:00}", parent, new Vector2(0, -index * 100 - 48), new Vector2(536, 92));
            var rowRect = row.GetComponent<RectTransform>(); rowRect.anchorMin = rowRect.anchorMax = new Vector2(.5f, 1); rowRect.pivot = new Vector2(.5f, .5f);
            var background = row.AddComponent<UnityEngine.UI.Image>(); background.sprite = Sprite("bg_paihang_1.png"); background.type = UnityEngine.UI.Image.Type.Sliced;
            var medal = CreateImage("Medal", row.transform, Sprite("icon_paihang_jiangpai_1.png"), new Vector2(-213, 0), new Vector2(45, 55)); medal.preserveAspect = true;
            var rank = Text("Rank", row.transform, "4", 28, new Vector2(-213, 0), new Vector2(52, 52)); var flag = CreateImage("CountryFlag", row.transform, Sprite("country0.png"), new Vector2(-119, 0), new Vector2(46, 46)); flag.preserveAspect = true;
            var name = Text("Name", row.transform, "Player", 24, new Vector2(15, 0), new Vector2(230, 52)); name.alignment = TextAlignmentOptions.MidlineLeft;
            var score = Text("Score", row.transform, "520", 25, new Vector2(210, 0), new Vector2(90, 52)); score.alignment = TextAlignmentOptions.MidlineRight;
            var view = row.AddComponent<RankRowView>(); Set(view, "background", background); Set(view, "countryFlag", flag); Set(view, "medal", medal); Set(view, "medalSprites", new[] { Sprite("icon_paihang_jiangpai_1.png"), Sprite("icon_paihang_jiangpai_2.png"), Sprite("icon_paihang_jiangpai_3.png") }); Set(view, "rankLabel", rank); Set(view, "nameLabel", name); Set(view, "scoreLabel", score); return view;
        }

        private static GameObject CreateUi(string name, Transform parent, Vector2 position, Vector2 size) { var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false); var rt = go.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = position; rt.sizeDelta = size; return go; }
        private static Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size) { var image = CreateUi(name, parent, position, size).AddComponent<Image>(); image.sprite = sprite; return image; }
        private static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Vector2 position, Vector2 dimensions) { var text = CreateUi(name, parent, position, dimensions).AddComponent<TextMeshProUGUI>(); text.text = value; text.font = textFont; text.fontSize = size; text.color = Color.white; text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.NoWrap; return text; }
        private static Sprite Sprite(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + file);
        private static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        private static void Set(UnityEngine.Object target, string property, object value)
        {
            var serialized = new SerializedObject(target); var p = serialized.FindProperty(property);
            if (value is RankEntry[] entries)
            {
                p.arraySize = entries.Length;
                for (var i = 0; i < entries.Length; i++)
                {
                    var item = p.GetArrayElementAtIndex(i); var entry = entries[i];
                    item.FindPropertyRelative("displayName").stringValue = entry.displayName;
                    item.FindPropertyRelative("score").intValue = entry.score;
                    item.FindPropertyRelative("countryFlag").objectReferenceValue = entry.countryFlag;
                    item.FindPropertyRelative("isCurrentPlayer").boolValue = entry.isCurrentPlayer;
                }
            }
            else if (value is Array array) { p.arraySize = array.Length; for (var i = 0; i < array.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = array.GetValue(i) as UnityEngine.Object; }
            else p.objectReferenceValue = value as UnityEngine.Object;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
