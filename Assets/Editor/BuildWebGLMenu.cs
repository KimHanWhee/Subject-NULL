using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildWebGLMenu
{
    [MenuItem("Tools/Build WebGL → Builds/WebGL")]
    public static void Build()
    {
        string[] scenes = System.Array.ConvertAll(
            System.Array.FindAll(EditorBuildSettings.scenes, s => s.enabled),
            s => s.path);

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"[BuildWebGL] 성공: {report.summary.totalSize / (1024 * 1024)}MB → Builds/WebGL");
        else
            Debug.LogError($"[BuildWebGL] 실패: {report.summary.result}");
    }
}
