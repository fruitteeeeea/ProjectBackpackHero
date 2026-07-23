using System;
using UnityEngine;

namespace BackpackHero.Input
{
    /// <summary>
    /// Runtime-safe bridge between the swipe input and editor-only debugging tools.
    /// </summary>
    public static class HorizontalSwipeCurveDebugBridge
    {
        private static HorizontalSwipeCurveDebugRuntime current;
        private static CurvedConnectionRenderer currentCurve;

        public static event Action TargetAvailable;
        public static event Action TargetUnavailable;

        public static bool HasTarget => Current != null;
        public static HorizontalSwipeCurveDebugRuntime Current => current;
        public static HorizontalSwipeCurveInput Target => Current != null ? Current.Input : null;
        public static float CurrentValue => Target != null ? Target.CurrentValue : 0f;
        public static float Sensitivity => Target != null ? Target.SwipeSensitivity : 1f;
        public static bool IsInputEnabled => Target != null && Target.IsInputEnabled;
        public static bool HasCurveTarget => CurveTarget != null;
        public static CurvedConnectionRenderer CurveTarget => currentCurve;
        public static float MaxBendDistance => CurveTarget != null ? CurveTarget.MaxBendDistance : 0f;
        public static int SegmentCount => CurveTarget != null ? CurveTarget.SegmentCount : 0;

        public static void Register(HorizontalSwipeCurveDebugRuntime runtime)
        {
            if (runtime == null || current == runtime)
            {
                return;
            }

            current = runtime;
            TargetAvailable?.Invoke();
        }

        public static void Unregister(HorizontalSwipeCurveDebugRuntime runtime)
        {
            if (runtime == null || current != runtime)
            {
                return;
            }

            current = null;
            TargetUnavailable?.Invoke();
        }

        public static void SetInputEnabled(bool enabled)
        {
            Target?.SetInputEnabled(enabled);
        }

        public static void SetSensitivity(float sensitivity)
        {
            if (Target != null)
            {
                Target.SwipeSensitivity = sensitivity;
            }
        }

        public static void RegisterCurve(CurvedConnectionRenderer curve)
        {
            if (curve != null)
            {
                currentCurve = curve;
            }
        }

        public static void UnregisterCurve(CurvedConnectionRenderer curve)
        {
            if (curve != null && currentCurve == curve)
            {
                currentCurve = null;
            }
        }

        public static void SetMaxBendDistance(float distance)
        {
            if (CurveTarget != null)
            {
                CurveTarget.MaxBendDistance = distance;
            }
        }

        public static void SetSegmentCount(int count)
        {
            if (CurveTarget != null)
            {
                CurveTarget.SegmentCount = count;
            }
        }

        public static void ResetValue()
        {
            if (Target == null)
            {
                return;
            }

            Target.SetInputEnabled(false);
            Target.SetInputEnabled(true);
        }
    }

    [DisallowMultipleComponent]
    public sealed class HorizontalSwipeCurveDebugRuntime : MonoBehaviour
    {
        [SerializeField] private HorizontalSwipeCurveInput input;

        public HorizontalSwipeCurveInput Input => input;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureDebugRuntimeExists()
        {
            if (FindAnyObjectByType<HorizontalSwipeCurveDebugRuntime>() != null)
            {
                return;
            }

            var existingInput = FindAnyObjectByType<HorizontalSwipeCurveInput>();
            if (existingInput != null)
            {
                existingInput.gameObject.AddComponent<HorizontalSwipeCurveDebugRuntime>();
                return;
            }

            var debugObject = new GameObject("Horizontal Swipe Curve Debug Runtime");
            DontDestroyOnLoad(debugObject);
            debugObject.AddComponent<HorizontalSwipeCurveInput>();
            debugObject.AddComponent<HorizontalSwipeCurveDebugRuntime>();
        }
#endif

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponent<HorizontalSwipeCurveInput>();
            }

            if (input == null)
            {
                input = gameObject.AddComponent<HorizontalSwipeCurveInput>();
            }
        }

        private void OnEnable()
        {
            HorizontalSwipeCurveDebugBridge.Register(this);
            input?.SetInputEnabled(true);
        }

        private void OnDisable()
        {
            input?.SetInputEnabled(false);
            HorizontalSwipeCurveDebugBridge.Unregister(this);
        }
    }
}
