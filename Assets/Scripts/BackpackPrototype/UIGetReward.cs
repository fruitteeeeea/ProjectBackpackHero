using System.Collections;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace BackpackPrototype
{
    /// <summary>Source-prefab-compatible full-screen card pack opening and reward view.</summary>
    public sealed class UIGetReward : MonoBehaviour
    {
        public GameObject objItem, objAnimation;
        public Transform root;
        public SkeletonGraphic packSpine, spine;
        public TextMeshProUGUI txtTitle;
        public float animInterval = 2f;
        public float revealInterval = .15f;

        PackMenuPresenter presenter;
        int index = -1;
        bool waitingForOpen;
        bool settled;
        readonly List<GameObject> spawned = new();

        internal void Initialize(PackMenuPresenter value) => presenter = value;

        public void ShowOpen(int slot)
        {
            index = slot;
            waitingForOpen = true;
            settled = false;
            gameObject.SetActive(true);
            if (txtTitle != null) txtTitle.text = "Tap to skip";
            if (packSpine != null) packSpine.gameObject.SetActive(true);
            if (spine != null) spine.gameObject.SetActive(false);
            if (root != null) root.gameObject.SetActive(false);
            if (objAnimation != null) objAnimation.SetActive(false);
            SetPackSkin();
            if (packSpine != null && packSpine.AnimationState != null)
                packSpine.AnimationState.SetAnimation(0, "wait", true);
        }

        public void OnClickBg()
        {
            if (!waitingForOpen) { gameObject.SetActive(false); return; }
            waitingForOpen = false;
            if (packSpine == null || packSpine.AnimationState == null)
            {
                StartCoroutine(SettleAfterFallback());
                return;
            }

            TrackEntry entry = packSpine.AnimationState.SetAnimation(0, "open", false);
            if (entry == null) StartCoroutine(SettleAfterFallback());
            else entry.Complete += OnOpenCompleted;
        }

        void OnOpenCompleted(TrackEntry entry)
        {
            entry.Complete -= OnOpenCompleted;
            Settle();
        }

        IEnumerator SettleAfterFallback()
        {
            yield return new WaitForSeconds(.8f);
            Settle();
        }

        void Settle()
        {
            if (settled) return;
            settled = true;
            if (presenter == null || !presenter.Packs.TrySettleReward(index, out PackReward reward))
            {
                gameObject.SetActive(false);
                return;
            }

            if (packSpine != null) packSpine.gameObject.SetActive(false);
            if (spine != null) spine.gameObject.SetActive(true);
            if (root != null) root.gameObject.SetActive(true);
            if (objAnimation != null) objAnimation.SetActive(true);
            if (txtTitle != null) txtTitle.text = "Click to Continue";
            ShowReward("Gold", reward.Gold);
            ShowReward("Diamond", reward.Diamond);
            foreach (KeyValuePair<ItemData, int> pair in reward.Fragments) ShowReward(pair.Key.Name, pair.Value);
        }

        void ShowReward(string label, int count)
        {
            if (objItem == null || root == null) return;
            GameObject go = Instantiate(objItem, root);
            go.name = "ItemReward";
            go.SetActive(true);
            TextMeshProUGUI[] texts = go.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in texts)
            {
                if (text.name == "count") text.text = "x" + count;
                else if (text.name == "name") text.text = label;
            }
            spawned.Add(go);
        }

        void SetPackSkin()
        {
            if (packSpine == null || packSpine.Skeleton == null || presenter == null) return;
            PackDefinition definition = presenter.Packs.GetDefinition(presenter.Packs.GetSlots()[index].Id);
            if (definition == null) return;
            packSpine.Skeleton.SetSkin(definition.SpineSkin);
            packSpine.Skeleton.SetSlotsToSetupPose();
            packSpine.AnimationState.Apply(packSpine.Skeleton);
            packSpine.Update(0);
        }
    }
}
