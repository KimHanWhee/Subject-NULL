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

    // GEM 충전 상품표(서버 권위). UI 표시용 — 실제 금액은 결제 시 서버가 다시 확정한다.
    public static async Task<List<GemPackage>> GetGemPackagesAsync()
    {
        var res = await CloudCodeService.Instance.CallEndpointAsync<GemPackageList>("GetGemPackages", new Dictionary<string, object>());
        return res != null ? res.packages : null;
    }

    // ── PayPal 결제 ────────────────────────────────────────
    // 클라는 sku만 보낸다. 금액·지급 GEM은 서버 가격표에서만 결정되므로 위조가 불가능하다.

    [System.Serializable]
    public class PaypalOrder { public string orderId; }

    [System.Serializable]
    public class PaypalCapture
    {
        public int granted;          // 이번에 지급된 GEM(재처리면 0)
        public string sku;
        public bool alreadyProcessed; // 이미 지급된 주문(멱등)
    }

    [System.Serializable]
    public class PendingOrder { public string orderId; public string sku; }

    // ── 청약철회 ────────────────────────────────────────────
    // 철회 가능 여부(canWithdraw)와 사유(reason)는 서버가 판단해서 내려준다.
    // 클라이언트는 표시만 하고, 실제 실행도 서버가 다시 검증한다.
    [System.Serializable]
    public class PurchaseItem
    {
        public string orderId;
        public string sku;
        public int gem;
        public string amount;      // "0.99"
        public string currency;    // "USD"
        public string capturedAt;  // ISO8601
        public bool canWithdraw;
        public string reason;      // ok | used | expired | refunded | processing | no-capture | no-baseline
    }

    [System.Serializable]
    public class PurchaseHistory { public List<PurchaseItem> items; public long gem; }

    [System.Serializable]
    public class WithdrawResult { public bool ok; public int gem; public string amount; public string currency; }

    public static async Task<PurchaseHistory> GetPurchaseHistoryAsync()
    {
        return await CloudCodeService.Instance.CallEndpointAsync<PurchaseHistory>("GetPurchaseHistory", new Dictionary<string, object>());
    }

    // 실패 시 예외. 메시지에 서버 사유(gem-already-used 등)가 실려 온다.
    public static async Task<WithdrawResult> WithdrawPurchaseAsync(string orderId)
    {
        var args = new Dictionary<string, object> { { "orderId", orderId } };
        return await CloudCodeService.Instance.CallEndpointAsync<WithdrawResult>("WithdrawPurchase", args);
    }

    // 아직 정리되지 않은 주문 조회 — 새 구매 전에 반드시 확인한다.
    // 결제만 되고 지급이 안 된 주문을 이어받지 않으면 재구매 시 이중 결제가 된다.
    public static async Task<PendingOrder> GetPendingOrderAsync()
    {
        return await CloudCodeService.Instance.CallEndpointAsync<PendingOrder>("GetPendingOrder", new Dictionary<string, object>());
    }

    // 주문 생성 → PayPal 결제창에 넘길 orderId 반환
    public static async Task<PaypalOrder> CreatePaypalOrderAsync(string sku)
    {
        var args = new Dictionary<string, object> { { "sku", sku } };
        return await CloudCodeService.Instance.CallEndpointAsync<PaypalOrder>("CreatePaypalOrder", args);
    }

    // 승인된 주문을 서버가 확정(capture)하고 GEM 지급. 실패 시 예외.
    public static async Task<PaypalCapture> CapturePaypalOrderAsync(string orderId)
    {
        var args = new Dictionary<string, object> { { "orderId", orderId } };
        return await CloudCodeService.Instance.CallEndpointAsync<PaypalCapture>("CapturePaypalOrder", args);
    }
}
