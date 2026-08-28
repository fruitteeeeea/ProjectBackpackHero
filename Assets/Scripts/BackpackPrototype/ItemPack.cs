using System;
using System.Linq;
using Spine.Unity;
using BackpackHero.Debugging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackPrototype
{
    /// <summary>Behaviour for the unmodified ItemPack prefab migrated from the source project.</summary>
    public sealed class ItemPack : MonoBehaviour
    {
        internal PackMenuPresenter presenter;
        internal int index;
        public GameObject objEmpty, objPack, objBase, objOpening, objUnLock;
        public ImageLoader loaderIcon, ImgPackIcon;
        public TextMeshProUGUI textType, textTime, textOpeningTime, textOpeningDiamond;
        public SkeletonGraphic skeletonGraphic;
        Image background;
        PackState displayedState = PackState.Empty;

        internal void Initialize(PackMenuPresenter value, int slotIndex)
        {
            presenter = value;
            index = slotIndex;
            objEmpty = Find("empty")?.gameObject;
            objPack = Find("pcak")?.gameObject;
            objBase = Find("base")?.gameObject;
            objOpening = Find("opening")?.gameObject;
            objUnLock = Find("unLock")?.gameObject;
            textType = TextIn(objBase, "text");
            textTime = TextIn(objBase, "time");
            textOpeningTime = TextIn(objOpening, "time");
            textOpeningDiamond = TextIn(objOpening, "cost");
            skeletonGraphic = GetComponentInChildren<SkeletonGraphic>(true);
            background = GetComponent<Image>();
        }

        public void OnClickItem()
        {
            if (FunctionBlockRuntime.IsPackBlocked) return;
            var state = presenter.Packs.GetSlotState(index);
            if (state == PackState.Empty) return;
            if (state == PackState.Opened) presenter.ShowOpen(index);
            else presenter.ShowPackInfo(index);
        }

        // Kept for the source prefab's persistent Button event.
        public void onClickItem() => OnClickItem();

        public void Tick()
        {
            var state = presenter?.Packs.GetSlotState(index) ?? PackState.Empty;
            if (state != displayedState)
            {
                Refresh();
                return;
            }
            if (state != PackState.Opening) return;
            if (textOpeningTime != null) textOpeningTime.text = Format(presenter.Packs.GetRemainingSeconds(index));
            if (textOpeningDiamond != null) textOpeningDiamond.text = presenter.Packs.GetSkipDiamondCost(index).ToString();
        }

        internal void Refresh()
        {
            if (presenter == null) return;
            var state = presenter.Packs.GetSlotState(index);
            displayedState = state;
            if (objEmpty != null) objEmpty.SetActive(state == PackState.Empty);
            if (objPack != null) objPack.SetActive(state != PackState.Empty);
            if (ImgPackIcon != null) ImgPackIcon.Select(state == PackState.Empty ? 3 : state == PackState.Opening ? 1 : state == PackState.Opened ? 2 : 0);
            if (background != null) background.sprite = Resources.Load<Sprite>($"PackUI/Source/Main/bg_zjm_kabao_{(state == PackState.Empty ? 4 : state == PackState.Opening ? 2 : state == PackState.Opened ? 3 : 1)}");
            if (state == PackState.Empty) { if (skeletonGraphic != null) skeletonGraphic.gameObject.SetActive(false); return; }
            if (loaderIcon != null)
            {
                PackDefinition definition = presenter.Packs.GetDefinition(presenter.Packs.GetSlots()[index].Id);
                if (definition != null) loaderIcon.Select(definition.VisualIndex);
            }
            if (objBase != null) objBase.SetActive(state != PackState.Opening);
            if (objOpening != null) objOpening.SetActive(state == PackState.Opening);
            if (objUnLock != null) objUnLock.SetActive(state == PackState.Locked);
            // The source main-menu slot intentionally never displays its baked-in
            // unlock time; timing belongs in the pack detail panel only.
            if (textTime != null) textTime.gameObject.SetActive(false);
            if (textType != null) textType.text = state == PackState.Start || state == PackState.Locked ? "Unlock" : state == PackState.Opened ? "Open" : "";
            if (skeletonGraphic != null) skeletonGraphic.gameObject.SetActive(state == PackState.Opened);
            if (state == PackState.Opening)
            {
                if (textOpeningTime != null) textOpeningTime.text = Format(presenter.Packs.GetRemainingSeconds(index));
                if (textOpeningDiamond != null) textOpeningDiamond.text = presenter.Packs.GetSkipDiamondCost(index).ToString();
            }
        }

        internal static string Format(int seconds) => TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
        Transform Find(string name) => transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name);
        static TextMeshProUGUI TextIn(GameObject root, string name) => root == null ? null : root.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(x => x.name == name);
    }
}
