using System;
using PlanetWar.ReusableMainMenu;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
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
        private const string OriginalEntityDetailsPath = Package + "/Prefabs/HangarOriginal/Details/UICardInfo.prefab";
        private const string OriginalSpellDetailsPath = Package + "/Prefabs/HangarOriginal/Details/UICardSpell.prefab";
        private const string Art = Package + "/Art/Ranks/";
        private const string HangarArt = Package + "/Art/Hangar/Icon/";
        private static TMP_FontAsset textFont;

        [InitializeOnLoadMethod]
        private static void BuildMissingRanksAfterImport()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                var firstPreview = prefab != null ? prefab.transform.Find("UICardView/Scroll View/Viewport/Content/HangarItemPreview_01") : null;
                var fourthDeckPreview = prefab != null ? prefab.transform.Find("UICardView/equipWeapon/HangarEquipPreview_04") : null;
                var lastCollectionPreview = prefab != null ? prefab.transform.Find("UICardView/Scroll View/Viewport/Content/HangarItemPreview_12") : null;
                var firstDeckCard = fourthDeckPreview != null ? fourthDeckPreview.GetComponent<HangarCardItem>() : null;
                var deckLockVisible = fourthDeckPreview != null && Find(fourthDeckPreview, "Lock")?.gameObject.activeSelf == true;
                if (prefab != null && (prefab.transform.Find("UIRankList") == null || prefab.transform.Find("UICardView") == null || prefab.transform.Find("UICardView/UICardInfo") == null || prefab.transform.Find("UICardView/UICardSpell") == null || firstPreview == null || fourthDeckPreview == null || lastCollectionPreview == null || firstPreview.GetComponent<HangarCardItem>() == null || firstDeckCard == null || !firstDeckCard.IsUnlocked || deckLockVisible || prefab.transform.Find("UICardView/UICardInfo")?.GetComponent<HangarDetailLayout>() == null))
                {
                    Debug.Log("[PlanetWar] Rebuilding MainMenu Hangar with the original static card configuration.");
                    Rebuild();
                }
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
            var entityDetails = CreateStaticDetails(OriginalEntityDetailsPath, page.transform, view, "UICardInfo", HangarCardItem.CardKind.Entity);
            var spellDetails = CreateStaticDetails(OriginalSpellDetailsPath, page.transform, view, "UICardSpell", HangarCardItem.CardKind.Spell);
            var rankCoin = Find(page.transform, "resCoin");
            var rankDiamond = Find(page.transform, "resDiam");
            var topHud = CreateUi("HangarTopSafeArea", page.transform, Vector2.zero, Vector2.zero);
            var topHudRect = topHud.GetComponent<RectTransform>();
            topHudRect.anchorMin = new Vector2(0f, 1f); topHudRect.anchorMax = new Vector2(1f, 1f);
            topHudRect.pivot = new Vector2(.5f, 1f); topHudRect.anchoredPosition = Vector2.zero; topHudRect.sizeDelta = Vector2.zero;
            topHud.AddComponent<TopSafeAreaInset>();
            if (rankCoin != null) rankCoin.SetParent(topHud.transform, true);
            if (rankDiamond != null) rankDiamond.SetParent(topHud.transform, true);
            Set(view, "entityDetails", entityDetails);
            Set(view, "spellDetails", spellDetails);
            Set(view, "entityLayout", ConfigureDetailLayout(entityDetails));
            Set(view, "spellLayout", ConfigureDetailLayout(spellDetails));
            Set(view, "goldText", Find(rankCoin, "Text (TMP)")?.GetComponent<TMP_Text>());
            Set(view, "diamondText", Find(rankDiamond, "Text (TMP)")?.GetComponent<TMP_Text>());
            var battleTop = Find(root.transform, "Top");
            Set(view, "battleGoldText", battleTop != null ? Find(battleTop, "resCoin")?.GetComponentInChildren<TMP_Text>(true) : null);
            Set(view, "battleDiamondText", battleTop != null ? Find(battleTop, "resDiam")?.GetComponentInChildren<TMP_Text>(true) : null);
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
            {
                var deck = new[]
                {
                    Card(1001, "Pioneer", "A small basic fighter craft, a common sight on the space battlefield.", "icon_feichuan_1", true, true, 1),
                    Card(1002, "UFO", "Fires lightning chains to deal chain damage to surrounding enemies.", "icon_feichuan_2", true, true, 1),
                    Card(1003, "Swarm", "Appears in massive swarms, flies at high speed, and focuses fire on enemy targets.", "icon_feichuan_3", true, true, 1),
                    Card(1004, "Battleship", "The core combat force of space warfare, slow in flight, and excels at long-range heavy artillery barrages.", "icon_feichuan_4", true, true, 1),
                };
                for (var i = 0; i < deck.Length; i++) CreateStaticPreviewItem(equipTemplate, equipParent, view, $"HangarEquipPreview_{i + 1:00}", deck[i]);
            }
            if (content == null || itemTemplate == null) return;
            var collection = new[]
            {
                Card(1005, "Gemini", "Twin raiders of the space battlefield, boasting high attack speed paired with dual-unit linkage.", "icon_feichuan_5", false, false, 1),
                Card(1006, "Strangler", "Wields plasma cannons to efficiently annihilate swarms of enemy aircraft.", "icon_feichuan_6", false, false, 2),
                Card(1007, "Aegis", "A medium-sized high-defense fighter that summons 2 units at once.", "icon_feichuan_7", false, false, 5),
                Card(1008, "Viking", "Launches guided missiles to inflict small-area AoE damage.", "icon_feichuan_8", false, false, 3),
                Card(1101, "Fireball", "Charges at enemies upon detection and detonates instantly to deal area damage.", "icon_feichuan_9", false, false, 4),
                Card(1102, "Titan", "A heavy frontline fighter, capable of blocking all incoming attacks.", "icon_feichuan_10", false, false, 5),
                Card(1103, "Star-Piercer", "Fires armor-piercing rounds that pierce through frontline targets and precisely strike hidden enemies in the rear.", "icon_feichuan_11", false, false, 6),
                Card(1104, "Enforcer", "Roams the entire battlefield to pick off high-value enemy targets with pinpoint accuracy.", "icon_feichuan_12", false, false, 7),
                Card(2005, "Solar-Storm", "Deal damage to all enemy units in a vertical line.", "icon_jineng_5", false, false, 1, HangarCardItem.CardKind.Spell),
                Card(2003, "Freeze-Beam", "Freeze all combat units in a vertical line.", "icon_jineng_3", false, false, 2, HangarCardItem.CardKind.Spell),
                Card(2001, "Nano-Boost", "Shield all friendly units in a vertical line with 1 layer of shield.", "icon_jineng_1", false, false, 3, HangarCardItem.CardKind.Spell),
                Card(2006, "Collective-Frenzy", "Increase the attack speed of all friendly units on the field.", "icon_jineng_6", false, false, 4, HangarCardItem.CardKind.Spell),
            };
            for (var i = 0; i < collection.Length; i++) CreateStaticPreviewItem(itemTemplate, content, view, $"HangarItemPreview_{i + 1:00}", collection[i]);
        }

        private static void CreateStaticPreviewItem(GameObject template, Transform parent, HangarView view, string name, HangarCardState state)
        {
            var item = UnityEngine.Object.Instantiate(template, parent, false);
            item.name = name;
            RemoveMissingScripts(item);
            var card = item.GetComponent<HangarCardItem>() ?? item.AddComponent<HangarCardItem>();
            card.Configure(view, state.kind, state.id, state.displayName, state.description, HangarSprite(state.icon), HangarSprite(state.icon + "_hui"), state.unlocked, state.equipped, state.rank);
            // Serialize the exact original ItemCard lock hierarchy state as well. Runtime applies
            // the same state on enable; this keeps the authored prefab correct before play mode.
            var lockRoot = Find(item.transform, "Lock");
            if (lockRoot != null) lockRoot.gameObject.SetActive(!state.unlocked);
            var lockBackground = Find(item.transform, "Lockbg");
            if (lockBackground != null) lockBackground.gameObject.SetActive(!state.unlocked);
            foreach (var button in item.GetComponentsInChildren<Button>(true))
            {
                button.onClick = new Button.ButtonClickedEvent();
                UnityEventTools.AddPersistentListener(button.onClick, card.onClickItem);
            }
        }

        private static GameObject CreateStaticDetails(string prefabPath, Transform parent, HangarView view, string name, HangarCardItem.CardKind kind)
        {
            var original = PrefabUtility.LoadPrefabContents(prefabPath);
            if (original == null) throw new InvalidOperationException($"{name} original prefab is missing.");
            var panel = UnityEngine.Object.Instantiate(original);
            PrefabUtility.UnloadPrefabContents(original);
            panel.name = name;
            panel.transform.SetParent(parent, false);
            RemoveMissingScripts(panel);
            panel.transform.SetAsLastSibling();
            var close = panel.transform.Find("close");
            if (close != null)
            {
                var dismiss = close.GetComponent<HangarDetailClose>() ?? close.gameObject.AddComponent<HangarDetailClose>();
                dismiss.Configure(view);
                var button = close.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick = new Button.ButtonClickedEvent();
                    UnityEventTools.AddPersistentListener(button.onClick, dismiss.OnClickClose);
                }
            }
            panel.SetActive(false);
            return panel;
        }

        private static HangarDetailLayout ConfigureDetailLayout(GameObject panel)
        {
            var layout = panel.GetComponent<HangarDetailLayout>() ?? panel.AddComponent<HangarDetailLayout>();
            var previewRoot = Find(panel.transform, "ItemCard (1)") ?? Find(panel.transform, "ItemCard");
            var preview = previewRoot != null ? previewRoot.GetComponent<HangarCardItem>() ?? previewRoot.gameObject.AddComponent<HangarCardItem>() : null;
            layout.Configure(Find(panel.transform, "btnUpgrade")?.gameObject, Find(panel.transform, "btnUpBattle")?.gameObject, Find(panel.transform, "ObjSlider")?.gameObject, Find(panel.transform, "btns")?.gameObject, Find(panel.transform, "name")?.GetComponentInChildren<TMP_Text>(true), Find(panel.transform, "desc")?.GetComponentInChildren<TMP_Text>(true), Find(panel.transform, "textLock")?.GetComponent<TMP_Text>(), preview);
            return layout;
        }

        private readonly struct HangarCardState
        {
            public readonly int id, rank;
            public readonly string displayName, description, icon;
            public readonly bool unlocked, equipped;
            public readonly HangarCardItem.CardKind kind;
            public HangarCardState(int id, string displayName, string description, string icon, bool unlocked, bool equipped, int rank, HangarCardItem.CardKind kind)
            { this.id = id; this.displayName = displayName; this.description = description; this.icon = icon; this.unlocked = unlocked; this.equipped = equipped; this.rank = rank; this.kind = kind; }
        }

        private static HangarCardState Card(int id, string name, string desc, string icon, bool unlocked, bool equipped, int rank, HangarCardItem.CardKind kind = HangarCardItem.CardKind.Entity)
            => new HangarCardState(id, name, desc, icon, unlocked, equipped, rank, kind);

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
                Set(row, "background", Find(root, "bg")?.GetComponent<UnityEngine.UI.Image>());
            Set(row, "normalBackground", Sprite("bg_paihang_1.png"));
            Set(row, "currentPlayerBackground", Sprite("bg_paihang_2.png"));
                Set(row, "countryFlag", Find(root, "iconCountry")?.GetComponent<UnityEngine.UI.Image>());
                Set(row, "medal", Find(root, "topRank")?.GetComponentInChildren<UnityEngine.UI.Image>(true));
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
                var viewport = CreateUi("Viewport", scrollRoot.transform, Vector2.zero, new Vector2(0, -10)); Stretch(viewport.GetComponent<RectTransform>()); viewport.AddComponent<UnityEngine.UI.Image>().color = new Color(1, 1, 1, .001f); viewport.AddComponent<Mask>().showMaskGraphic = false;
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
        private static UnityEngine.UI.Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size) { var image = CreateUi(name, parent, position, size).AddComponent<UnityEngine.UI.Image>(); image.sprite = sprite; return image; }
        private static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Vector2 position, Vector2 dimensions) { var text = CreateUi(name, parent, position, dimensions).AddComponent<TextMeshProUGUI>(); text.text = value; text.font = textFont; text.fontSize = size; text.color = Color.white; text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.NoWrap; return text; }
        private static Sprite Sprite(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + file);
        private static Sprite HangarSprite(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(HangarArt + file + ".png");
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
