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
        bool settled;
        PresentationState presentationState;
        readonly List<ItemReward> spawned = new();
        Coroutine revealRoutine;
        Coroutine animationRoutine;
        TrackEntry openEntry;
        ItemReward rewardPrefab;

        enum PresentationState
        {
            WaitingToStart,
            OpeningAnimation,
            RevealingRewards,
            ReadyToClose,
        }

        internal void Initialize(PackMenuPresenter value) => presenter = value;

        public void ShowOpen(int slot)
        {
            StopPresentationRoutines();
            ResetRewardItems();
            index = slot;
            presentationState = PresentationState.WaitingToStart;
            settled = false;
            gameObject.SetActive(true);
            if (txtTitle != null) txtTitle.text = "Tap to skip";
            if (packSpine != null) packSpine.gameObject.SetActive(true);
            if (spine != null) spine.gameObject.SetActive(false);
            if (root != null) root.gameObject.SetActive(false);
            if (objAnimation != null) objAnimation.SetActive(false);
            StartPackAnimation("wait", true);
        }

        public void OnClickBg()
        {
            switch (presentationState)
            {
                case PresentationState.WaitingToStart:
                    // First tap starts the normal pack-opening animation.
                    PlayOpenAnimation();
                    break;
                case PresentationState.OpeningAnimation:
                    // A subsequent tap skips only the opening animation. It must still
                    // settle and present every reward before this page can be closed.
                    DetachOpenAnimationCallback();
                    EnterRewardPresentation();
                    break;
                case PresentationState.RevealingRewards:
                    // The same tap which skips the animation, and any later taps while
                    // rewards are appearing, must not close the reward screen.
                    break;
                case PresentationState.ReadyToClose:
                    gameObject.SetActive(false);
                    break;
            }
        }

        // The first tap starts the normal open flow. Its completion and a later
        // animation skip both share the same settlement and reward-reveal path.
        public void PlayOpenAnimation()
        {
            if (presentationState != PresentationState.WaitingToStart) return;
            presentationState = PresentationState.OpeningAnimation;
            DetachOpenAnimationCallback();
            openEntry = StartPackAnimation("open", false);
            if (openEntry == null)
            {
                EnterRewardPresentation();
                return;
            }

            openEntry.Complete += OnOpenCompleted;
        }

        void OnOpenCompleted(TrackEntry entry)
        {
            entry.Complete -= OnOpenCompleted;
            if (ReferenceEquals(openEntry, entry)) openEntry = null;
            EnterRewardPresentation();
        }

        void DetachOpenAnimationCallback()
        {
            if (openEntry == null) return;
            openEntry.Complete -= OnOpenCompleted;
            openEntry = null;
        }

        void EnterRewardPresentation()
        {
            if (settled || presentationState != PresentationState.OpeningAnimation) return;
            presentationState = PresentationState.RevealingRewards;
            if (presenter == null || !presenter.Packs.TrySettleReward(index, out PackReward reward))
            {
                // Do not silently dismiss to the main menu: keeping this page visible
                // makes an invalid slot state diagnosable and allows a retry.
                presentationState = PresentationState.WaitingToStart;
                Debug.LogError($"[PackReward] Unable to settle pack reward for slot {index}.", this);
                return;
            }
            settled = true;

            if (packSpine != null) packSpine.gameObject.SetActive(false);
            if (spine != null) spine.gameObject.SetActive(true);
            if (root != null) root.gameObject.SetActive(true);
            if (objAnimation != null) objAnimation.SetActive(true);
            if (txtTitle != null) txtTitle.text = string.Empty;
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
            if (root == null)
            {
                FinishRewardReveal();
                yield break;
            }
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
            FinishRewardReveal();
        }

        void FinishRewardReveal()
        {
            revealRoutine = null;
            presentationState = PresentationState.ReadyToClose;
            if (txtTitle != null) txtTitle.text = "Click to Continue";
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
            DetachOpenAnimationCallback();
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

        /// <summary>
        /// The reward view is reused between packs. Reset the old track and pose before
        /// starting a new pack animation so its first frame is never blended from the
        /// last frame of a previous wait/open animation.
        /// </summary>
        TrackEntry StartPackAnimation(string animationName, bool loop)
        {
            if (packSpine == null) return null;
            if (packSpine.Skeleton == null || packSpine.AnimationState == null)
                packSpine.Initialize(true);
            if (packSpine.Skeleton == null || packSpine.AnimationState == null) return null;

            Spine.AnimationState state = packSpine.AnimationState;
            state.ClearTracks();
            SetPackSkin();
            packSpine.Skeleton.SetToSetupPose();

            TrackEntry entry = state.SetAnimation(0, animationName, loop);
            if (entry == null) return null;
            entry.TrackTime = 0f;
            entry.TimeScale = 1f;
            entry.MixDuration = 0f;
            state.Apply(packSpine.Skeleton);
            packSpine.Update(0f);
            return entry;
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
