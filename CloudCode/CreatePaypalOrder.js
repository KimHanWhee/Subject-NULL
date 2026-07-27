// CreatePaypalOrder — PayPal 주문 생성 (UGS Cloud Code / JavaScript).
//
// 흐름(서버 권위 결제): 클라가 sku만 보냄 → 서버가 GetGemPackages와 동일한 가격표에서
//   금액을 확정(가격 위조 방지) → PayPal Orders v2 로 주문 생성 → orderId 반환.
//   실제 GEM 지급은 CapturePaypalOrder에서 결제가 확정(capture)된 뒤에만 이뤄진다.
//
// 시크릿(대시보드 Secret Manager에 등록, 코드에 하드코딩 금지):
//   PAYPAL_CLIENT_ID     — PayPal 앱 Client ID
//   PAYPAL_SECRET        — PayPal 앱 Secret
//   PAYPAL_ENV(선택)     — "sandbox"(기본) | "live"
//
// ⚠️ Sandbox → Live 전환 시 겪은 함정(재발 방지용 기록):
//   1) 셋 다 바꿔야 한다. 하나만 Live면 OAuth 401이거나, 주문은 만들어지는데
//      브라우저가 그 주문을 못 찾아 PayPal이 INVALID_RESOURCE_ID를 낸다.
//   2) 시크릿은 조직/프로젝트/환경 계층이고 **환경 레벨이 최우선**이다.
//      환경 레벨에 옛 값이 남아 있으면 다른 레벨을 고쳐도 계속 옛 값을 읽는다.
//   3) Cloud Code는 시크릿을 최대 5분 캐시한다. 바꾼 직후엔 반영이 안 보일 수 있다.
//   4) 시크릿 등록 시 접근 서비스 목록에 Cloud Code가 포함돼 있어야 한다.
//
// ⚠️ 런타임 제약: UGS Cloud Code에는 전역 fetch도 Node의 Buffer도 없다.
//    외부 HTTP는 반드시 axios-1.6 을 쓰고, Basic 인증은 axios의 auth 옵션에 맡긴다
//    (직접 base64 인코딩하려 들면 Buffer가 없어서 터진다).
//
// ⚠️ 가격표는 GetGemPackages.js 와 반드시 동일하게 유지할 것(둘 다 서버라 안전하지만 값은 동기화 필요).

const axios = require("axios-1.6");
const { DataApi } = require("@unity-services/cloud-save-1.4");

// 미해결 주문 기록 키. 주문을 만들면 여기 남기고, 지급이 끝나면 비운다.
// 이게 있어야 "결제는 됐는데 지급이 안 된" 주문을 다음 접속 때 이어받을 수 있다.
// (없으면 사용자가 재구매 → 새 주문 → 이중 결제가 된다)
const PENDING_KEY = "paypal_pending";

const PACKAGES = {
  gem_1000:  { gem: 1000,  priceUsd: "0.99" },
  gem_5500:  { gem: 5500,  priceUsd: "4.99" },
  gem_12000: { gem: 12000, priceUsd: "9.99" },
  gem_25000: { gem: 25000, priceUsd: "19.99" }
};

// ⚠️ 대소문자·앞뒤 공백을 정규화한다.
// 예전엔 env === "live" 로만 비교해서 "Live"나 "live "를 넣으면 조용히 sandbox로 떨어졌다.
// 그러면 서버는 Sandbox에 주문을 만들고 브라우저는 Live에서 그 주문을 찾다가
// "현재 오류가 발생한 것 같습니다"로 실패한다 — 원인을 찾기 매우 어려운 유형이다.
function normalizeEnv(v) {
  return String(v || "").trim().toLowerCase() === "live" ? "live" : "sandbox";
}

function apiBase(env) {
  return normalizeEnv(env) === "live"
    ? "https://api-m.paypal.com"
    : "https://api-m.sandbox.paypal.com";
}

// axios 오류를 로그에 남길 수 있는 문자열로 압축(응답 본문에 원인이 들어있다).
function describe(err) {
  if (err && err.response) {
    return "HTTP " + err.response.status + " " + JSON.stringify(err.response.data);
  }
  return err && err.message ? err.message : String(err);
}

// PayPal OAuth2 액세스 토큰 획득(client_credentials)
async function getAccessToken(base, clientId, secret) {
  const res = await axios.post(
    base + "/v1/oauth2/token",
    "grant_type=client_credentials",
    {
      auth: { username: clientId, password: secret }, // Basic 인증 — 수동 base64 불필요
      headers: { "Content-Type": "application/x-www-form-urlencoded" }
    }
  );
  return res.data.access_token;
}

module.exports = async ({ params, context, logger, secretManager }) => {
  const { projectId, playerId } = context;
  const sku = params.sku;
  const pkg = PACKAGES[sku];
  if (!pkg) throw new Error("unknown-sku: " + sku);

  let clientId, secret;
  try {
    clientId = (await secretManager.getSecret("PAYPAL_CLIENT_ID")).value;
    secret = (await secretManager.getSecret("PAYPAL_SECRET")).value;
  } catch (e) {
    logger.error("secret lookup failed", { "error.message": describe(e) });
    throw new Error("paypal-secrets-missing");
  }

  let env = "sandbox";
  try { env = (await secretManager.getSecret("PAYPAL_ENV")).value || "sandbox"; } catch (e) { /* 미설정=sandbox */ }
  const base = apiBase(env);

  let token;
  try {
    token = await getAccessToken(base, clientId, secret);
  } catch (e) {
    // 여기서 401이면 Client ID/Secret이 틀렸거나 Sandbox/Live 짝이 안 맞는 것.
    logger.error("paypal oauth failed", { env: env, "error.message": describe(e) });
    throw new Error("paypal-oauth-failed");
  }

  // 주문 생성 — custom_id에 지급 정보를 서버끼리 실어 나른다(capture에서 재검증).
  try {
    const res = await axios.post(
      base + "/v2/checkout/orders",
      {
        intent: "CAPTURE",
        purchase_units: [{
          custom_id: playerId + "|" + sku,
          amount: { currency_code: "USD", value: pkg.priceUsd },
          description: sku
        }]
      },
      {
        headers: {
          "Authorization": "Bearer " + token,
          "Content-Type": "application/json"
        }
      }
    );
    const orderId = res.data.id;

    // 미해결 주문으로 기록. 지급이 끝나면 CapturePaypalOrder가 비운다.
    // 기록 실패가 결제를 막을 이유는 없으므로 삼켜두되, 로그는 남긴다.
    try {
      const cloudSave = new DataApi(context);
      await cloudSave.setItem(projectId, playerId, {
        key: PENDING_KEY,
        value: { orderId: orderId, sku: sku, createdAt: new Date().toISOString() }
      });
    } catch (e) {
      logger.error("pending order 기록 실패(이중 결제 방어 약화)", { orderId: orderId, "error.message": describe(e) });
    }

    // ⚠️ 응답 필드를 늘리지 말 것.
    // Unity Cloud Code SDK는 클라이언트 클래스(GachaService.PaypalOrder)에 없는 멤버가
    // 응답에 있으면 DeserializationException을 던진다. 서버만 고쳐서 필드를 추가하면
    // 그 순간 결제가 통째로 깨진다. 필드를 늘리려면 클라이언트 클래스도 함께 고치고 재빌드해야 한다.
    return { orderId: orderId };
  } catch (e) {
    logger.error("CreatePaypalOrder failed", { "error.message": describe(e) });
    throw new Error("paypal-create-failed");
  }
};

module.exports.params = {
  sku: { type: "STRING", required: true }
};
