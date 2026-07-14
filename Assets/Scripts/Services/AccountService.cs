using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;

// 영구 로그인(Account Linking). 익명 계정에 아이디/비밀번호를 연결해 어느 기기에서든
// 복구 가능하게 한다. 진행상황(playerId·GEM·소유)은 그대로 유지되고 자격증명만 추가된다.
// ⚠️ 실제 결제(라이브) 전에 반드시 유도할 것 — 익명 토큰은 브라우저 저장소라 캐시 삭제/타 기기 시 소실.
public static class AccountService
{
    // 현재 계정에 아이디/비번이 연결돼 있는지(= 복구 가능한 영구 계정).
    public static bool IsLinked
    {
        get
        {
            try
            {
                if (!ServicesBootstrap.IsSignedIn) return false;
                PlayerInfo info = AuthenticationService.Instance.PlayerInfo;
                return info != null && !string.IsNullOrEmpty(info.Username);
            }
            catch { return false; }
        }
    }

    public static string Username
    {
        get
        {
            try
            {
                PlayerInfo info = AuthenticationService.Instance.PlayerInfo;
                return info != null && info.Username != null ? info.Username : "";
            }
            catch { return ""; }
        }
    }

    // 서버 호출 전 형식 검증(UGS 규칙). 통과 못하면 친절한 메시지 반환(null=통과).
    // 아이디: 3~20자 · 영문/숫자/. - @ _ · 비번: 8~30자 · 대문자·소문자·숫자·기호 각 1+.
    public static string ValidateUsername(string u)
    {
        if (string.IsNullOrEmpty(u) || u.Length < 3 || u.Length > 20) return "아이디는 3~20자여야 합니다";
        foreach (char c in u)
            if (!(char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '@' || c == '_'))
                return "아이디는 영문/숫자/. - @ _ 만 가능합니다";
        return null;
    }

    public static string ValidatePassword(string p)
    {
        if (string.IsNullOrEmpty(p) || p.Length < 8 || p.Length > 30) return "비밀번호는 8~30자여야 합니다";
        bool up = false, low = false, dig = false, sym = false;
        foreach (char c in p)
        {
            if (char.IsUpper(c)) up = true;
            else if (char.IsLower(c)) low = true;
            else if (char.IsDigit(c)) dig = true;
            else sym = true;
        }
        if (!(up && low && dig && sym)) return "비밀번호는 대문자·소문자·숫자·기호를 각각 포함해야 합니다";
        return null;
    }

    // 현재 익명 계정에 아이디/비번 연결(진행상황 유지). 이미 연결됐거나 아이디 중복이면 실패.
    public static async Task<(bool ok, string error)> LinkAsync(string username, string password)
    {
        string v = ValidateUsername(username) ?? ValidatePassword(password);
        if (v != null) return (false, v);
        try
        {
            await AuthenticationService.Instance.AddUsernamePasswordAsync(username, password);
            return (true, null);
        }
        catch (Exception e) { return (false, Friendly(e)); }
    }

    // 기존 계정으로 로그인(현재 세션 로그아웃 후 재로그인).
    // ⚠️ 현재 익명 계정의 진행상황은 이후 접근 불가(연결 안 했다면). 호출 전 사용자 확인 필요.
    public static async Task<(bool ok, string error)> LoginAsync(string username, string password)
    {
        string v = ValidateUsername(username);
        if (v != null) return (false, v);
        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
                AuthenticationService.Instance.SignOut();
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            return (true, null);
        }
        catch (Exception e)
        {
            // 실패 시 세션이 끊겼으면 익명으로 복귀(게임 계속 가능)
            try
            {
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch { }
            return (false, Friendly(e));
        }
    }

    static string Friendly(Exception e)
    {
        string m = e != null ? e.Message : "알 수 없는 오류";
        if (m.Contains("already") && m.Contains("username")) return "이미 아이디가 연결된 계정입니다";
        if (m.Contains("exists") || m.Contains("taken") || m.Contains("USERNAME_ALREADY")) return "이미 사용 중인 아이디입니다";
        if (m.Contains("Invalid") || m.Contains("credential") || m.Contains("WRONG")) return "아이디 또는 비밀번호가 올바르지 않습니다";
        return m;
    }
}
