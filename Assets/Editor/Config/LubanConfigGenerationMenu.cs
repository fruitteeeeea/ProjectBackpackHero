using System;
using System.Diagnostics;
using System.IO;
using BackpackHero.Config;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.Editor
{
    /// <summary>Unity menu entry points for the checked-in Luban generation script.</summary>
    public static class LubanConfigGenerationMenu
    {
        private const string MenuRoot = "Tools/Backpack Hero/";
        private const string GeneratorPath = "Tools/Luban/generate.ps1";

        [MenuItem(MenuRoot + "Generate Luban Config")]
        public static void Generate()
        {
            if (!TryGenerate(out string output))
            {
                ReportFailure(output);
                return;
            }

            RefreshGeneratedAssets();
            UnityEngine.Debug.Log("[Luban] Configuration generation succeeded.\n" + output);
        }

        [MenuItem(MenuRoot + "Generate and Validate Luban Config")]
        public static void GenerateAndValidate()
        {
            if (!TryGenerate(out string output))
            {
                ReportFailure(output);
                return;
            }

            RefreshGeneratedAssets();
            UnityEngine.Debug.Log("[Luban] Configuration generation succeeded.\n" + output);
            GameConfigValidation.ValidateFromMenu();
        }

        private static bool TryGenerate(out string output)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string generator = Path.Combine(projectRoot, GeneratorPath);
            if (!File.Exists(generator))
            {
                output = "Missing generation script: " + generator;
                return false;
            }

            EditorUtility.DisplayProgressBar(
                "Generate Luban Config",
                "Generating C# tables and JSON payloads...",
                0.5f);

            try
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + generator + "\"",
                    WorkingDirectory = projectRoot,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                using Process process = Process.Start(startInfo);
                string standardOutput = process.StandardOutput.ReadToEnd();
                string standardError = process.StandardError.ReadToEnd();
                process.WaitForExit();

                output = string.IsNullOrWhiteSpace(standardError)
                    ? standardOutput
                    : standardOutput + "\n" + standardError;
                return process.ExitCode == 0;
            }
            catch (Exception exception)
            {
                output = exception.ToString();
                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void RefreshGeneratedAssets()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            GameConfigService.ResetForTests();
        }

        private static void ReportFailure(string output)
        {
            const string message = "Luban configuration generation failed. See Console for details.";
            UnityEngine.Debug.LogError("[Luban] " + message + "\n" + output);
            EditorUtility.DisplayDialog("Generate Luban Config", message, "OK");
        }
    }
}
