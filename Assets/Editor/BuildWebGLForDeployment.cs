using System.IO;
using UnityEditor;

public static class BuildWebGLForDeployment
{
    public static void Build()
    {
        const string outputPath = "Build/WebGL";
        Directory.CreateDirectory(outputPath);

        var scenes = EditorBuildSettings.scenes;
        BuildPipeline.BuildPlayer(scenes, outputPath, BuildTarget.WebGL, BuildOptions.None);
    }
}
