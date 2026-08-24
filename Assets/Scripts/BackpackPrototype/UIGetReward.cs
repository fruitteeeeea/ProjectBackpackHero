using System.Collections;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        readonly List<ItemReward> spawned = new();
        Coroutine revealRoutine;
        Coroutine animationRoutine;
        ItemReward rewardPrefab;

        internal void Initialize(PackMenuPresenter value) => presenter = value;

        public void ShowOpen(int slot)
        {
            StopPresentationRoutines();
            ResetRewardItems();
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
            PlayAnimation(spine);

            List<System.Action<ItemReward>> bindings = new();
            if (reward.Gold > 0) bindings.Add(item => item.BindCurrency(false, reward.Gold, true));
            if (reward.Diamond > 0) bindings.Add(item => item.BindCurrency(true, reward.Diamond, true));
            foreach (KeyValuePair<ItemData, int> pair in reward.Fragments)
            {
                ItemData item = pair.Key;
                int amount = pair.Value;
                if (item != null && amount > 0) bindings.Add(view => view.BindItemFragment(item, amount, true));
            }

            revealRoutine = StartCoroutine(RevealRewards(bindings));
            animationRoutine = StartCoroutine(HideAnimationAfterDelay());
        }

        IEnumerator RevealRewards(IReadOnlyList<System.Action<ItemReward>> bindings)
        {
            if (root == null) yield break;
            root.gameObject.SetActive(true);
            for (int i = 0; i < bindings.Count; i++)
            {
                ItemReward item = GetRewardItem(i);
                if (item == null) continue;
                item.gameObject.SetActive(false);
                bindings[i](item);
                item.gameObject.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(root as RectTransform);
                if (i < bindings.Count - 1) yield return new WaitForSeconds(revealInterval);
            }
            revealRoutine = null;
        }

        IEnumerator HideAnimationAfterDelay()
        {
            yield return new WaitForSeconds(animInterval);
            if (objAnimation != null) objAnimation.SetActive(false);
            animationRoutine = null;
        }

        ItemReward GetRewardItem(int itemIndex)
        {
            if (root == null) return null;
            if (rewardPrefab == null)
                rewardPrefab = Resources.Load<ItemReward>("PlanetWar/OriginalSettlement/Prefab/ItemReward");
            if (rewardPrefab == null) return null;

            while (spawned.Count <= itemIndex)
            {
                ItemReward instance = Instantiate(rewardPrefab, root);
                instance.name = "ItemReward";
                instance.gameObject.SetActive(false);
                spawned.Add(instance);
            }
            return spawned[itemIndex];
        }

        void ResetRewardItems()
        {
            foreach (ItemReward item in spawned)
            {
                if (item == null) continue;
                item.ResetForReuse();
                item.gameObject.SetActive(false);
            }
        }

        void StopPresentationRoutines()
        {
            if (revealRoutine != null) StopCoroutine(revealRoutine);
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            revealRoutine = null;
            animationRoutine = null;
        }

        void OnDisable()
        {
            StopPresentationRoutines();
            ResetRewardItems();
        }

        static void PlayAnimation(SkeletonGraphic skeleton)
        {
            if (skeleton != null && skeleton.AnimationState != null)
                skeleton.AnimationState.SetAnimation(0, "animation", false);
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
