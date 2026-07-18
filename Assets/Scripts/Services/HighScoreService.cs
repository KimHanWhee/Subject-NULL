using UnityEngine;

// 계정별 최고기록 — 로컬 저장(UGS playerId를 키로 사용해 계정마다 분리). 로그인 안 됐으면 guest 키.
// (기기 로컬 저장이라 같은 계정으로 같은 기기에서 유지됨. 크로스 디바이스 동기화는 추후 Cloud Save로 확장 가능.)
public static class HighScoreService
{
    static string Key()
    {
        string id = "guest";
        try
        {
            if (ServicesBootstrap.IsSignedIn)
            {
                string pid = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
                if (!string.IsNullOrEmpty(pid)) id = pid;
            }
        }
        catch { /* 인증 미초기화 시 guest */ }
        return "best_score_" + id;
    }

    public static int GetBest()
    {
        return PlayerPrefs.GetInt(Key(), 0);
    }

    // 갱신 시 true(신기록).
    public static bool Submit(int score)
    {
        if (score <= GetBest()) return false;
        PlayerPrefs.SetInt(Key(), score);
        PlayerPrefs.Save();
        CloudSyncService.PushScore(score); // 서버 동기화(비동기)
        return true;
    }

    // 서버(Cloud Save) 기록으로 로컬 캐시 교체 — 로그인 직후 CloudSyncService.PullAsync에서 호출.
    public static void OverwriteLocal(int score)
    {
        PlayerPrefs.SetInt(Key(), score);
        PlayerPrefs.Save();
    }
}
