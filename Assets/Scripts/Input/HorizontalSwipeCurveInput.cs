using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BackpackHero.Input
{
    /// <summary>
    /// Converts a full-screen horizontal drag into a value between -1 and 1.
    /// Input must be explicitly enabled by the owning gameplay system.
    /// </summary>
    public sealed class HorizontalSwipeCurveInput : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float swipeSensitivity = 25f;

        private readonly HorizontalSwipeValueModel model = new();
        private PointerKind activePointerKind;

        public float CurrentValue => model.CurrentValue;
        public bool IsInputEnabled => model.IsEnabled;

        public float SwipeSensitivity
        {
            get => swipeSensitivity;
            set => swipeSensitivity = Mathf.Max(0f, value);
        }

        public event Action<float> ValueChanged;

        /// <summary>
        /// Enables or disables gesture input. Transitioning to enabled resets the value to zero.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            if (enabled == model.IsEnabled)
            {
                return;
            }

            activePointerKind = PointerKind.None;

            if (enabled)
            {
                model.Enable();
                ValueChanged?.Invoke(model.CurrentValue);
            }
            else
            {
                model.Disable();
            }
        }

        private void Update()
        {
            if (!model.IsEnabled)
            {
                return;
            }

            if (model.HasActivePointer)
            {
                UpdateActivePointer();
                return;
            }

            TryBeginTouch();

            if (!model.HasActivePointer)
            {
                TryBeginMouse();
            }
        }

        private void TryBeginTouch()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return;
            }

            foreach (var touch in touchscreen.touches)
            {
                if (!touch.press.wasPressedThisFrame)
                {
                    continue;
                }

                var touchId = touch.touchId.ReadValue();
                model.TryBegin(touchId, IsPointerOverUi(touchId));
                activePointerKind = PointerKind.Touch;
                return;
            }
        }

        private void TryBeginMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            const int mousePointerId = -1;
            model.TryBegin(mousePointerId, IsPointerOverUi(mousePointerId));
            activePointerKind = PointerKind.Mouse;
        }

        private void UpdateActivePointer()
        {
            switch (activePointerKind)
            {
                case PointerKind.Touch:
                    UpdateActiveTouch();
                    break;
                case PointerKind.Mouse:
                    UpdateActiveMouse();
                    break;
                default:
                    CancelGesture();
                    break;
            }
        }

        private void UpdateActiveTouch()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                CancelGesture();
                return;
            }

            foreach (var touch in touchscreen.touches)
            {
                if (touch.touchId.ReadValue() != model.ActivePointerId)
                {
                    continue;
                }

                ApplyDelta(touch.delta.x.ReadValue());

                if (touch.press.wasReleasedThisFrame || !touch.press.isPressed)
                {
                    EndGesture();
                }

                return;
            }

            CancelGesture();
        }

        private void UpdateActiveMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                CancelGesture();
                return;
            }

            ApplyDelta(mouse.delta.x.ReadValue());

            if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed)
            {
                EndGesture();
            }
        }

        private void ApplyDelta(float horizontalDelta)
        {
            if (model.TryApplyDelta(model.ActivePointerId, horizontalDelta, Screen.width, swipeSensitivity))
            {
                ValueChanged?.Invoke(model.CurrentValue);
            }
        }

        private void EndGesture()
        {
            model.End(model.ActivePointerId);
            activePointerKind = PointerKind.None;
        }

        private void CancelGesture()
        {
            model.Cancel();
            activePointerKind = PointerKind.None;
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);
        }

        private void OnDisable()
        {
            CancelGesture();
        }

        private enum PointerKind
        {
            None,
            Touch,
            Mouse,
        }
    }

    public sealed class HorizontalSwipeValueModel
    {
        public float CurrentValue { get; private set; }
        public bool IsEnabled { get; private set; }
        public bool HasActivePointer { get; private set; }
        public int ActivePointerId { get; private set; }

        public void Enable()
        {
            IsEnabled = true;
            CurrentValue = 0f;
            Cancel();
        }

        public void Disable()
        {
            IsEnabled = false;
            Cancel();
        }

        public bool TryBegin(int pointerId, bool beganOverUi)
        {
            if (!IsEnabled || HasActivePointer || beganOverUi)
            {
                return false;
            }

            ActivePointerId = pointerId;
            HasActivePointer = true;
            return true;
        }

        public bool TryApplyDelta(int pointerId, float horizontalDelta, float screenWidth, float sensitivity)
        {
            if (!IsEnabled || !HasActivePointer || pointerId != ActivePointerId || screenWidth <= 0f)
            {
                return false;
            }

            var nextValue = Mathf.Clamp(CurrentValue + horizontalDelta / screenWidth * Mathf.Max(0f, sensitivity), -1f, 1f);
            if (Mathf.Approximately(nextValue, CurrentValue))
            {
                return false;
            }

            CurrentValue = nextValue;
            return true;
        }

        public void End(int pointerId)
        {
            if (HasActivePointer && pointerId == ActivePointerId)
            {
                Cancel();
            }
        }

        public void Cancel()
        {
            HasActivePointer = false;
            ActivePointerId = 0;
        }
    }
}
