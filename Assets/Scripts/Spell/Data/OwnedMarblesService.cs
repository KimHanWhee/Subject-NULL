using System.Collections.Generic;
using UnityEngine;

// 마블 소유 관리(로컬 MVP) — 계정(playerId)별 PlayerPrefs 저장. 덱 저장(DeckSaveService)과 동일 패턴.
// 규칙: Normal 등급은 항상 무료 소유(스타터). Gold 이상은 가챠로 획득해야 소유로 인정.
// 저장에는 Gold+ 획득분 marbleName만 담긴다(Normal은 규칙상 소유라 저장 불필요).
// TODO(Stage 2): UGS Cloud Save의 ownedMarbles로 이관(서버 권위). [[ugs-backend-plan]]
public static class OwnedMarblesService
{
    const string BaseKey = "OwnedMarbles.v1";
    const string LastIdKey = "SpellDeck.lastPlayerId"; // 덱 저장과 같은 계정 캐시 공유

    [System.Serializable]
    class Blob { public List<string> names = new List<string>(); }

    static HashSet<string> cache;
    static string cacheKey;

    // Normal은 항상 소유. 그 외엔 획득 집합에 있어야 소유.
    public static bool IsOwned(SpellMarble m)
        => m != null && (m.grade == Grade.Normal || Owned().Contains(m.marbleName));

    public static bool IsOwnedName(string marbleName)
        => !string.IsNullOrEmpty(marbleName) && Owned().Contains(marbleName);

    public static void Grant(string marbleName)
    {
        if (string.IsNullOrEmpty(marbleName)) return;
        HashSet<string> s = Owned();
        if (s.Add(marbleName)) Persist(s);
    }

    public static void Revoke(string marbleName)
    {
        HashSet<string> s = Owned();
        if (s.Remove(marbleName)) Persist(s);
    }

    // 테스트/디버그: 레지스트리의 Gold+ 전부 해금
    public static void GrantAll(SpellMarbleRegistry registry)
    {
        if (registry == null) return;
        HashSet<string> s = Owned();
        bool changed = false;
        foreach (SpellMarble m in registry.allMarbles)
            if (m != null && m.grade != Grade.Normal && !string.IsNullOrEmpty(m.marbleName))
                changed |= s.Add(m.marbleName);
        if (changed) Persist(s);
    }

    public static IReadOnlyCollection<string> OwnedGoldPlus() => Owned();

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key());
        cache = null; cacheKey = null;
    }

    // ── 내부 ──────────────────────────────────────────────
    static HashSet<string> Owned()
    {
        string key = Key();
        if (cache != null && cacheKey == key) return cache;
        cache = new HashSet<string>();
        cacheKey = key;
        if (PlayerPrefs.HasKey(key))
        {
            try
            {
                Blob b = JsonUtility.FromJson<Blob>(PlayerPrefs.GetString(key));
                if (b != null && b.names != null)
                    foreach (string n in b.names) if (!string.IsNullOrEmpty(n)) cache.Add(n);
            }
            catch { } // 손상 저장은 빈 소유로 취급
        }
        return cache;
    }

    static void Persist(HashSet<string> s)
    {
        Blob b = new Blob();
        b.names.AddRange(s);
        PlayerPrefs.SetString(Key(), JsonUtility.ToJson(b));
        PlayerPrefs.Save();
        cache = s; cacheKey = Key();
    }

    static string Key()
    {
        string id = CurrentPlayerId();
        return string.IsNullOrEmpty(id) ? BaseKey : BaseKey + "." + id;
    }

    // 로그인 중이면 실제 playerId, 아니면 마지막 로그인 계정(덱 저장이 갱신하는 캐시)로 폴백.
    static string CurrentPlayerId()
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
