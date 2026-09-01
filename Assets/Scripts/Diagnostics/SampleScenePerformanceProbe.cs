// Temporary performance instrumentation for SampleScene.
// It is compiled out of non-development player builds and can be deleted once
// the performance investigation is complete.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace BackpackHero.Diagnostics
{
    /// <summary>
    /// Emits compact, low-frequency runtime performance samples for SampleScene.
    /// Use the Unity Profiler for the call tree after this probe identifies the
    /// category that correlates with long frames.
    /// </summary>
    internal sealed class SampleScenePerformanceProbe : MonoBehaviour
    {
        private const string LogPrefix = "[PERF-SAMPLE]";
        private const float SummaryIntervalSeconds = 5f;
        private const float SpikeThresholdMilliseconds = 33.3f;
        private const int FrameHistorySize = 300;

        private readonly float[] frameTimes = new float[FrameHistorySize];
        private readonly float[] percentileBuffer = new float[FrameHistorySize];

        private static SampleScenePerformanceProbe instance;

        private ProfilerRecorder mainThreadRecorder;
        private ProfilerRecorder behaviourUpdateRecorder;
        private ProfilerRecorder physicsRecorder;
        private ProfilerRecorder renderRecorder;
        private ProfilerRecorder waitForPresentRecorder;
        private ProfilerRecorder waitForTargetFpsRecorder;
        private ProfilerRecorder canvasRecorder;
        private ProfilerRecorder canvasBuildRecorder;
        private ProfilerRecorder canvasUpdateBatchesRecorder;
        private ProfilerRecorder canvasCallbacksRecorder;
        private ProfilerRecorder operationPlannerRecorder;
        private ProfilerRecorder resolveTargetsRecorder;
        private ProfilerRecorder uiSfxScanRecorder;
        private ProfilerRecorder sfxSettingsRecorder;
        private ProfilerRecorder debugSnapshotRecorder;
        private ProfilerRecorder gcAllocRecorder;

        private int frameHistoryCount;
        private int frameHistoryIndex;
        private int framesSinceSummary;
        private int gcCollectionsAtLastSummary;
        private float frameTimeSum;
        private float nextSummaryTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForSampleScene()
        {
            if (SceneManager.GetActiveScene().name != "SampleScene")
            {
                return;
            }

            if (instance != null)
            {
                return;
            }

            foreach (SampleScenePerformanceProbe existing in
                     Resources.FindObjectsOfTypeAll<SampleScenePerformanceProbe>())
            {
                if (existing != null)
                {
                    Destroy(existing.gameObject);
                }
            }

            var probeObject = new GameObject("SampleScenePerformanceProbe");
            probeObject.hideFlags = HideFlags.HideInHierarchy;
            probeObject.AddComponent<SampleScenePerformanceProbe>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            mainThreadRecorder = StartRecorder(ProfilerCategory.Internal, "Main Thread");
            behaviourUpdateRecorder = StartRecorder(ProfilerCategory.Scripts, "BehaviourUpdate");
            physicsRecorder = StartRecorder(ProfilerCategory.Physics, "Physics.Simulate");
            renderRecorder = StartRecorder(ProfilerCategory.Render, "Camera.Render");
            waitForPresentRecorder = StartRecorder(ProfilerCategory.Render,
                "Gfx.WaitForPresentOnGfxThread");
            waitForTargetFpsRecorder = StartRecorder(ProfilerCategory.Internal,
                "WaitForTargetFPS");
            canvasRecorder = StartRecorder(ProfilerCategory.Gui,
                "Canvas.SendWillRenderCanvases");
            operationPlannerRecorder = StartRecorder(ProfilerCategory.Scripts,
                "SampleScenePerf.BackpackOperationPlanner.TrySelectBest");
            resolveTargetsRecorder = StartRecorder(ProfilerCategory.Scripts,
                "SampleScenePerf.LevelFlowController.ResolveTargets");
            uiSfxScanRecorder = StartRecorder(ProfilerCategory.Scripts,
                "SampleScenePerf.UiSfxAutoBinder.Scan");
            sfxSettingsRecorder = StartRecorder(ProfilerCategory.Scripts,
                "SampleScenePerf.SfxSettingsBridge.Update");
            debugSnapshotRecorder = StartRecorder(ProfilerCategory.Scripts,
                "SampleScenePerf.PlayerBackpackDebugBridge.RefreshSnapshot");
            gcAllocRecorder = StartRecorder(ProfilerCategory.Memory, "GC.Alloc");
            ConfigureCanvasRecorders();

            nextSummaryTime = Time.unscaledTime + SummaryIntervalSeconds;
            gcCollectionsAtLastSummary = GC.CollectionCount(0);

            Debug.Log(
                $"{LogPrefix} Started scene=SampleScene targetFrameMs={SpikeThresholdMilliseconds:F1} " +
                $"renderers={FindObjectsByType<Renderer>().Length} " +
                $"behaviours={FindObjectsByType<MonoBehaviour>().Length} " +
                $"collider2D={FindObjectsByType<Collider2D>().Length}.");
        }

        private void Update()
        {
            float frameMilliseconds = Time.unscaledDeltaTime * 1000f;
            RecordFrame(frameMilliseconds);
            framesSinceSummary++;

            if (frameMilliseconds >= SpikeThresholdMilliseconds)
            {
                Debug.Log(
                    $"{LogPrefix} Spike frame={Time.frameCount} frameMs={frameMilliseconds:F1} " +
                    $"gcAlloc={BytesToKb(GetLastValue(gcAllocRecorder)):F1}KB " +
                    $"main={FormatMilliseconds(mainThreadRecorder)} " +
                    $"scripts={FormatMilliseconds(behaviourUpdateRecorder)} " +
                    $"physics={FormatMilliseconds(physicsRecorder)} " +
                    $"render={FormatMilliseconds(renderRecorder)} " +
                    $"presentWait={FormatMilliseconds(waitForPresentRecorder)} " +
                    $"targetWait={FormatMilliseconds(waitForTargetFpsRecorder)} " +
                    $"canvas={FormatMilliseconds(canvasRecorder)} " +
                    $"canvasBuild={FormatMilliseconds(canvasBuildRecorder)} " +
                    $"canvasBatches={FormatMilliseconds(canvasUpdateBatchesRecorder)} " +
                    $"canvasCallbacks={FormatMilliseconds(canvasCallbacksRecorder)} " +
                    $"planner={FormatMilliseconds(operationPlannerRecorder)} " +
                    $"targetScan={FormatMilliseconds(resolveTargetsRecorder)} " +
                    $"uiSfxScan={FormatMilliseconds(uiSfxScanRecorder)} " +
                    $"sfxSettings={FormatMilliseconds(sfxSettingsRecorder)} " +
                    $"debugSnapshot={FormatMilliseconds(debugSnapshotRecorder)} " +
                    $"monoUsed={BytesToMb(Profiler.GetMonoUsedSizeLong()):F1}MB.");
            }

            if (Time.unscaledTime >= nextSummaryTime)
            {
                LogSummary();
                nextSummaryTime = Time.unscaledTime + SummaryIntervalSeconds;
            }
        }

        private void OnDestroy()
        {
            LogSummary();
            DisposeRecorder(ref mainThreadRecorder);
            DisposeRecorder(ref behaviourUpdateRecorder);
            DisposeRecorder(ref physicsRecorder);
            DisposeRecorder(ref renderRecorder);
            DisposeRecorder(ref waitForPresentRecorder);
            DisposeRecorder(ref waitForTargetFpsRecorder);
            DisposeRecorder(ref canvasRecorder);
            DisposeRecorder(ref canvasBuildRecorder);
            DisposeRecorder(ref canvasUpdateBatchesRecorder);
            DisposeRecorder(ref canvasCallbacksRecorder);
            DisposeRecorder(ref operationPlannerRecorder);
            DisposeRecorder(ref resolveTargetsRecorder);
            DisposeRecorder(ref uiSfxScanRecorder);
            DisposeRecorder(ref sfxSettingsRecorder);
            DisposeRecorder(ref debugSnapshotRecorder);
            DisposeRecorder(ref gcAllocRecorder);
            if (instance == this)
            {
                instance = null;
            }
        }

        private void RecordFrame(float frameMilliseconds)
        {
            if (frameHistoryCount == FrameHistorySize)
            {
                frameTimeSum -= frameTimes[frameHistoryIndex];
            }
            else
            {
                frameHistoryCount++;
            }

            frameTimes[frameHistoryIndex] = frameMilliseconds;
            frameTimeSum += frameMilliseconds;
            frameHistoryIndex = (frameHistoryIndex + 1) % FrameHistorySize;
        }

        private void LogSummary()
        {
            if (frameHistoryCount == 0)
            {
                return;
            }

            Array.Copy(frameTimes, percentileBuffer, frameHistoryCount);
            Array.Sort(percentileBuffer, 0, frameHistoryCount);
            int p95Index = Mathf.Clamp(Mathf.CeilToInt(frameHistoryCount * 0.95f) - 1, 0, frameHistoryCount - 1);
            int gcCollections = GC.CollectionCount(0);

            Debug.Log(
                $"{LogPrefix} Summary frames={framesSinceSummary} avg={frameTimeSum / frameHistoryCount:F1}ms " +
                $"p95={percentileBuffer[p95Index]:F1}ms max={percentileBuffer[frameHistoryCount - 1]:F1}ms " +
                $"gcGen0={gcCollections - gcCollectionsAtLastSummary} " +
                $"monoUsed={BytesToMb(Profiler.GetMonoUsedSizeLong()):F1}MB " +
                $"totalAllocated={BytesToMb(Profiler.GetTotalAllocatedMemoryLong()):F1}MB.");

            framesSinceSummary = 0;
            gcCollectionsAtLastSummary = gcCollections;
        }

        private static ProfilerRecorder StartRecorder(ProfilerCategory category, string markerName)
        {
            try
            {
                return ProfilerRecorder.StartNew(category, markerName, 1);
            }
            catch (ArgumentException)
            {
                return default;
            }
        }

        private void ConfigureCanvasRecorders()
        {
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);

            var markerNames = new List<string>();
            foreach (ProfilerRecorderHandle handle in handles)
            {
                ProfilerRecorderDescription description =
                    ProfilerRecorderHandle.GetDescription(handle);
                string name = description.Name;
                if (!string.IsNullOrEmpty(name) &&
                    (name.IndexOf("canvas", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     name.IndexOf("ui.", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     name.IndexOf("uire", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    markerNames.Add(name);
                }

                if (string.Equals(name, "Canvas.BuildBatch",
                        StringComparison.OrdinalIgnoreCase))
                {
                    canvasBuildRecorder = StartRecorder(handle);
                }
                else if (string.Equals(name, "UGUI.Rendering.UpdateBatches",
                             StringComparison.OrdinalIgnoreCase))
                {
                    canvasUpdateBatchesRecorder = StartRecorder(handle);
                }
                else if (string.Equals(name, "UIEvents.WillRenderCanvases",
                             StringComparison.OrdinalIgnoreCase))
                {
                    canvasCallbacksRecorder = StartRecorder(handle);
                }
            }

            markerNames.Sort(StringComparer.Ordinal);
            Debug.Log($"{LogPrefix} Available UI markers: " +
                (markerNames.Count == 0
                    ? "<none>"
                    : string.Join(" | ", markerNames)));
        }

        private static ProfilerRecorder StartRecorder(ProfilerRecorderHandle handle)
        {
            try
            {
                var recorder = new ProfilerRecorder(handle, 1);
                recorder.Start();
                return recorder;
            }
            catch (ArgumentException)
            {
                return default;
            }
        }

        private static long GetLastValue(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue : 0L;
        }

        private static string FormatMilliseconds(ProfilerRecorder recorder)
        {
            return recorder.Valid
                ? $"{NanosecondsToMilliseconds(recorder.LastValue):F1}ms"
                : "n/a";
        }

        private static void DisposeRecorder(ref ProfilerRecorder recorder)
        {
            if (recorder.Valid)
            {
                recorder.Dispose();
            }
        }

        private static float NanosecondsToMilliseconds(long value)
        {
            return value / 1000000f;
        }

        private static float BytesToKb(long value)
        {
            return value / 1024f;
        }

        private static float BytesToMb(long value)
        {
            return value / (1024f * 1024f);
        }
    }
}
#endif
