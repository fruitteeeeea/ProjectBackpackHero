using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackPrototype
{
    /// <summary>Source-prefab-compatible card pack detail view.</summary>
    public sealed class UIPackInfo : MonoBehaviour
    {
        public ImageLoader loaderIcon;
        public TextMeshProUGUI textName, textTime, textCoin, textDiamond, textDebris, textOpenCost, textStartTime;
        public GameObject btnVideo, btnOpen, btnAther, btnStart, timeObj;

        PackMenuPresenter presenter;
        int index = -1;

        internal void Initialize(PackMenuPresenter value)
        {
            presenter = value;
            Button startButton = btnStart != null ? btnStart.GetComponent<Button>() : null;
            if (startButton != null) startButton.onClick.AddListener(OnClickStart);
        }

        public void Show(int slot)
        {
            index = slot;
            gameObject.SetActive(true);
            Refresh();
        }

        public void Refresh()
        {
            if (!gameObject.activeSelf || presenter == null || index < 0) return;
            PackState state = presenter.Packs.GetSlotState(index);
            if (state == PackState.Empty) { Close(); return; }

            PackDefinition definition = presenter.Packs.GetDefinition(presenter.Packs.GetSlots()[index].Id);
            if (definition == null) { Close(); return; }
            if (loaderIcon != null) loaderIcon.Select(IconIndex(definition.Id));
            if (textName != null) textName.text = definition.Name;
            if (textCoin != null) textCoin.text = $"{definition.GoldMin}~{definition.GoldMax}";
            if (textDiamond != null) textDiamond.text = $"{definition.DiamondMin}~{definition.DiamondMax}";
            if (textDebris != null) textDebris.text = $"{definition.FragmentMin}~{definition.FragmentMax}";
            if (textTime != null) textTime.text = FormatTime(presenter.Packs.GetRemainingSeconds(index));
            if (textStartTime != null) textStartTime.text = FormatTime(presenter.Packs.GetRemainingSeconds(index));
            if (textOpenCost != null) textOpenCost.text = presenter.Packs.GetSkipDiamondCost(index).ToString();
            if (btnVideo != null) btnVideo.SetActive(false);
            if (btnAther != null) btnAther.SetActive(state == PackState.Locked);
            if (btnStart != null) btnStart.SetActive(state == PackState.Start);
            if (btnOpen != null) btnOpen.SetActive(state == PackState.Opening || state == PackState.Locked);
            if (timeObj != null) timeObj.SetActive(state == PackState.Opening);
        }

        public void OnClickStart()
        {
            if (presenter != null && presenter.Packs.TryStartPack(index)) Close();
        }

        public void OnClickOpen()
        {
            if (presenter == null || !presenter.Packs.TrySkipAndOpenPack(index)) return;
            Close();
            presenter.ShowOpen(index);
        }

        public void OnClickVideo() { }
        public void OnClickAther() { }
        public void OnClickClose() => Close();
        public void Close() => gameObject.SetActive(false);

        static int IconIndex(PackId id) => id switch
        {
            PackId.Green => 0,
            PackId.Blue => 1,
            PackId.Purple => 2,
            PackId.Gold => 3,
            _ => 0
        };

        static string FormatTime(int seconds) => TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
    }
}
