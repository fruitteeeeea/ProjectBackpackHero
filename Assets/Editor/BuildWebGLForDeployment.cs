using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class BuildWebGLForDeployment
{
    public const string OutputRelativePath = "Build/WebGL";
    public const string DefaultScenePath = "Assets/Samples/PlanetWar Reusable Main Menu/0.1.0/Main Menu Demo/MainMenuDemo.unity";

    private const string ConfigPathPreference = "ProjectBackpackHero.WebGLPublisher.ConfigPath";
    private const string CosCmdPathPreference = "ProjectBackpackHero.WebGLPublisher.CosCmdPath";
    private const string RemoteFolderPreference = "ProjectBackpackHero.WebGLPublisher.RemoteFolder";

    public static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    public static string OutputPath => Path.Combine(ProjectRoot, OutputRelativePath);

    [MenuItem("工具/WebGL/一键构建并上传 COS", priority = 100)]
    public static void BuildAndUploadFromMenu()
    {
        Publish(LoadSettings(), !Application.isBatchMode);
    }

    [MenuItem("工具/WebGL/发布设置...", priority = 101)]
    public static void OpenPublisherWindow()
    {
        WebGLCosPublisherWindow.Open();
    }

    /// <summary>
    /// Command-line entry point used with Unity's -executeMethod option.
    /// </summary>
    public static void Build()
    {
        BuildWebGL();
    }

    public static WebGLPublishSettings LoadSettings()
    {
        return new WebGLPublishSettings
        {
            ConfigPath = EditorPrefs.GetString(ConfigPathPreference, FindDefaultConfigPath()),
            CosCmdPath = EditorPrefs.GetString(CosCmdPathPreference, FindDefaultCosCmdPath()),
            RemoteFolder = EditorPrefs.GetString(RemoteFolderPreference, new DirectoryInfo(ProjectRoot).Name)
        };
    }

    public static void SaveSettings(WebGLPublishSettings settings)
    {
        EditorPrefs.SetString(ConfigPathPreference, settings.ConfigPath ?? string.Empty);
        EditorPrefs.SetString(CosCmdPathPreference, settings.CosCmdPath ?? string.Empty);
        EditorPrefs.SetString(RemoteFolderPreference, settings.RemoteFolder ?? string.Empty);
    }

    public static WebGLPublishResult Publish(WebGLPublishSettings settings, bool showDialog)
    {
        try
        {
            ValidateSettings(settings);
            SaveSettings(settings);

            DisplayProgress("正在构建 WebGL...", 0.05f);
            var buildReport = BuildWebGL();

            DisplayProgress("正在读取 COS 配置...", 0.65f);
            var cosConfig = ReadCosConfig(settings.ConfigPath);
            var remoteFolder = NormalizeRemoteFolder(settings.RemoteFolder);
            var files = Directory.GetFiles(OutputPath, "*", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (files.Length == 0)
                throw new InvalidOperationException($"WebGL 输出目录没有文件：{OutputPath}");

            for (var index = 0; index < files.Length; index++)
            {
                var localPath = files[index];
                var relativePath = GetRelativePath(OutputPath, localPath).Replace('\\', '/');
                var remotePath = $"{remoteFolder}/{relativePath}";
                var progress = 0.65f + 0.34f * (index + 1f) / files.Length;

                DisplayProgress($"正在上传 {index + 1}/{files.Length}：{relativePath}", progress);

                UploadFile(settings.CosCmdPath, settings.ConfigPath, localPath, remotePath);
            }

            var publicUrl = BuildPublicUrl(cosConfig, remoteFolder);
            var result = new WebGLPublishResult(
                publicUrl,
                files.Length,
                buildReport.summary.totalSize,
                buildReport.summary.totalTime);

            if (!Application.isBatchMode)
            {
                EditorGUIUtility.systemCopyBuffer = publicUrl;
                Debug.Log($"WebGL 发布成功：{publicUrl}\n链接已复制到剪贴板。");
            }
            else
            {
                Debug.Log($"WebGL 发布成功：{publicUrl}");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "WebGL 发布成功",
                    $"已上传 {files.Length} 个文件。\n\n{publicUrl}\n\n链接已复制到剪贴板。",
                    "确定");
            }

            return result;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (showDialog)
                EditorUtility.DisplayDialog("WebGL 发布失败", exception.Message, "确定");
            throw;
        }
        finally
        {
            if (!Application.isBatchMode)
                EditorUtility.ClearProgressBar();
        }
    }

    public static BuildReport BuildWebGL()
    {
        if (!File.Exists(Path.Combine(ProjectRoot, DefaultScenePath)))
            throw new BuildFailedException($"未找到默认启动场景：{DefaultScenePath}");

        var scenes = new[] { DefaultScenePath }
            .Concat(EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.Equals(scene.path, DefaultScenePath, StringComparison.OrdinalIgnoreCase))
                .Select(scene => scene.path))
            .ToArray();

        PrepareCleanOutputDirectory();

        Debug.Log($"开始构建 WebGL。默认场景：{scenes[0]}");
        var report = BuildPipeline.BuildPlayer(scenes, OutputPath, BuildTarget.WebGL, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException(
                $"WebGL 构建失败：{report.summary.result}，错误 {report.summary.totalErrors}，警告 {report.summary.totalWarnings}。");
        }

        var indexPath = Path.Combine(OutputPath, "index.html");
        if (!File.Exists(indexPath))
            throw new BuildFailedException($"构建报告成功，但未找到入口文件：{indexPath}");

        Debug.Log($"WebGL 构建成功：{OutputPath}");
        return report;
    }

    public static string PreviewPublicUrl(WebGLPublishSettings settings)
    {
        try
        {
            var config = ReadCosConfig(settings.ConfigPath);
            return BuildPublicUrl(config, NormalizeRemoteFolder(settings.RemoteFolder));
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void PrepareCleanOutputDirectory()
    {
        var projectRootWithSeparator = ProjectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var fullOutputPath = Path.GetFullPath(OutputPath);

        if (!fullOutputPath.StartsWith(projectRootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"拒绝清理项目目录外的输出路径：{fullOutputPath}");

        if (Directory.Exists(fullOutputPath))
            Directory.Delete(fullOutputPath, true);

        Directory.CreateDirectory(fullOutputPath);
    }

    private static void DisplayProgress(string message, float progress)
    {
        if (!Application.isBatchMode)
            EditorUtility.DisplayProgressBar("WebGL 一键发布", message, progress);
    }

    private static void UploadFile(string cosCmdPath, string configPath, string localPath, string remotePath)
    {
        var headers = BuildHeaders(localPath);
        var arguments = string.Join(" ", new[]
        {
            "-c", QuoteArgument(configPath),
            "upload", "-f", "-y",
            "-H", QuoteArgument(headers),
            QuoteArgument(localPath),
            QuoteArgument(remotePath)
        });

        var startInfo = new ProcessStartInfo
        {
            FileName = cosCmdPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = ProjectRoot
        };

        var output = new StringBuilder();
        using (var process = new Process { StartInfo = startInfo })
        {
            process.OutputDataReceived += (_, eventArgs) => AppendProcessLine(output, eventArgs.Data);
            process.ErrorDataReceived += (_, eventArgs) => AppendProcessLine(output, eventArgs.Data);

            if (!process.Start())
                throw new InvalidOperationException("无法启动 coscmd。");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"上传失败：{remotePath}\n退出码：{process.ExitCode}\n{output}".Trim());
            }
        }

        if (output.Length > 0)
            Debug.Log(output.ToString().Trim());
    }

    private static void AppendProcessLine(StringBuilder output, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        lock (output)
            output.AppendLine(line);
    }

    private static string BuildHeaders(string path)
    {
        var fileName = Path.GetFileName(path);
        var contentEncoding = string.Empty;
        if (fileName.EndsWith(".br", StringComparison.OrdinalIgnoreCase))
        {
            contentEncoding = "br";
            fileName = fileName.Substring(0, fileName.Length - 3);
        }

        var contentType = GetContentType(fileName);
        var entries = new List<string>
        {
            "x-cos-acl: public-read",
            $"Content-Type: {contentType}",
            "Cache-Control: no-cache"
        };

        if (!string.IsNullOrEmpty(contentEncoding))
            entries.Add($"Content-Encoding: {contentEncoding}");

        return "{" + string.Join(", ", entries) + "}";
    }

    private static string GetContentType(string fileName)
    {
        switch (Path.GetExtension(fileName).ToLowerInvariant())
        {
            case ".html": return "text/html";
            case ".js": return "application/javascript";
            case ".wasm": return "application/wasm";
            case ".json": return "application/json";
            case ".css": return "text/css";
            case ".png": return "image/png";
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".ico": return "image/x-icon";
            case ".svg": return "image/svg+xml";
            default: return "application/octet-stream";
        }
    }

    private static void ValidateSettings(WebGLPublishSettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (string.IsNullOrWhiteSpace(settings.ConfigPath) || !File.Exists(settings.ConfigPath))
            throw new FileNotFoundException("未找到 .cos.conf，请在发布设置中选择配置文件。", settings.ConfigPath);
        if (string.IsNullOrWhiteSpace(settings.CosCmdPath) || !File.Exists(settings.CosCmdPath))
            throw new FileNotFoundException("未找到 coscmd.exe，请在发布设置中选择可执行文件。", settings.CosCmdPath);

        NormalizeRemoteFolder(settings.RemoteFolder);
        ReadCosConfig(settings.ConfigPath);
    }

    private static string NormalizeRemoteFolder(string remoteFolder)
    {
        var normalized = (remoteFolder ?? string.Empty).Trim().Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("COS 项目目录不能为空。");

        var segments = normalized.Split('/');
        if (segments.Any(segment => string.IsNullOrWhiteSpace(segment) || segment == "." || segment == ".."))
            throw new InvalidOperationException($"COS 项目目录不合法：{remoteFolder}");

        return normalized;
    }

    private static CosConnectionSettings ReadCosConfig(string configPath)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var inCommonSection = false;

        foreach (var rawLine in File.ReadAllLines(configPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                inCommonSection = string.Equals(line.Substring(1, line.Length - 2).Trim(), "common", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inCommonSection)
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            values[line.Substring(0, separator).Trim()] = line.Substring(separator + 1).Trim();
        }

        if (!values.TryGetValue("bucket", out var bucket) || string.IsNullOrWhiteSpace(bucket))
            throw new InvalidOperationException(".cos.conf 缺少 [common] bucket。 ");
        if (!values.TryGetValue("region", out var region) || string.IsNullOrWhiteSpace(region))
            throw new InvalidOperationException(".cos.conf 缺少 [common] region。 ");

        values.TryGetValue("schema", out var schema);
        return new CosConnectionSettings(bucket, region, string.IsNullOrWhiteSpace(schema) ? "https" : schema);
    }

    private static string BuildPublicUrl(CosConnectionSettings config, string remoteFolder)
    {
        var encodedFolder = string.Join("/", remoteFolder.Split('/').Select(Uri.EscapeDataString));
        return $"{config.Schema}://{config.Bucket}.cos.{config.Region}.myqcloud.com/{encodedFolder}/index.html";
    }

    private static string FindDefaultConfigPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", ".cos.conf");
    }

    private static string FindDefaultCosCmdPath()
    {
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var pythonRoot = Path.Combine(roaming, "Python");

        if (Directory.Exists(pythonRoot))
        {
            var match = Directory.GetDirectories(pythonRoot, "Python*", SearchOption.TopDirectoryOnly)
                .Select(directory => Path.Combine(directory, "Scripts", "coscmd.exe"))
                .Where(File.Exists)
                .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(match))
                return match;
        }

        return "coscmd.exe";
    }

    private static string GetRelativePath(string rootPath, string fullPath)
    {
        var rootUri = new Uri(AppendDirectorySeparator(Path.GetFullPath(rootPath)));
        var fileUri = new Uri(Path.GetFullPath(fullPath));
        return Uri.UnescapeDataString(rootUri.MakeRelativeUri(fileUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
    }

    private static string AppendDirectorySeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static string QuoteArgument(string argument)
    {
        return "\"" + (argument ?? string.Empty).Replace("\"", "\\\"") + "\"";
    }

    private sealed class CosConnectionSettings
    {
        public CosConnectionSettings(string bucket, string region, string schema)
        {
            Bucket = bucket;
            Region = region;
            Schema = schema;
        }

        public string Bucket { get; }
        public string Region { get; }
        public string Schema { get; }
    }
}

[Serializable]
public sealed class WebGLPublishSettings
{
    public string ConfigPath;
    public string CosCmdPath;
    public string RemoteFolder;
}

public sealed class WebGLPublishResult
{
    public WebGLPublishResult(string publicUrl, int fileCount, ulong buildSize, TimeSpan buildTime)
    {
        PublicUrl = publicUrl;
        FileCount = fileCount;
        BuildSize = buildSize;
        BuildTime = buildTime;
    }

    public string PublicUrl { get; }
    public int FileCount { get; }
    public ulong BuildSize { get; }
    public TimeSpan BuildTime { get; }
}

public sealed class WebGLCosPublisherWindow : EditorWindow
{
    private WebGLPublishSettings settings;
    private string lastPublicUrl;

    public static void Open()
    {
        var window = GetWindow<WebGLCosPublisherWindow>(true, "WebGL COS 一键发布", true);
        window.minSize = new Vector2(680f, 260f);
        window.Show();
    }

    private void OnEnable()
    {
        settings = BuildWebGLForDeployment.LoadSettings();
        lastPublicUrl = BuildWebGLForDeployment.PreviewPublicUrl(settings);
    }

    private void OnGUI()
    {
        if (settings == null)
            settings = BuildWebGLForDeployment.LoadSettings();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("WebGL 构建并上传到腾讯云 COS", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "点击一次即可清理旧输出、构建 Build Settings 中启用的场景、上传全部 WebGL 文件，并配置公开读取、Brotli 与 MIME 响应头。",
            MessageType.Info);

        DrawFileField("COS 配置文件", ref settings.ConfigPath, "conf");
        DrawFileField("coscmd.exe", ref settings.CosCmdPath, "exe");

        EditorGUI.BeginChangeCheck();
        settings.RemoteFolder = EditorGUILayout.TextField("COS 项目目录", settings.RemoteFolder);
        if (EditorGUI.EndChangeCheck())
            lastPublicUrl = BuildWebGLForDeployment.PreviewPublicUrl(settings);

        EditorGUILayout.LabelField("本地输出", BuildWebGLForDeployment.OutputPath);
        EditorGUILayout.LabelField("启动场景", BuildWebGLForDeployment.DefaultScenePath);
        EditorGUILayout.LabelField("访问链接", string.IsNullOrEmpty(lastPublicUrl) ? "配置完整后自动生成" : lastPublicUrl);

        EditorGUILayout.Space(12f);
        using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("一键构建并上传", GUILayout.Height(42f)))
            {
                var result = BuildWebGLForDeployment.Publish(settings, true);
                lastPublicUrl = result.PublicUrl;
                Repaint();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("保存设置"))
            {
                BuildWebGLForDeployment.SaveSettings(settings);
                lastPublicUrl = BuildWebGLForDeployment.PreviewPublicUrl(settings);
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(lastPublicUrl)))
            {
                if (GUILayout.Button("复制链接"))
                    EditorGUIUtility.systemCopyBuffer = lastPublicUrl;
                if (GUILayout.Button("打开链接"))
                    Application.OpenURL(lastPublicUrl);
            }
        }
    }

    private void DrawFileField(string label, ref string value, string extension)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.TextField(label, value);
            if (EditorGUI.EndChangeCheck())
                lastPublicUrl = BuildWebGLForDeployment.PreviewPublicUrl(settings);

            if (GUILayout.Button("选择...", GUILayout.Width(76f)))
            {
                var initialDirectory = string.IsNullOrWhiteSpace(value)
                    ? BuildWebGLForDeployment.ProjectRoot
                    : Path.GetDirectoryName(value);
                var selected = EditorUtility.OpenFilePanel(label, initialDirectory, extension);
                if (!string.IsNullOrEmpty(selected))
                {
                    value = selected;
                    lastPublicUrl = BuildWebGLForDeployment.PreviewPublicUrl(settings);
                }
            }
        }
    }
}
