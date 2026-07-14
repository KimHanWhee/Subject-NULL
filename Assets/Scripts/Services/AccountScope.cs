using UnityEngine;

// 계정(playerId) 스코프 해석 — 로컬 저장 키를 계정별로 분리하기 위한 공용 헬퍼.
// 로그인 중이면 실제 playerId, 아니면 덱 저장이 갱신하는 마지막 로그인 캐시로 폴백.
public static class AccountScope
{
    const string LastIdKey = "SpellDeck.lastPlayerId";

    public static string PlayerId
    {
        get
        {
            try
            {
                if (ServicesBootstrap.IsSignedIn)
                {
                    string id = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
                    if (!string.IsNullOrEmpty(id)) return id;
                }
            }
            catch { }
            return PlayerPrefs.GetString(LastIdKey, "");
        }
    }

    // baseKey를 계정별 키로. 미로그인/미캐시면 baseKey 그대로.
    public static string Key(string baseKey)
    {
        string id = PlayerId;
        return string.IsNullOrEmpty(id) ? baseKey : baseKey + "." + id;
    }
}
