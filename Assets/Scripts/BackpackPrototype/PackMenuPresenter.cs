using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackPrototype
{
    /// <summary>Builds the migrated pack UI beneath the reusable main menu's existing Pack node.</summary>
    public sealed class PackMenuPresenter : MonoBehaviour
    {
        readonly List<ItemPack> slots = new();
        Transform packContainer;
        Transform canvasRoot;
        UIPackInfo packInfo;
        UIGetReward rewardView;
        PackSystem packs;

        void Awake() { packs = GetComponent<PackSystem>(); if (packs != null) packs.Changed += Refresh; }
        void OnDestroy() { if (packs != null) packs.Changed -= Refresh; }
        void Update()
        {
            if (packContainer == null) TryBuild();
            if (packContainer != null) foreach (var slot in slots) slot.Tick();
        }
        void TryBuild()
        {
            var main = FindFirstObjectByType<PlanetWar.ReusableMainMenu.MainMenuView>(FindObjectsInactive.Include);
            if (main == null) return;
            Transform pack = FindChild(main.transform, "Pack");
            if (pack == null) return;
            canvasRoot = pack.GetComponentInParent<Canvas>()?.transform ?? main.transform;
            // These values, the GridLayoutGroup, and the ItemPack prefab are copied from
            // PlanetWar/Assets/Prefab/UI/UIMain.prefab and ItemPack.prefab respectively.
            var rect = (RectTransform)pack;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0, -372.5f);
            rect.sizeDelta = new Vector2(586.6886f, 210.7102f);
            packContainer = pack;
            var grid = pack.GetComponent<GridLayoutGroup>() ?? pack.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(5, 0, 0, 0);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.cellSize = new Vector2(138, 210);
            grid.spacing = new Vector2(8, 20);
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            for (int i = 0; i < PackSystem.SlotCount; i++) slots.Add(CreateSlot(i));
            Refresh();
        }
        ItemPack CreateSlot(int index)
        {
            var prefab = Resources.Load<GameObject>("PackUI/Source/Prefabs/ItemPack");
            if (prefab == null) throw new InvalidOperationException("Source ItemPack prefab is missing from Resources/PackUI/Source/Prefabs.");
            var root = Instantiate(prefab, packContainer);
            var view = root.GetComponent<ItemPack>() ?? root.AddComponent<ItemPack>();
            view.Initialize(this, index);
            return view;
        }
        internal void ShowPackInfo(int index)
        {
            if (packInfo == null) packInfo = BuildPackInfo();
            packInfo.Show(index);
        }
        internal void ShowOpen(int index)
        {
            if (rewardView == null) rewardView = BuildReward();
            rewardView.ShowOpen(index);
        }
        internal PackSystem Packs => packs;
        internal void Refresh() { foreach (var slot in slots) slot.Refresh(); packInfo?.Refresh(); }

        UIPackInfo BuildPackInfo()
        {
            var prefab = Resources.Load<GameObject>("PackUI/Source/Prefabs/UIPackInfo");
            if (prefab == null) throw new InvalidOperationException("Source UIPackInfo prefab is missing from Resources/PackUI/Source/Prefabs.");
            var root = Instantiate(prefab, canvasRoot);
            var view = root.GetComponent<UIPackInfo>() ?? root.AddComponent<UIPackInfo>();
            view.Initialize(this);
            root.SetActive(false);
            return view;
        }
        UIGetReward BuildReward()
        {
            var root = CreateModal("UIGetReward"); var panel = CreatePanel("content", root.transform); var view = root.AddComponent<UIGetReward>(); view.presenter = this;
            view.packSpine = TryCreateSpine("packSpine", panel.transform); view.spine = TryCreateSpine("spine", panel.transform);
            view.root = CreateUi("root", panel.transform).transform; AddVerticalLayout(view.root.gameObject, 6); view.objAnimation = CreateUi("objAnimation", panel.transform).gameObject; view.txtTitle = CreateLabel("txtTitle", panel.transform, "", 20);
            view.objItem = CreateUi("objItem", view.root).gameObject; view.objItem.AddComponent<ItemReward>(); view.objItem.SetActive(false);
            var bg = root.GetComponent<Button>(); bg.onClick.AddListener(view.OnClickBg); root.SetActive(false); return view;
        }
        GameObject CreateModal(string name)
        {
            var root = CreateUi(name, canvasRoot); var rect = root.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = root.AddComponent<Image>(); image.color = new Color(0, 0, 0, .85f); var button = root.AddComponent<Button>(); button.targetGraphic = image; return root;
        }
        static GameObject CreateUi(string name, Transform parent) { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go; }
        static GameObject CreatePanel(string name, Transform parent) { var go = CreateUi(name, parent); var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f); rect.sizeDelta = new Vector2(420, 560); go.AddComponent<Image>().color = new Color(.06f,.1f,.18f,.98f); AddVerticalLayout(go, 12); return go; }
        static void AddVerticalLayout(GameObject go, float spacing) { var layout = go.AddComponent<VerticalLayoutGroup>(); layout.childAlignment = TextAnchor.MiddleCenter; layout.spacing = spacing; layout.padding = new RectOffset(20,20,20,20); layout.childControlWidth = true; layout.childControlHeight = false; layout.childForceExpandHeight = false; }
        static TextMeshProUGUI CreateLabel(string name, Transform parent, string text, float size)
        { var go = CreateUi(name, parent); var label = go.AddComponent<TextMeshProUGUI>(); label.text = text; label.fontSize = size; label.alignment = TextAlignmentOptions.Center; label.color = Color.white; var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(130, 28); return label; }
        static GameObject CreateButton(string name, Transform parent, string text, UnityEngine.Events.UnityAction action)
        { var go = CreateUi(name, parent); var image = go.AddComponent<Image>(); image.color = new Color(.18f,.45f,.82f,1); var button = go.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action); CreateLabel("Text", go.transform, text, 14); go.GetComponent<RectTransform>().sizeDelta = new Vector2(260, 42); return go; }
        static SkeletonGraphic TryCreateSpine(string name, Transform parent)
        { var go = CreateUi(name, parent); var value = go.AddComponent<SkeletonGraphic>(); value.skeletonDataAsset = Resources.Load<SkeletonDataAsset>("PackUI/openPack/kaika_SkeletonData"); if (value.skeletonDataAsset != null) value.Initialize(true); return value; }
        static Transform FindChild(Transform root, string name) { foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child; return null; }
    }
    public sealed class UIGetReward : MonoBehaviour
    {
        internal PackMenuPresenter presenter; int index = -1; bool opening;
        public GameObject objItem, objAnimation; public Transform root; public SkeletonGraphic packSpine, spine; public TextMeshProUGUI txtTitle;
        public void ShowOpen(int slot) { index=slot; opening=true; gameObject.SetActive(true); txtTitle.text="Tap to open"; root.gameObject.SetActive(false); if (packSpine != null && packSpine.Skeleton != null) { var skin=presenter.Packs.GetDefinition(presenter.Packs.GetSlots()[slot].Id).SpineSkin; packSpine.Skeleton.SetSkin(skin); packSpine.AnimationState.SetAnimation(0,"wait",true); } }
        public void OnClickBg() { if (!opening) { gameObject.SetActive(false); return; } opening=false; StartCoroutine(SettleAfterOpenAnimation()); }
        IEnumerator SettleAfterOpenAnimation() { if (packSpine != null && packSpine.Skeleton != null) packSpine.AnimationState.SetAnimation(0,"open",false); yield return new WaitForSeconds(0.8f); if (!presenter.Packs.TrySettleReward(index, out var reward)) { gameObject.SetActive(false); yield break; } root.gameObject.SetActive(true); txtTitle.text="Click to Continue"; ShowReward("Gold", reward.Gold); ShowReward("Diamond", reward.Diamond); foreach (var pair in reward.Fragments) ShowReward(pair.Key.Name, pair.Value); }
        void ShowReward(string label, int count) { var go=Instantiate(objItem, root); go.name="ItemReward"; go.SetActive(true); var reward=go.GetComponent<ItemReward>(); reward.Set(label,count); }
    }
    public sealed class ItemReward : MonoBehaviour { TextMeshProUGUI label; public void Set(string name, int count) { label ??= CreateLabel(); label.text=$"{name} x{count}"; } TextMeshProUGUI CreateLabel() { var go=new GameObject("textCount",typeof(RectTransform),typeof(TextMeshProUGUI)); go.transform.SetParent(transform,false); var value=go.GetComponent<TextMeshProUGUI>(); value.fontSize=16; value.alignment=TextAlignmentOptions.Center; value.color=Color.white; return value; } }
}
