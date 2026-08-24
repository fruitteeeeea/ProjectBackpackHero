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
        GameObject backdrop;
        int index = -1;
        PackState displayedState = PackState.Empty;

        internal void Initialize(PackMenuPresenter value, GameObject backdropObject)
        {
            presenter = value;
            backdrop = backdropObject;
            Button startButton = btnStart != null ? btnStart.GetComponent<Button>() : null;
            if (startButton != null) startButton.onClick.AddListener(OnClickStart);
        }

        public void Show(int slot)
        {
            index = slot;
            if (backdrop != null)
            {
                backdrop.SetActive(true);
                backdrop.transform.SetAsLastSibling();
            }
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
        }

        public void Refresh()
        {
            if (!gameObject.activeSelf || presenter == null || index < 0) return;
            PackState state = presenter.Packs.GetSlotState(index);
            if (state == PackState.Empty) { Close(); return; }
            displayedState = state;

            PackDefinition definition = presenter.Packs.GetDefinition(presenter.Packs.GetSlots()[index].Id);
            if (definition == null) { Close(); return; }
            if (loaderIcon != null) loaderIcon.Select(IconIndex(definition.Id));
            if (textName != null) textName.text = definition.Name;
            if (textCoin != null) textCoin.text = $"{definition.GoldMin}~{definition.GoldMax}";
            if (textDiamond != null) textDiamond.text = $"{definition.DiamondMin}~{definition.DiamondMax}";
            if (textDebris != null) textDebris.text = $"{definition.FragmentMin}~{definition.FragmentMax}";
            RefreshCountdown();
            if (btnVideo != null) btnVideo.SetActive(false);
            if (btnAther != null) btnAther.SetActive(state == PackState.Locked);
            if (btnStart != null) btnStart.SetActive(state == PackState.Start);
            if (btnOpen != null) btnOpen.SetActive(state == PackState.Opening || state == PackState.Locked);
            if (timeObj != null) timeObj.SetActive(state == PackState.Opening);
        }

        void Update()
        {
            if (!gameObject.activeSelf || presenter == null || index < 0) return;
            PackState state = presenter.Packs.GetSlotState(index);
            if (state != displayedState)
            {
                // UTC time can change a slot from Opening to Opened without a
                // PackSystem Changed event, so refresh its controls once here.
                Refresh();
                return;
            }

            if (state == PackState.Opening) RefreshCountdown();
        }

        void RefreshCountdown()
        {
            if (presenter == null || index < 0) return;
            int remainingSeconds = presenter.Packs.GetRemainingSeconds(index);
            if (textTime != null) textTime.text = FormatTime(remainingSeconds);
            if (textStartTime != null) textStartTime.text = FormatTime(remainingSeconds);
            if (textOpenCost != null) textOpenCost.text = presenter.Packs.GetSkipDiamondCost(index).ToString();
        }

        public void OnClickStart()
        {
            if (presenter != null && presenter.Packs.TryStartPack(index)) Close();
        }

        public void OnClickOpen()
        {
            int slot = index;
            if (presenter == null || !presenter.Packs.TrySkipAndOpenPack(slot)) return;
            Close();
            presenter.ShowOpen(slot);
        }

        public void OnClickVideo() { }
        public void OnClickAther() { }
        public void OnClickClose() => Close();
        public void Close()
        {
            if (backdrop != null) backdrop.SetActive(false);
            gameObject.SetActive(false);
            index = -1;
        }

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
