using System;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// Google Identity Services 브리지의 C# 진입점 (WebGL 전용, 에디터/기타 플랫폼은 미지원 콜백).
// 사용: GoogleAuth.RequestIdToken(jwt => ..., err => ...) → 받은 jwt를 UGS OIDC 로그인/연결에 사용.
public class GoogleAuth : MonoBehaviour
{
    public const string ClientId = "79126493050-sdv57mlj7kd41nkid1t2a9vdno6a1f82.apps.googleusercontent.com";
    public const string ProviderName = "oidc-google"; // UGS 대시보드 Identity Provider 이름과 일치해야 함

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void GSI_Setup(string clientId, string goName);
    [DllImport("__Internal")] static extern void GSI_Show();
    [DllImport("__Internal")] static extern void GSI_Hide();
#endif

    static GoogleAuth instance;
    Action<string> onToken;
    Action<string> onError;

    public static bool IsSupported
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }

    static GoogleAuth Ensure()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("GoogleAuth");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GoogleAuth>();
#if UNITY_WEBGL && !UNITY_EDITOR
            GSI_Setup(ClientId, go.name);
#endif
        }
        return instance;
    }

    // GIS 초기화만 먼저 시작한다(오버레이는 띄우지 않음).
    // 로그인 선택 화면이 뜨는 순간 불러두면, 사용자가 버튼을 누를 때쯤엔 준비가 끝나 있다.
    // 느린 회선에서 첫 시도가 "준비 중"으로 튕기는 걸 막는다.
    public static void Warmup()
    {
        if (!IsSupported) return;
        Ensure();
    }

    // 구글 로그인 오버레이 표시 → 성공 시 id_token(JWT) 전달. 실패/취소 시 error("cancelled" 등).
    public static void RequestIdToken(Action<string> token, Action<string> error)
    {
        GoogleAuth a = Ensure();
        a.onToken = token;
        a.onError = error;
#if UNITY_WEBGL && !UNITY_EDITOR
        GSI_Show();
#else
        a.FailEditor();
#endif
    }

    void FailEditor()
    {
        Action<string> cb = onError;
        onToken = null; onError = null;
        cb?.Invoke("editor-unsupported");
    }

    public static void CloseUi()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        GSI_Hide();
#endif
    }

    // ↓ jslib SendMessage 수신부 — 이름/시그니처 변경 금지
    void OnGoogleIdToken(string jwt)
    {
        Action<string> cb = onToken;
        onToken = null; onError = null;
        cb?.Invoke(jwt);
    }

    void OnGoogleError(string err)
    {
        Action<string> cb = onError;
        onToken = null; onError = null;
        cb?.Invoke(err);
    }
}
