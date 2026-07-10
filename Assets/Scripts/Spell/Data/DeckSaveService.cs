using System.Collections.Generic;
using UnityEngine;

// Design Ref: §3.2 — 덱 저장/로드. marbleName 문자열만 JSON으로 저장하고
// 로드 시 SpellMarbleRegistry로 실제 SO 해석(에셋 GUID 의존 회피).
// Plan SC: FR-04 — 저장 덱(P2). SpellCaster가 저장 덱 존재 시 기본 DeckData 대신 사용.
// 저장 키는 UGS 계정별(playerId) 분리 — 로그인 전엔 마지막 playerId 캐시로 폴백.
// (기기 로컬 한정. 기기 간 동기화는 가챠/Economy 작업 때 Cloud Save로 이관 예정)
public static class DeckSaveService
{
    public const int MinSize = 15; // CLAUDE.md — 덱 크기 15~25
    public const int MaxSize = 25;

    const string BaseKey = "SpellDeck.v1";              // 계정 미상 시 폴백 키(구버전 저장 위치)
    const string LastIdKey = "SpellDeck.lastPlayerId";  // 오프라인 폴백용 마지막 로그인 계정

    [System.Serializable]
    class Blob
    {
        public List<string> names = new List<string>();
    }

    public static bool HasSave() => PlayerPrefs.HasKey(ResolveKey());

    public static void Save(List<SpellMarble> deck)
    {
        Blob blob = new Blob();
        if (deck != null)
            foreach (SpellMarble m in deck)
                if (m != null && !string.IsNullOrEmpty(m.marbleName))
                    blob.names.Add(m.marbleName);
        PlayerPrefs.SetString(ResolveKey(), JsonUtility.ToJson(blob));
        PlayerPrefs.Save();
    }

    // 미존재 id는 스킵(마블 에셋이 삭제/개명된 경우 폴백). Design §6 에러 처리.
    public static List<SpellMarble> Load(SpellMarbleRegistry registry)
    {
        List<SpellMarble> result = new List<SpellMarble>();
        if (registry == null) return result;
        string key = ResolveKey();
        if (!PlayerPrefs.HasKey(key)) return result;

        Blob blob = null;
        try { blob = JsonUtility.FromJson<Blob>(PlayerPrefs.GetString(key)); }
        catch { return result; } // 손상된 저장은 빈 덱 취급 → 기본 덱 폴백

        if (blob == null || blob.names == null) return result;
        foreach (string name in blob.names)
        {
            SpellMarble m = registry.Get(name);
            if (m != null) result.Add(m);
        }
        return result;
    }

    public static void Clear() => PlayerPrefs.DeleteKey(ResolveKey());

    // 실제 사용할 키 결정 + 구버전(계정 없는 키) 저장분 1회 이관.
    // 이관 후 구키는 삭제 — 나중에 다른 계정이 로그인해도 남의 덱이 보이지 않게.
    static string ResolveKey()
    {
        string id = CurrentPlayerId();
        string key = string.IsNullOrEmpty(id) ? BaseKey : BaseKey + "." + id;
        if (key != BaseKey && !PlayerPrefs.HasKey(key) && PlayerPrefs.HasKey(BaseKey))
        {
            PlayerPrefs.SetString(key, PlayerPrefs.GetString(BaseKey));
            PlayerPrefs.DeleteKey(BaseKey);
            PlayerPrefs.Save();
        }
        return key;
    }

    // 로그인 중이면 실제 playerId(+캐시 갱신), 아니면 마지막 로그인 계정으로 폴백.
    // UGS 미초기화 상태에서 AuthenticationService 접근이 던질 수 있어 방어.
    static string CurrentPlayerId()
    {
        try
        {
            if (ServicesBootstrap.IsSignedIn)
            {
                string id = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
                if (!string.IsNullOrEmpty(id))
                {
                    if (PlayerPrefs.GetString(LastIdKey, "") != id)
                    {
                        PlayerPrefs.SetString(LastIdKey, id);
                        PlayerPrefs.Save();
                    }
                    return id;
                }
            }
        }
        catch { }
        return PlayerPrefs.GetString(LastIdKey, "");
    }
}
