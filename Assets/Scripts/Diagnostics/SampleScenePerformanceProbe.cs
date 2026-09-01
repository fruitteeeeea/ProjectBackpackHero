// Temporary performance instrumentation for SampleScene.
// It is compiled out of non-development player builds and can be deleted once
// the performance investigation is complete.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Unity.Profiling;
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

        private ProfilerRecorder mainThreadRecorder;
        private ProfilerRecorder behaviourUpdateRecorder;
        private ProfilerRecorder physicsRecorder;
        private ProfilerRecorder renderRecorder;
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

            var probeObject = new GameObject("SampleScenePerformanceProbe");
            probeObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(probeObject);
            probeObject.AddComponent<SampleScenePerformanceProbe>();
        }

        private void Awake()
        {
            mainThreadRecorder = StartRecorder(ProfilerCategory.Internal, "Main Thread");
            behaviourUpdateRecorder = StartRecorder(ProfilerCategory.Scripts, "BehaviourUpdate");
            physicsRecorder = StartRecorder(ProfilerCategory.Physics, "Physics.Simulate");
            renderRecorder = StartRecorder(ProfilerCategory.Render, "Camera.Render");
            gcAllocRecorder = StartRecorder(ProfilerCategory.Memory, "GC.Alloc");

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
                    $"main={NanosecondsToMilliseconds(GetLastValue(mainThreadRecorder)):F1}ms " +
                    $"scripts={NanosecondsToMilliseconds(GetLastValue(behaviourUpdateRecorder)):F1}ms " +
                    $"physics={NanosecondsToMilliseconds(GetLastValue(physicsRecorder)):F1}ms " +
                    $"render={NanosecondsToMilliseconds(GetLastValue(renderRecorder)):F1}ms " +
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
            DisposeRecorder(ref gcAllocRecorder);
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

        private static long GetLastValue(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue : 0L;
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
