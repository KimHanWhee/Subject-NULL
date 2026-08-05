using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.CloudSave;

// 덱 구성·최고 점수 서버 동기화(UGS Cloud Save player data).
// 정책: 로컬 PlayerPrefs가 즉시 캐시, 서버가 원본(기기 간 이동/캐시 삭제 대비).
//  - 로그인 직후 PullAsync 1회: 서버 → 로컬 캐시(덱은 서버 우선, 점수는 큰 쪽 승리)
//  - 덱 저장/신기록 때만 Push: 초소형 JSON 1회 업로드(호출 빈도 낮음 — UGS 부하 미미)
// 실패해도 로컬 캐시는 유지되고 다음 저장/로그인 때 자연 재시도된다.
public static class CloudSyncService
{
    const string DeckKey = "deck";        // List<string> marbleNames
    const string PassiveKey = "passives"; // List<string> marbleNames (패시브 칸)
    const string ScoreKey = "highScore";  // int

    // 로그인 직후 호출(ServicesBootstrap.SignedIn 이벤트).
    public static async Task PullAsync()
    {
        if (!ServicesBootstrap.IsSignedIn) return;
        try
        {
            var keys = new HashSet<string> { DeckKey, PassiveKey, ScoreKey };
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (data.TryGetValue(DeckKey, out var deckItem))
            {
                List<string> names = deckItem.Value.GetAs<List<string>>();
                // 패시브는 별도 키 — 구버전 계정엔 없으므로 없으면 빈 리스트로 둔다.
                List<string> passives = null;
                if (data.TryGetValue(PassiveKey, out var passiveItem))
                    passives = passiveItem.Value.GetAs<List<string>>();
                if (names != null) DeckSaveService.OverwriteLocal(names, passives);
            }
            if (data.TryGetValue(ScoreKey, out var scoreItem))
            {
                int cloud = scoreItem.Value.GetAs<int>();
                int local = HighScoreService.GetBest();
                if (cloud > local) HighScoreService.OverwriteLocal(cloud);
                else if (local > cloud) PushScore(local); // 로컬이 앞서면(과거 오프라인 기록) 서버로 올림
            }
            Debug.Log("[CloudSync] pull 완료");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[CloudSync] pull 실패(로컬 캐시로 진행): " + e.Message);
        }
    }

    public static void PushDeck(List<string> names, List<string> passives = null)
    {
        if (names == null) return;
        Push(DeckKey, new List<string>(names)); // 호출자 리스트 변형 방지용 복사
        if (passives != null) Push(PassiveKey, new List<string>(passives));
    }

    public static void PushScore(int score) => Push(ScoreKey, score);

    // 공용 fire-and-forget 업로드 — UI를 막지 않고, 실패는 로그만(로컬 우선 정책).
    static async void Push(string key, object value)
    {
        if (!ServicesBootstrap.IsSignedIn) return;
        try
        {
            await CloudSaveService.Instance.Data.Player.SaveAsync(
                new Dictionary<string, object> { { key, value } });
        }
        catch (Exception e)
        {
            Debug.LogWarning("[CloudSync] push 실패(" + key + "): " + e.Message);
        }
    }
}
