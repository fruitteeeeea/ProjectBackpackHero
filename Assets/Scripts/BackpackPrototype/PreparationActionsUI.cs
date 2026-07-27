using UnityEngine;

namespace BackpackPrototype
{
    public sealed class PreparationActionsUI : MonoBehaviour
    {
        [SerializeField]
        private PlayerBackpackSystem playerBackpackSystem;

        private PlayerBackpackSystem System =>
            playerBackpackSystem != null
                ? playerBackpackSystem
                : playerBackpackSystem =
                    GetComponentInParent<
                        PlayerBackpackSystem>(true);

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
    }
}
