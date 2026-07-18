using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// 플랫폼 공용 클립보드 복사 — WebGL은 jslib(브라우저 API), 그 외는 systemCopyBuffer.
public static class ClipboardUtil
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void JS_CopyToClipboard(string text);
#endif

    public static void Copy(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
#if UNITY_WEBGL && !UNITY_EDITOR
        JS_CopyToClipboard(text);
#else
        GUIUtility.systemCopyBuffer = text;
#endif
    }
}
