using UnityEngine;

namespace BackpackPrototype
{
    public sealed class PreparationActionsUI : MonoBehaviour
    {
        public void RefreshShop()
        {
            BackpackDebugRuntime runtime =
                BackpackDebugRuntime.Instance;

            if (runtime == null)
            {
                Debug.LogWarning(
                    "无法刷新商店：BackpackDebugRuntime未就绪。",
                    this);
                return;
            }

            runtime.RefreshShop();
        }

        public void TogglePhase()
        {
            BackpackDebugRuntime runtime =
                BackpackDebugRuntime.Instance;

            if (runtime == null)
            {
                Debug.LogWarning(
                    "无法切换阶段：BackpackDebugRuntime未就绪。",
                    this);
                return;
            }

            runtime.TogglePhase();
        }
    }
}