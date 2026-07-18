using UnityEditor;
using UnityEngine;

public static class ApplyWebGLViewportSettings
{
    [InitializeOnLoadMethod]
    static void AutoApply()
    {
        if (PlayerSettings.WebGL.template == "PROJECT:SubjectNull"
            && PlayerSettings.defaultWebScreenWidth == 1920) return;
        EditorApplication.delayCall += Apply;
    }

    [MenuItem("Tools/Apply WebGL Viewport Settings")]
    public static void Apply()
    {
        PlayerSettings.WebGL.template = "PROJECT:SubjectNull";
        PlayerSettings.defaultWebScreenWidth = 1920;
        PlayerSettings.defaultWebScreenHeight = 1080;
        EditorApplication.ExecuteMenuItem("File/Save Project");
        Debug.Log($"[WebGLViewport] template={PlayerSettings.WebGL.template}, size={PlayerSettings.defaultWebScreenWidth}x{PlayerSettings.defaultWebScreenHeight}");
    }
}
