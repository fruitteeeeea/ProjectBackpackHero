using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class BuildAndroidForTesting
{
    public const string MainMenuScenePath =
        "Assets/Samples/PlanetWar Reusable Main Menu/0.1.0/" +
        "Main Menu Demo/MainMenuDemo.unity";
    public const string GameScenePath =
        "Assets/Scenes/SampleScene.unity";
    public const string IconPath =
        "Assets/Art/Images/icon.png";
    public const string OutputRelativePath =
        "Builds/Android/Development/ProjectBackpackHero-dev.apk";

    private const string CompanyName = "Lantern Fox Games";
    private const string ProductName = "ProjectBackpackHero";
    private const string PackageName =
        "com.lanternfoxgames.projectbackpackhero";

    [MenuItem("工具/Android/构建测试 APK", priority = 110)]
    public static void BuildFromMenu()
    {
        Build();
    }

    /// <summary>
    /// Command-line entry point for Unity's -executeMethod option.
    /// </summary>
    public static void Build()
    {
        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new BuildFailedException(
                    "请先退出 Play Mode，再构建 Android APK。");
            }

            ValidateInputs();
            ConfigureProjectSettings();

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                    NamedBuildTarget.Android,
                    BuildTarget.Android))
            {
                throw new BuildFailedException(
                    "无法切换到 Android 构建平台。");
            }

            PrepareOutputDirectory();
            string outputPath = GetOutputPath();
            var options = new BuildPlayerOptions
            {
                scenes = GetBuildScenes(),
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            Debug.Log(
                $"开始构建 Android 测试 APK：{outputPath}");
            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result != BuildResult.Succeeded ||
                report.summary.totalErrors != 0)
            {
                throw new BuildFailedException(
                    "Android 构建失败：" +
                    $"{report.summary.result}，错误 " +
                    $"{report.summary.totalErrors}，警告 " +
                    $"{report.summary.totalWarnings}。");
            }

            if (!File.Exists(outputPath) ||
                new FileInfo(outputPath).Length == 0)
            {
                throw new BuildFailedException(
                    $"构建报告成功，但 APK 不存在或为空：{outputPath}");
            }

            Debug.Log(
                "Android 测试 APK 构建成功：" +
                $"{outputPath} ({report.summary.totalSize} bytes)");

            if (ShouldShowDialog())
            {
                EditorUtility.DisplayDialog(
                    "Android 构建成功",
                    $"测试 APK 已生成：\n{outputPath}",
                    "确定");
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (ShouldShowDialog())
            {
                EditorUtility.DisplayDialog(
                    "Android 构建失败",
                    exception.Message,
                    "确定");
            }

            throw;
        }
    }

    public static void ConfigureProjectSettings()
    {
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.productName = ProductName;
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.SetApplicationIdentifier(
            NamedBuildTarget.Android,
            PackageName);

        PlayerSettings.defaultInterfaceOrientation =
            UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.minSdkVersion =
            AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion =
            AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.targetArchitectures =
            AndroidArchitecture.ARM64;
        PlayerSettings.Android.buildApkPerCpuArchitecture = false;
        PlayerSettings.Android.resizeableActivity = false;
        PlayerSettings.Android.useCustomKeystore = false;
        PlayerSettings.Android.minifyDebug = false;
        PlayerSettings.Android.minifyRelease = false;
        PlayerSettings.SetScriptingBackend(
            NamedBuildTarget.Android,
            ScriptingImplementation.IL2CPP);

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;
        EditorUserBuildSettings.buildWithDeepProfilingSupport = false;

        EditorBuildSettings.scenes = GetBuildScenes()
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();

        ConfigureAndroidIcons();
        AssetDatabase.SaveAssets();
    }

    public static string[] GetBuildScenes()
    {
        return new[]
        {
            MainMenuScenePath,
            GameScenePath,
        };
    }

    private static void ConfigureAndroidIcons()
    {
        Texture2D iconTexture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (iconTexture == null)
        {
            throw new BuildFailedException(
                $"无法加载 Android 图标：{IconPath}");
        }

        int configuredSlots = 0;
        PlatformIconKind[] kinds =
            PlayerSettings.GetSupportedIconKinds(
                NamedBuildTarget.Android);

        foreach (PlatformIconKind kind in kinds)
        {
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(
                NamedBuildTarget.Android,
                kind);

            // Adaptive icons require separate foreground/background assets.
            // This test build intentionally configures only single-layer
            // legacy and round icon slots from the supplied combined image.
            if (icons.Length == 0 ||
                icons.Any(icon => icon.maxLayerCount != 1))
            {
                continue;
            }

            foreach (PlatformIcon icon in icons)
            {
                icon.SetTexture(iconTexture, 0);
                configuredSlots++;
            }

            PlayerSettings.SetPlatformIcons(
                NamedBuildTarget.Android,
                kind,
                icons);
        }

        if (configuredSlots == 0)
        {
            throw new BuildFailedException(
                "Unity 没有返回可配置的 Android 单层图标槽。");
        }

        Debug.Log(
            $"已配置 {configuredSlots} 个 Android 图标槽：{IconPath}");
    }

    private static void ValidateInputs()
    {
        foreach (string scenePath in GetBuildScenes())
        {
            if (!File.Exists(Path.Combine(
                    GetProjectRoot(),
                    scenePath)))
            {
                throw new BuildFailedException(
                    $"未找到构建场景：{scenePath}");
            }
        }

        if (!File.Exists(Path.Combine(
                GetProjectRoot(),
                IconPath)))
        {
            throw new BuildFailedException(
                $"未找到 Android 图标：{IconPath}");
        }
    }

    private static void PrepareOutputDirectory()
    {
        string projectRoot = GetProjectRoot();
        string outputDirectory = Path.GetDirectoryName(
            GetOutputPath());
        string buildsRoot = Path.GetFullPath(
            Path.Combine(projectRoot, "Builds")) +
            Path.DirectorySeparatorChar;

        if (string.IsNullOrEmpty(outputDirectory) ||
            !outputDirectory.StartsWith(
                buildsRoot,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"拒绝清理 Builds 目录外的路径：{outputDirectory}");
        }

        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(outputDirectory, true);
        }

        Directory.CreateDirectory(outputDirectory);
    }

    private static string GetOutputPath()
    {
        return Path.GetFullPath(
            Path.Combine(GetProjectRoot(), OutputRelativePath));
    }

    private static string GetProjectRoot()
    {
        return Path.GetFullPath(
            Path.Combine(Application.dataPath, ".."));
    }

    private static bool ShouldShowDialog()
    {
        if (Application.isBatchMode)
        {
            return false;
        }

        string[] commandLineArgs = Environment.GetCommandLineArgs();
        return !commandLineArgs.Any(argument =>
            string.Equals(
                argument,
                "-executeMethod",
                StringComparison.OrdinalIgnoreCase));
    }
}
