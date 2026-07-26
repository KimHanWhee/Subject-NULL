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
// ⚠️ 런타임 제약: UGS Cloud Code에는 전역 fetch도 Node의 Buffer도 없다.
//    외부 HTTP는 반드시 axios-1.6 을 쓰고, Basic 인증은 axios의 auth 옵션에 맡긴다
//    (직접 base64 인코딩하려 들면 Buffer가 없어서 터진다).
//
// ⚠️ 가격표는 GetGemPackages.js 와 반드시 동일하게 유지할 것(둘 다 서버라 안전하지만 값은 동기화 필요).

const axios = require("axios-1.6");

const PACKAGES = {
  gem_1000:  { gem: 1000,  priceUsd: "0.99" },
  gem_5500:  { gem: 5500,  priceUsd: "4.99" },
  gem_12000: { gem: 12000, priceUsd: "9.99" },
  gem_25000: { gem: 25000, priceUsd: "19.99" }
};

function apiBase(env) {
  return env === "live"
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
          custom_id: context.playerId + "|" + sku,
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
    return { orderId: res.data.id }; // 클라는 이 id로 PayPal JS SDK 결제창을 연다
  } catch (e) {
    logger.error("CreatePaypalOrder failed", { "error.message": describe(e) });
    throw new Error("paypal-create-failed");
  }
};

module.exports.params = {
  sku: { type: "STRING", required: true }
};
