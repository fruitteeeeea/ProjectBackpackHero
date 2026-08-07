using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BackpackHero.Battle;

namespace BackpackPrototype
{
    public sealed class PreparationActionsUI : MonoBehaviour
    {
        [SerializeField]
        private PlayerBackpackSystem playerBackpackSystem;

        [SerializeField]
        private Button rollButton;

        [SerializeField]
        private TMP_Text rollLabel;

        private PlayerBackpackSystem System =>
            playerBackpackSystem != null
                ? playerBackpackSystem
                : playerBackpackSystem =
                    GetComponentInParent<
                        PlayerBackpackSystem>(true);

        private void OnEnable()
        {
            BattleFlowController.PhaseChanged += HandlePhaseChanged;
            SubscribeToSystem();
            RefreshRollPresentation();
        }

        private void Start()
        {
            SubscribeToSystem();
            RefreshRollPresentation();
        }

        private void OnDisable()
        {
            BattleFlowController.PhaseChanged -= HandlePhaseChanged;
            if (playerBackpackSystem != null)
            {
                playerBackpackSystem.RollStateChanged -=
                    RefreshRollPresentation;
            }
        }

        public void RefreshShop()
        {
            if (System == null)
            {
                Debug.LogWarning(
                    "无法刷新商店：PlayerBackpackSystem未就绪。",
                    this);
                return;
            }

            System.RefreshShop();
            RefreshRollPresentation();
        }

        public void TogglePhase()
        {
            if (System == null)
            {
                Debug.LogWarning(
                    "无法进入战斗：PlayerBackpackSystem未就绪。",
                    this);
                return;
            }

            System.EnterCombat();
        }

        private void SubscribeToSystem()
        {
            PlayerBackpackSystem system = System;
            if (system == null)
            {
                return;
            }

            system.RollStateChanged -= RefreshRollPresentation;
            system.RollStateChanged += RefreshRollPresentation;
        }

        private void HandlePhaseChanged(BattlePhase _)
        {
            RefreshRollPresentation();
        }

        private void RefreshRollPresentation()
        {
            PlayerBackpackSystem system = System;
            if (system == null)
            {
                return;
            }

            if (rollLabel != null)
            {
                rollLabel.text =
                    $"Roll {system.RemainingRolls}/" +
                    system.RollsPerPreparation;
            }

            if (rollButton != null)
            {
                rollButton.interactable = system.CanRollShop;
            }
        }
    }
}
