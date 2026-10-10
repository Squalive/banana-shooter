using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;

public static class BuildDedicatedServer
{
    internal static bool Building { get; private set; }

    // Batch mode: Unity -batchmode -quit -projectPath . -executeMethod BuildDedicatedServer.Linux
    [MenuItem("Build/Dedicated Server (Linux)")]
    public static void Linux() => Build(BuildTarget.StandaloneLinux64, "Builds/Server/Linux/BananaShooterServer.x86_64");

    [MenuItem("Build/Dedicated Server (macOS)")]
    public static void Mac() => Build(BuildTarget.StandaloneOSX, "Builds/Server/Mac/BananaShooterServer");

    static void Build(BuildTarget target, string path)
    {
        BuildReport report;
        Building = true;
        try
        {
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                target = target,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                locationPathName = path,
            });
        }
        finally
        {
            Building = false;
        }

        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Dedicated server build failed: {report.summary.result}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        // GameServer.Init reads the app id from the working directory.
        File.Copy("steam_appid.txt", Path.Combine(Path.GetDirectoryName(path), "steam_appid.txt"), true);
        Debug.Log($"Dedicated server built to {path}");
    }
}

// Unity 2021.3 still compiles every shader variant for server builds, which never render.
class StripServerShaders : IPreprocessShaders, IPreprocessComputeShaders
{
    public int callbackOrder => 0;

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (BuildDedicatedServer.Building) data.Clear();
    }

    public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
    {
        if (BuildDedicatedServer.Building) data.Clear();
    }
}
