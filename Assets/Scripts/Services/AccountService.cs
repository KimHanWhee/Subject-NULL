using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;

// 계정 상태 조회(게스트/Google 모델).
// "영구 계정" = Google(OIDC) 연결됨 — 어느 기기에서든 복구 가능.
// ⚠️ 실제 결제(라이브) 전에 반드시 연결 유도할 것 — 게스트 세션 토큰은 브라우저 저장소라 캐시 삭제/타 기기 시 소실.
public static class AccountService
{
    static bool googleLinked;

    // 현재 계정이 영구(Google 연결) 계정인지 — RefreshAsync로 갱신된 캐시 + 동기 PlayerInfo 폴백.
    public static bool IsLinked
    {
        get
        {
            if (googleLinked) return true;
            try
            {
                if (!ServicesBootstrap.IsSignedIn) return false;
                return HasGoogleIdentity(AuthenticationService.Instance.PlayerInfo);
            }
            catch { return false; }
        }
    }

    // 서버에서 PlayerInfo를 받아 연결 여부 캐시 갱신 (로그인 직후·계정 화면 진입 시 호출)
    public static async Task RefreshAsync()
    {
        googleLinked = false;
        if (!ServicesBootstrap.IsSignedIn) return;
        try
        {
            PlayerInfo info = await AuthenticationService.Instance.GetPlayerInfoAsync();
            googleLinked = HasGoogleIdentity(info);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("[Account] PlayerInfo 조회 실패: " + e.Message);
        }
    }

    // Link 직후 등 확정 시점에 즉시 반영용
    public static void MarkLinked() { googleLinked = true; }

    static bool HasGoogleIdentity(PlayerInfo info)
    {
        if (info == null || info.Identities == null) return false;
        foreach (Identity id in info.Identities)
            if (id != null && id.TypeId != null && id.TypeId.Contains(GoogleAuth.ProviderName))
                return true;
        return false;
    }
}
