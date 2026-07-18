using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;

// 글로벌 랭킹(UGS Leaderboards) — 대시보드에 ID "HIGH_SCORE" 리더보드 필요
// (정렬 High to Low, 갱신 Best 설정 → 낮은 점수 제출은 서버가 무시).
// 모든 호출 방어적: 리더보드 미생성/오프라인이어도 게임 흐름을 막지 않는다.
public static class RankingService
{
    public const string LeaderboardId = "HIGH_SCORE";

    public class Entry
    {
        public int rank;        // 1-based
        public string name;
        public int score;
        public bool isMe;
    }

    // 사망 시 호출(fire-and-forget) — 서버가 Best만 유지하므로 매판 제출해도 안전.
    public static async void Submit(int score)
    {
        if (!ServicesBootstrap.IsSignedIn || score <= 0) return;
        try
        {
            await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, score);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Ranking] 점수 제출 실패: " + e.Message);
        }
    }

    // 상위 n명. 실패 시 null(UI는 "불러오지 못했습니다" 처리).
    public static async Task<List<Entry>> GetTopAsync(int n)
    {
        if (!ServicesBootstrap.IsSignedIn) return null;
        try
        {
            var res = await LeaderboardsService.Instance.GetScoresAsync(
                LeaderboardId, new GetScoresOptions { Limit = n });
            string myId = AuthenticationService.Instance.PlayerId;
            var list = new List<Entry>();
            foreach (LeaderboardEntry e in res.Results)
                list.Add(new Entry
                {
                    rank = e.Rank + 1, // SDK는 0-based
                    name = string.IsNullOrEmpty(e.PlayerName) ? Shorten(e.PlayerId) : e.PlayerName,
                    score = (int)e.Score,
                    isMe = e.PlayerId == myId
                });
            return list;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Ranking] 조회 실패: " + e.Message);
            return null;
        }
    }

    // 내 순위(기록 없으면 null).
    public static async Task<Entry> GetMyEntryAsync()
    {
        if (!ServicesBootstrap.IsSignedIn) return null;
        try
        {
            LeaderboardEntry e = await LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardId);
            return new Entry
            {
                rank = e.Rank + 1,
                name = string.IsNullOrEmpty(e.PlayerName) ? Shorten(e.PlayerId) : e.PlayerName,
                score = (int)e.Score,
                isMe = true
            };
        }
        catch { return null; } // 아직 기록 없음(404) 포함
    }

    // 로그인 직후 1회 — 닉네임이 없으면 UGS가 자동 생성(리더보드 표시용).
    public static async void EnsurePlayerName()
    {
        if (!ServicesBootstrap.IsSignedIn) return;
        try { await AuthenticationService.Instance.GetPlayerNameAsync(); }
        catch (Exception e) { Debug.LogWarning("[Ranking] 닉네임 확보 실패: " + e.Message); }
    }

    // 현재 닉네임(예: "BraveFox#1234"). 아직 미확보면 "".
    public static string CurrentName
    {
        get
        {
            try { return ServicesBootstrap.IsSignedIn ? (AuthenticationService.Instance.PlayerName ?? "") : ""; }
            catch { return ""; }
        }
    }

    // 닉네임 변경. 반환: null=성공, 그 외=사용자 표시용 에러.
    // UGS 규칙: 공백 불가, 최대 50자. 뒤의 #숫자 태그는 서버가 자동 부여.
    public static async Task<string> SetNameAsync(string name)
    {
        if (!ServicesBootstrap.IsSignedIn) return "로그인 상태가 아닙니다";
        name = name == null ? "" : name.Trim();
        if (name.Length < 2 || name.Length > 16) return "닉네임은 2~16자로 입력하세요";
        if (name.Contains(" ")) return "닉네임에 공백은 쓸 수 없습니다";
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(name);
            return null;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Ranking] 닉네임 변경 실패: " + e.Message);
            return "닉네임 변경에 실패했습니다";
        }
    }

    static string Shorten(string playerId)
    {
        return string.IsNullOrEmpty(playerId) ? "?" : "실험체-" + playerId.Substring(0, Mathf.Min(6, playerId.Length));
    }
}
