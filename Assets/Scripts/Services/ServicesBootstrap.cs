using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

// UGS(백엔드) 진입점: 게임 시작 시 1회 초기화.
// 이전 세션 토큰이 있으면 자동 재개(게스트/구글 공통), 없으면 로그인 화면(LoginGate)의
// [게스트로 시작]/[Google로 시작] 선택을 기다린다.
// 여러 씬에 있어도 중복 없이 유지(싱글턴 + DontDestroyOnLoad).
public class ServicesBootstrap : MonoBehaviour
{
    public static ServicesBootstrap Instance { get; private set; }

    public static bool IsInitialized =>
        UnityServices.State == ServicesInitializationState.Initialized;

    // 다른 스크립트에서 로그인 완료 여부 확인용
    public static bool IsSignedIn =>
        IsInitialized && AuthenticationService.Instance.IsSignedIn;

    // 이전 방문 세션 토큰 보유 여부(= 자동 재개 진행 예정) — 로그인 선택 화면 표시 판단용
    public static bool HasCachedSession
    {
        get
        {
            try { return IsInitialized && AuthenticationService.Instance.SessionTokenExists; }
            catch { return false; }
        }
    }

    // 로그인 완료까지 대기(최대 timeout초) — 로그인 대기 폴링 공용 진입점.
    // ⚠️ Task.Delay는 WebGL(스레드 없음)에서 완료되지 않을 수 있어
    //    프레임 기반 Awaitable.NextFrameAsync로 폴링한다(에디터/스탠드얼론/WebGL 공통 안전).
    public static async Task WaitSignedInAsync(float timeout = 10f)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!IsSignedIn && Time.realtimeSinceStartup < deadline)
            await Awaitable.NextFrameAsync();
    }

    public static async Task WaitInitializedAsync(float timeout = 10f)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!IsInitialized && Time.realtimeSinceStartup < deadline)
            await Awaitable.NextFrameAsync();
    }

    async void Awake()
    {
        // Destroy(gameObject) 금지 — 같은 GO에 다른 컴포넌트(AccountUI 등)가 함께 있으면 통째로 죽는다.
        if (Instance != null) { Destroy(this); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        await InitAsync();
    }

    async Task InitAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("[UGS] 로그인 성공 · playerId=" + AuthenticationService.Instance.PlayerId);
                _ = CloudSyncService.PullAsync(); // 덱/최고점수 서버 → 로컬 캐시(모든 로그인 경로 공통)
                RankingService.EnsurePlayerName(); // 리더보드 표시용 닉네임 자동 생성
            };
            AuthenticationService.Instance.SignInFailed += (err) =>
                Debug.LogError("[UGS] 로그인 실패: " + err);

            // 이전 세션이 있으면 자동 재개(게스트/구글 공통 — 세션 토큰이 같은 플레이어를 복원)
            if (!AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.SessionTokenExists)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            if (IsSignedIn) await AccountService.RefreshAsync(); // 결제 게이트용 연결 여부 캐시
        }
        catch (Exception e)
        {
            Debug.LogError("[UGS] 초기화/로그인 예외: " + e);
        }
    }

    // ── 로그인 방식 선택(LoginGate/AccountUI에서 호출) ──
    // 반환: null=성공, 그 외=사용자에게 보여줄 실패 메시지

    public static async Task<string> SignInGuestAsync()
    {
        if (!IsInitialized) return "서버 연결 준비 중입니다";
        if (IsSignedIn) return null;
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError("[UGS] 게스트 로그인 실패: " + e);
            return "게스트 로그인에 실패했습니다";
        }
    }

    public static async Task<string> SignInGoogleAsync()
    {
        if (!IsInitialized) return "서버 연결 준비 중입니다";
        string idToken;
        try { idToken = await RequestGoogleIdTokenAsync(); }
        catch (Exception e) { return GoogleUiError(e.Message); }
        try
        {
            await AuthenticationService.Instance.SignInWithOpenIdConnectAsync(GoogleAuth.ProviderName, idToken);
            AccountService.MarkLinked();
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError("[UGS] 구글 로그인 실패: " + e);
            return "Google 로그인에 실패했습니다";
        }
    }

    // 게스트(익명) 상태에서 구글 계정 연결 — 진행상황(playerId) 유지한 채 영구화
    public static async Task<string> LinkGoogleAsync()
    {
        if (!IsSignedIn) return "로그인 상태가 아닙니다";
        string idToken;
        try { idToken = await RequestGoogleIdTokenAsync(); }
        catch (Exception e) { return GoogleUiError(e.Message); }
        try
        {
            await AuthenticationService.Instance.LinkWithOpenIdConnectAsync(GoogleAuth.ProviderName, idToken);
            AccountService.MarkLinked();
            return null;
        }
        catch (AuthenticationException e) when (e.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            // 이 Gmail이 이미 다른 플레이어(예: 다른 도메인/기기에서 만든 계정)에 연결된 경우가 대부분
            return "이 Google 계정은 이미 다른 플레이어와 연결되어 있습니다.\n아래 [다른 계정으로 로그인]을 눌러 그 계정으로 로그인하세요.";
        }
        catch (AuthenticationException e) when (e.ErrorCode == AuthenticationErrorCodes.AccountLinkLimitExceeded)
        {
            return "이 Google 계정은 이미 다른 플레이어와 연결되어 있습니다.\n아래 [다른 계정으로 로그인]을 눌러 그 계정으로 로그인하세요.";
        }
        catch (Exception e)
        {
            Debug.LogError("[UGS] 구글 연결 실패: " + e);
            return "Google 계정 연결에 실패했습니다";
        }
    }

    static Task<string> RequestGoogleIdTokenAsync()
    {
        TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
        GoogleAuth.RequestIdToken(
            t => tcs.TrySetResult(t),
            e => tcs.TrySetException(new Exception(e)));
        return tcs.Task;
    }

    // 반환 규약: null=성공 아님(성공 여부는 IsSignedIn으로 판정), ""=사용자 취소(메시지 표시 생략)
    static string GoogleUiError(string code)
    {
        if (code == "cancelled") return ""; // 사용자가 닫음 — 에러 표시 불필요
        if (code == "editor-unsupported") return "Google 로그인은 웹 빌드에서만 지원됩니다";
        if (code == "not-ready") return "Google 로그인 준비 중입니다 — 잠시 후 다시 시도하세요";
        return "Google 로그인 창을 열지 못했습니다 (" + code + ")";
    }
}
