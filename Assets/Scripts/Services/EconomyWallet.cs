using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.CloudCode;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;

// 서버 권위 GEM 지갑(UGS Economy). 클라이언트는 잔액을 "읽기"만 하고,
// 증감(지급/차감)은 오직 Cloud Code(서비스 계정)만 수행한다.
//
// 전제(대시보드/CLI 설정, 코드로 못 함):
//   1) UGS Economy에 Currency 생성 — ID = "GEM"
//   2) Access Control 정책으로 Player의 Economy increment/decrement 를 Deny
//      (= 클라가 스스로 잔액을 못 올림 → 실제 결제의 의미가 생김)
//
// GEM 소비(가챠)도 서버로 옮기기 전까지는 이 컴포넌트를 기존 WalletService/가챠에
// 연결하지 않는다(중간 파손 방지). 우선 파이프라인만 독립 검증 → 이후 원자적 교체.
public static class EconomyWallet
{
    public const string GemCurrencyId = "GEM";

    // 마지막으로 서버에서 읽은 잔액(캐시). UI 표시용. 진실은 항상 서버.
    public static long CachedGem { get; private set; }
    public static event Action OnGemChanged;

    public static bool Ready => ServicesBootstrap.IsSignedIn;

    // 서버에서 GEM 잔액을 읽어 캐시 갱신.
    public static async Task<long> RefreshAsync()
    {
        if (!Ready) return CachedGem;
        try
        {
            GetBalancesResult res = await EconomyService.Instance.PlayerBalances.GetBalancesAsync();
            long gem = 0;
            foreach (PlayerBalance b in res.Balances)
                if (b.CurrencyId == GemCurrencyId) { gem = b.Balance; break; }
            CachedGem = gem;
            OnGemChanged?.Invoke();
            return gem;
        }
        catch (Exception e)
        {
            Debug.LogError("[EconomyWallet] 잔액 조회 실패: " + e);
            return CachedGem;
        }
    }

    // GEM 지급 경로는 클라이언트에 두지 않는다.
    // 예전에는 DevGrantGem(Cloud Code)을 직접 호출하는 개발용 지급 함수가 여기 있었는데,
    // 그 스크립트는 호출자 제한이 없어 로그인만 하면 누구나 GEM을 무제한 발행할 수 있었다.
    // → 스크립트를 서버에서 삭제하고 이 함수도 함께 제거했다.
    //   정상 지급 경로는 결제를 서버에서 검증하는 CapturePaypalOrder 하나뿐이다.
}
