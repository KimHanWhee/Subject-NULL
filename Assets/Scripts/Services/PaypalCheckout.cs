using System;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// PayPal 결제 진입점 (WebGL 전용). GoogleAuth와 동일한 jslib 브리지 패턴.
//
// 전체 흐름 — 지급 판단은 전적으로 서버가 한다:
//   ① CreatePaypalOrder(sku)   서버가 가격표로 금액 확정 → orderId
//   ② PP_Pay(orderId)          PayPal 결제창에서 사용자가 승인
//   ③ CapturePaypalOrder(id)   서버가 결제 확정 + 금액 재검증 + 멱등 처리 후 GEM 지급
//
// 클라가 보내는 건 sku와 orderId뿐이라 금액·지급량을 조작할 수 없다.
public class PaypalCheckout : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void PP_Setup(string goName);
    [DllImport("__Internal")] static extern void PP_Pay(string orderId);
    [DllImport("__Internal")] static extern void PP_Hide();
#endif

    static PaypalCheckout instance;
    TaskCompletionSource<string> approval; // 승인된 orderId 또는 실패

    public static bool IsSupported
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }

    static PaypalCheckout Ensure()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("PaypalCheckout");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<PaypalCheckout>();
#if UNITY_WEBGL && !UNITY_EDITOR
            PP_Setup(go.name);
#endif
        }
        return instance;
    }

    public class Result
    {
        public bool ok;
        public int granted;      // 지급된 GEM
        public bool alreadyPaid; // 이미 처리된 주문(멱등)
        public string error;     // "cancelled" | "editor-unsupported" | 기타
    }

    // 상품 구매 — 성공 시 서버가 GEM을 지급한 뒤 결과를 반환.
    public static async Task<Result> PurchaseAsync(string sku)
    {
        if (!IsSupported) return new Result { ok = false, error = "editor-unsupported" };

        PaypalCheckout c = Ensure();

        // ① 서버에서 주문 생성(금액은 서버 가격표로 확정)
        GachaService.PaypalOrder order;
        try
        {
            order = await GachaService.CreatePaypalOrderAsync(sku);
        }
        catch (Exception e)
        {
            Debug.LogError("[PayPal] createOrder 실패: " + e);
            return new Result { ok = false, error = "create-failed" };
        }
        if (order == null || string.IsNullOrEmpty(order.orderId))
            return new Result { ok = false, error = "create-failed" };

        // ② 결제창 — 사용자가 승인할 때까지 대기
        c.approval = new TaskCompletionSource<string>();
#if UNITY_WEBGL && !UNITY_EDITOR
        PP_Pay(order.orderId);
#endif
        string approved = await c.approval.Task;
        if (string.IsNullOrEmpty(approved) || approved.StartsWith("!"))
            return new Result { ok = false, error = approved != null ? approved.TrimStart('!') : "unknown" };

        // ③ 서버 확정 + 지급.
        //    한 번 실패하면 곧바로 한 번 더 시도한다. 서버는 주문ID 기준으로 멱등이라
        //    이미 지급됐다면 alreadyProcessed로 돌아오고, 아직이면 정상 지급된다.
        //    — 지급은 됐는데 응답만 유실된 경우(타임아웃 등)를 실패로 표시하지 않기 위함.
        //      실제로 "지급 실패라더니 젬은 들어와 있는" 현상이 여기서 나왔다.
        //    ⚠️ WebGL에서는 Task.Delay가 동작하지 않으므로 대기 없이 즉시 재시도한다.
        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                GachaService.PaypalCapture cap = await GachaService.CapturePaypalOrderAsync(approved);
                return new Result
                {
                    ok = true,
                    granted = cap != null ? cap.granted : 0,
                    alreadyPaid = cap != null && cap.alreadyProcessed
                };
            }
            catch (Exception e)
            {
                Debug.LogError("[PayPal] capture 실패(" + (attempt + 1) + "/2): " + e);
            }
        }
        return new Result { ok = false, error = "capture-failed" };
    }

    public static void CloseUi()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        PP_Hide();
#endif
    }

    // ↓ jslib SendMessage 수신부 — 이름/시그니처 변경 금지
    void OnPaypalApproved(string orderId)
    {
        TaskCompletionSource<string> t = approval;
        approval = null;
        if (t != null) t.TrySetResult(orderId);
    }

    void OnPaypalError(string err)
    {
        TaskCompletionSource<string> t = approval;
        approval = null;
        if (t != null) t.TrySetResult("!" + err); // 앞의 '!'가 실패 표식
    }
}
