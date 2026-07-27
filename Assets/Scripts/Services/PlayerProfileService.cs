using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// 서버 권위 프로필(gem/shards/owned)을 읽어와 로컬 미러(WalletService·OwnedMarblesService)에
// 반영한다. 이렇게 하면 기존 동기 소비자(SpellCaster·DeckBuilderUI 등)는 코드 수정 없이
// "마지막으로 서버에서 읽은 값"을 그대로 사용한다. 진실은 항상 서버.
//
// 사용 패턴:
//   씬 로드 시 → await RefreshAsync()  (미러 최신화)
//   서버 뽑기/교환 후 → ApplyPull(result) 또는 ApplyBalances(gem, shards)로 즉시 미러 갱신
public static class PlayerProfileService
{
    public static bool Loaded { get; private set; }
    public static long Gem { get; private set; }
    public static int Shards { get; private set; }
    public static event Action OnChanged;

    // 서버에서 프로필을 읽어 로컬 미러에 반영.
    //
    // 로그인 직후 첫 호출이 간헐적으로 실패한다(토큰이 Cloud Code까지 자리잡기 전).
    // 실패하면 화면이 "젬 0 · 미보유"로 보여서 사용자가 재화를 잃은 줄 알게 되므로
    // 몇 번 더 시도한다. ⚠️ WebGL에서는 Task.Delay가 동작하지 않아 대기 없이 재시도하는데,
    // 호출 자체가 왕복이라 자연스레 간격이 생긴다.
    public static async Task<bool> RefreshAsync()
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (await TryRefreshOnceAsync()) return true;
            if (!ServicesBootstrap.IsSignedIn) return false; // 로그인 자체가 안 된 상태면 재시도 무의미
        }
        return false;
    }

    static async Task<bool> TryRefreshOnceAsync()
    {
        if (!ServicesBootstrap.IsSignedIn) return false;
        try
        {
            GachaService.Profile p = await GachaService.GetProfileAsync();
            if (p == null) return false;
            Gem = p.gem;
            Shards = p.shards;
            OwnedMarblesService.SetFromServer(p.owned);
            WalletService.SetFromServer(p.gem, p.shards);
            Loaded = true;
            OnChanged?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[PlayerProfile] 조회 실패: " + e);
            return false;
        }
    }

    // 서버 뽑기 결과를 미러에 즉시 반영(추가 왕복 없이).
    public static void ApplyPull(GachaService.PullResult r)
    {
        if (r == null || !string.IsNullOrEmpty(r.error)) return;
        Gem = r.gem;
        Shards = r.shards;
        if (r.isNew && !string.IsNullOrEmpty(r.marbleName))
            OwnedMarblesService.MirrorGrant(r.marbleName);
        WalletService.SetFromServer(r.gem, r.shards);
        OnChanged?.Invoke();
    }

    // 서버 교환 등으로 재화만 바뀐 경우.
    public static void ApplyBalances(long gem, int shards)
    {
        Gem = gem;
        Shards = shards;
        WalletService.SetFromServer(gem, shards);
        OnChanged?.Invoke();
    }
}
