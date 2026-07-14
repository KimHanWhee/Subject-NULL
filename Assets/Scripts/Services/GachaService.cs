using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode;

// 서버 권위 가챠/프로필 통신. RNG·재화·소유는 전적으로 서버(Cloud Code).
//   GachaPull   → 뽑기 1회(GEM 차감+RNG+소유 지급+조각)
//   GetProfile  → gem/shards/owned 조회
//   ExchangeMarble → 조각으로 미보유 Gold+ 확정 교환
public static class GachaService
{
    [System.Serializable]
    public class Profile
    {
        public long gem;
        public int shards;
        public List<string> owned = new List<string>();
    }

    [System.Serializable]
    public class PullResult
    {
        public string marbleName; // 뽑힌 마블 식별(SpellMarble.marbleName)
        public string grade;      // "Gold" | "Diamond" | "Legend"
        public bool isNew;        // 신규 획득 여부
        public int shardsGained;  // 이번 뽑기 조각
        public long gem;          // 뽑기 후 GEM
        public int shards;        // 뽑기 후 조각
        public string error;      // "INSUFFICIENT_GEM" 등(성공 시 null)
    }

    [System.Serializable]
    public class ExchangeResult
    {
        public bool ok;
        public long gem;
        public int shards;
        public string error;
    }

    [System.Serializable]
    public class GemPackage
    {
        public string sku;      // 서버 상품 식별자
        public int gem;         // 지급 GEM
        public string priceUsd; // 결제 금액(USD)
        public string label;    // 표시명
        public string bonus;    // 보너스 뱃지(선택)
    }

    [System.Serializable]
    public class GemPackageList { public System.Collections.Generic.List<GemPackage> packages; }

    public static async Task<Profile> GetProfileAsync()
    {
        var args = new Dictionary<string, object>();
        return await CloudCodeService.Instance.CallEndpointAsync<Profile>("GetProfile", args);
    }

    public static async Task<PullResult> PullAsync()
    {
        var args = new Dictionary<string, object>();
        return await CloudCodeService.Instance.CallEndpointAsync<PullResult>("GachaPull", args);
    }

    public static async Task<ExchangeResult> ExchangeAsync(string marbleName)
    {
        var args = new Dictionary<string, object> { { "marbleName", marbleName } };
        return await CloudCodeService.Instance.CallEndpointAsync<ExchangeResult>("ExchangeMarble", args);
    }

    // GEM 충전 상품표(서버 권위). UI 표시용 — 결제는 별도(PayPal, 단계 3b).
    public static async Task<List<GemPackage>> GetGemPackagesAsync()
    {
        var res = await CloudCodeService.Instance.CallEndpointAsync<GemPackageList>("GetGemPackages", new Dictionary<string, object>());
        return res != null ? res.packages : null;
    }
}
