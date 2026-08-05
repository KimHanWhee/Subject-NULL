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
        // 패시브 오브. 구버전 저장본에는 이 필드가 없는데, JsonUtility는 없는 필드를
        // 초기값(빈 리스트)으로 두므로 그대로 읽힌다 — 마이그레이션 불필요.
        public List<string> passives = new List<string>();
    }

    public static bool HasSave() => PlayerPrefs.HasKey(ResolveKey());

    public static void Save(List<SpellMarble> deck, List<SpellMarble> passives = null)
    {
        Blob blob = new Blob();
        if (deck != null)
            foreach (SpellMarble m in deck)
                if (m != null && !string.IsNullOrEmpty(m.marbleName))
                    blob.names.Add(m.marbleName);
        if (passives != null)
            foreach (SpellMarble m in passives)
                if (m != null && !string.IsNullOrEmpty(m.marbleName))
                    blob.passives.Add(m.marbleName);
        PlayerPrefs.SetString(ResolveKey(), JsonUtility.ToJson(blob));
        PlayerPrefs.Save();
        CloudSyncService.PushDeck(blob.names, blob.passives); // 서버 동기화(비동기 — 실패 시 다음 저장/로그인 때 재시도)
    }

    // 서버(Cloud Save) 덱으로 로컬 캐시 교체 — 로그인 직후 CloudSyncService.PullAsync에서 호출.
    public static void OverwriteLocal(List<string> names, List<string> passives = null)
    {
        Blob blob = new Blob();
        if (names != null)
            foreach (string n in names)
                if (!string.IsNullOrEmpty(n)) blob.names.Add(n);
        if (passives != null)
            foreach (string n in passives)
                if (!string.IsNullOrEmpty(n)) blob.passives.Add(n);
        PlayerPrefs.SetString(ResolveKey(), JsonUtility.ToJson(blob));
        PlayerPrefs.Save();
    }

    // 미존재 id는 스킵(마블 에셋이 삭제/개명된 경우 폴백). Design §6 에러 처리.
    public static List<SpellMarble> Load(SpellMarbleRegistry registry)
    {
        return Resolve(registry, false);
    }

    // 패시브 칸에 편성된 오브. 저장본이 없거나 구버전이면 빈 리스트.
    public static List<SpellMarble> LoadPassives(SpellMarbleRegistry registry)
    {
        return Resolve(registry, true);
    }

    static List<SpellMarble> Resolve(SpellMarbleRegistry registry, bool passive)
    {
        List<SpellMarble> result = new List<SpellMarble>();
        if (registry == null) return result;
        string key = ResolveKey();
        if (!PlayerPrefs.HasKey(key)) return result;

        Blob blob = null;
        try { blob = JsonUtility.FromJson<Blob>(PlayerPrefs.GetString(key)); }
        catch { return result; } // 손상된 저장은 빈 덱 취급 → 기본 덱 폴백
        if (blob == null) return result;

        List<string> src = passive ? blob.passives : blob.names;
        if (src == null) return result;
        foreach (string name in src)
        {
            SpellMarble m = registry.Get(name);
            if (m == null) continue;
            // 편성 칸이 갈리므로 저장본이 반대편이면 버린다.
            // (오브를 패시브로 바꾸거나 되돌리면 옛 저장본이 어긋날 수 있다)
            if (m.isPassive != passive) continue;
            if (passive && result.Contains(m)) continue; // 패시브는 중복 불가
            result.Add(m);
        }
        return result;
    }

    public static void Clear() => PlayerPrefs.DeleteKey(ResolveKey());

    // 저장 덱 복구 — 일반 오브가 패시브로 바뀌었을 때 기존 플레이어를 구제한다.
    //
    // 왜 필요한가: 덱에 넣어두었던 오브가 패시브로 바뀌면 로드에서 조용히 걸러진다.
    // 그러면 (a) 덱이 15개 미만으로 줄어 덱이 빨리 돌고(조커·고난이 잦아짐),
    // (b) 그 오브를 덱 빌더에 다시 들어가기 전까지 아예 못 쓴다.
    //
    // 하는 일: 패시브가 된 것들을 패시브 칸으로 옮기고, 모자란 일반 덱을 보유분으로 채운다.
    // 멱등 — 옮길 것도 채울 것도 없으면 아무 일도 하지 않으므로 매 로드마다 호출해도 된다.
    public static void Repair(SpellMarbleRegistry registry)
    {
        if (registry == null) return;
        string key = ResolveKey();
        if (!PlayerPrefs.HasKey(key)) return;

        Blob blob;
        try { blob = JsonUtility.FromJson<Blob>(PlayerPrefs.GetString(key)); }
        catch { return; }
        if (blob == null || blob.names == null) return;
        if (blob.passives == null) blob.passives = new List<string>();

        int passiveSlots = DeckRules.Instance.PassiveSlots;
        bool changed = false;

        // ① 덱에 남아 있는 패시브 오브를 패시브 칸으로 이동
        for (int i = blob.names.Count - 1; i >= 0; i--)
        {
            SpellMarble m = registry.Get(blob.names[i]);
            if (m == null || !m.isPassive) continue;

            blob.names.RemoveAt(i);
            changed = true;
            if (blob.passives.Count < passiveSlots && !blob.passives.Contains(m.marbleName))
                blob.passives.Add(m.marbleName); // 칸이 남고 중복이 아니면 살려준다
        }

        // ② 덱이 최소 크기에 못 미치면 보유한 일반 오브로 채운다.
        //    안 채우면 짧은 덱으로 게임이 시작돼 덱 순환이 빨라진다.
        if (blob.names.Count < MinSize)
        {
            List<SpellMarble> pool = new List<SpellMarble>();
            foreach (SpellMarble m in registry.allMarbles)
                if (m != null && !m.isPassive && OwnedMarblesService.IsOwned(m)) pool.Add(m);

            int guard = 0;
            while (blob.names.Count < MinSize && pool.Count > 0 && guard++ < 500)
            {
                SpellMarble pick = null;
                foreach (SpellMarble m in pool)
                {
                    int have = 0;
                    foreach (string n in blob.names) if (n == m.marbleName) have++;
                    if (have < DeckRules.Instance.MaxCopies(m.grade)) { pick = m; break; }
                }
                if (pick == null) break; // 한도까지 다 찼으면 더 넣을 수 없다
                blob.names.Add(pick.marbleName);
                changed = true;
            }
        }

        if (!changed) return;
        PlayerPrefs.SetString(key, JsonUtility.ToJson(blob));
        PlayerPrefs.Save();
        CloudSyncService.PushDeck(blob.names, blob.passives);
        Debug.Log("[Deck] 저장 덱 복구: 일반 " + blob.names.Count + " / 패시브 " + blob.passives.Count);
    }

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
