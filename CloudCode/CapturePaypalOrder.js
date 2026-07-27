// CapturePaypalOrder — PayPal 결제 확정 + GEM 지급 (UGS Cloud Code / JavaScript).
//
// 흐름: 클라가 승인한 orderId 를 서버가 직접 capture(결제 확정) →
//   ① 결제 상태 COMPLETED 확인  ② 금액/통화가 SKU 가격과 일치하는지 검증
//   ③ 멱등: 이미 처리한 orderId면 재지급하지 않음(중복 클릭·재요청 방어)
//   ④ 서비스 계정으로 GEM 증가  → 최신 잔액 반환.
//
// 보안 원칙: 지급 판단은 전적으로 서버. 클라가 보내는 건 orderId 뿐이며, 금액·지급량은
//   PayPal capture 응답과 서버 가격표에서만 결정한다. Economy 증액은 서비스 계정(CurrenciesApi(context)).
//
// ⚠️ 런타임 제약: UGS Cloud Code에는 전역 fetch도 Node의 Buffer도 없다.
//    외부 HTTP는 axios-1.6, Basic 인증은 axios의 auth 옵션으로 처리한다.
//
// 시크릿: PAYPAL_CLIENT_ID / PAYPAL_SECRET / PAYPAL_ENV(선택) — CreatePaypalOrder와 동일.

const axios = require("axios-1.6");
const { CurrenciesApi } = require("@unity-services/economy-2.4");
const { DataApi } = require("@unity-services/cloud-save-1.4");

const PACKAGES = {
  gem_1000:  { gem: 1000,  priceUsd: "0.99" },
  gem_5500:  { gem: 5500,  priceUsd: "4.99" },
  gem_12000: { gem: 12000, priceUsd: "9.99" },
  gem_25000: { gem: 25000, priceUsd: "19.99" }
};

// 청약철회 화면이 읽는 결제 내역(배열). ledger 키는 개별 조회만 되고 목록화가 안 되므로 따로 둔다.
const HISTORY_KEY = "purchase_history";
const MAX_HISTORY = 50;

// 대소문자·공백 정규화 — CreatePaypalOrder와 동일 규약(불일치하면 주문을 못 찾는다).
function normalizeEnv(v) {
  return String(v || "").trim().toLowerCase() === "live" ? "live" : "sandbox";
}

function apiBase(env) {
  return normalizeEnv(env) === "live"
    ? "https://api-m.paypal.com"
    : "https://api-m.sandbox.paypal.com";
}

function describe(err) {
  if (err && err.response) {
    return "HTTP " + err.response.status + " " + JSON.stringify(err.response.data);
  }
  return err && err.message ? err.message : String(err);
}

async function getAccessToken(base, clientId, secret) {
  const res = await axios.post(
    base + "/v1/oauth2/token",
    "grant_type=client_credentials",
    {
      auth: { username: clientId, password: secret },
      headers: { "Content-Type": "application/x-www-form-urlencoded" }
    }
  );
  return res.data.access_token;
}

module.exports = async ({ params, context, logger, secretManager }) => {
  const { projectId, playerId } = context;
  const orderId = params.orderId;
  if (!orderId) throw new Error("missing-orderId");

  const cloudSave = new DataApi(context);
  const ledgerKey = "paypal_order_" + orderId;
  const PENDING_KEY = "paypal_pending";

  // 미해결 주문 기록 비우기. 삭제 대신 빈 값으로 덮는다(삭제 API 시그니처 의존을 피함).
  // 실패해도 지급 자체를 막지 않는다 — 남아 있어도 다음 정리 때 멱등하게 처리된다.
  const clearPending = async function () {
    try {
      await cloudSave.setItem(projectId, playerId, { key: PENDING_KEY, value: { orderId: "" } });
    } catch (e) {
      logger.error("pending 정리 실패", { orderId: orderId, "error.message": describe(e) });
    }
  };

  // ③ 멱등 — 이미 지급한 주문이면 재지급 없이 종료.
  //    Cloud Save에 처리 기록을 남기고, 다음 요청부터 이 기록으로 걸러낸다.
  try {
    const prev = await cloudSave.getItems(projectId, playerId, [ledgerKey]);
    if (prev.data && prev.data.results && prev.data.results.length > 0) {
      await clearPending();
      return { alreadyProcessed: true, granted: 0, sku: "" };
    }
  } catch (e) { /* 조회 실패는 아래 capture로 진행 — 최종 지급 전 다시 기록 확인 */ }

  let clientId, secret;
  try {
    clientId = (await secretManager.getSecret("PAYPAL_CLIENT_ID")).value;
    secret = (await secretManager.getSecret("PAYPAL_SECRET")).value;
  } catch (e) {
    logger.error("secret lookup failed", { "error.message": describe(e) });
    throw new Error("paypal-secrets-missing");
  }

  let env = "sandbox";
  try { env = (await secretManager.getSecret("PAYPAL_ENV")).value || "sandbox"; } catch (e) {}
  const base = apiBase(env);

  let token;
  try {
    token = await getAccessToken(base, clientId, secret);
  } catch (e) {
    logger.error("paypal oauth failed", { env: env, "error.message": describe(e) });
    throw new Error("paypal-oauth-failed");
  }

  const authHeaders = { "Authorization": "Bearer " + token, "Content-Type": "application/json" };

  // 결제 확정(capture)
  // validateStatus로 모든 상태를 직접 받아본다 — 실패 본문을 봐야 원인을 구분할 수 있다.
  const capRes = await axios.post(
    base + "/v2/checkout/orders/" + orderId + "/capture",
    {},
    { headers: authHeaders, validateStatus: function () { return true; } }
  );
  let order = capRes.data;

  // 이미 확정된 주문이면 주문 조회로 대체한다.
  // 결제는 이미 성립했으므로 여기서 포기하면 "돈은 받고 재화는 안 준" 상태가 영구히 남는다.
  // (capture 성공 후 지급 단계에서 실패해 재시도하는 경우가 여기로 온다. 중복 지급은
  //  아래 Cloud Save 원장 확인이 막는다.)
  const alreadyCaptured =
    capRes.status === 422 &&
    order && order.details && order.details.some(function (d) { return d.issue === "ORDER_ALREADY_CAPTURED"; });

  if (alreadyCaptured) {
    logger.info("order already captured — 주문 조회로 복구 시도", { orderId: orderId });
    const getRes = await axios.get(
      base + "/v2/checkout/orders/" + orderId,
      { headers: authHeaders, validateStatus: function () { return true; } }
    );
    if (getRes.status >= 200 && getRes.status < 300) order = getRes.data;
  }

  if (!order || order.status !== "COMPLETED") {
    logger.error("capture not completed", {
      httpStatus: capRes.status,
      orderStatus: order && order.status ? order.status : "none",
      body: JSON.stringify(order),
      orderId: orderId
    });
    throw new Error("payment-not-completed: " + ((order && order.status) || capRes.status));
  }

  // ② 금액·SKU 검증 — custom_id(playerId|sku)와 실제 결제 금액을 서버 가격표와 대조.
  const pu = order.purchase_units && order.purchase_units[0];
  const capture = pu && pu.payments && pu.payments.captures && pu.payments.captures[0];

  // custom_id는 응답 형태에 따라 purchase_units 바로 아래에 오기도 하고
  // payments.captures[] 안에 실리기도 한다(capture 응답과 order 조회 응답이 다름). 양쪽을 본다.
  const customId =
    (pu && pu.custom_id) ||
    (capture && capture.custom_id) ||
    "";

  if (!customId) {
    // 여기서 못 찾으면 지급 근거가 없다. 원인 파악을 위해 응답 전문을 남긴다.
    logger.error("custom_id missing in paypal response", {
      orderId: orderId,
      body: JSON.stringify(order)
    });
    throw new Error("custom-id-missing");
  }

  const parts = customId.split("|");
  const paidPlayerId = parts[0];
  const sku = parts[1];
  const pkg = PACKAGES[sku];

  if (!pkg) throw new Error("unknown-sku-in-order: " + sku);
  if (paidPlayerId !== playerId) throw new Error("player-mismatch"); // 남의 결제로 내 계정 충전 방지

  // 금액도 capture 우선, 없으면 purchase_unit의 금액으로 대조.
  const amt = (capture && capture.amount) || (pu && pu.amount) || null;
  const paidAmount = amt ? amt.value : null;
  const paidCurrency = amt ? amt.currency_code : null;
  if (paidCurrency !== "USD" || paidAmount !== pkg.priceUsd) {
    throw new Error("amount-mismatch: paid " + paidCurrency + " " + paidAmount + " expected USD " + pkg.priceUsd);
  }

  // 멱등 재확인(capture와 지급 사이 경쟁 방어) 후 지급.
  try {
    const prev2 = await cloudSave.getItems(projectId, playerId, [ledgerKey]);
    if (prev2.data && prev2.data.results && prev2.data.results.length > 0) {
      await clearPending();
      return { alreadyProcessed: true, granted: 0, sku: sku };
    }
  } catch (e) {}

  // ④ GEM 지급(서비스 계정) — Access Control에서 Player 증액이 Deny여도 통과.
  const currencies = new CurrenciesApi(context);
  const incRes = await currencies.incrementPlayerCurrencyBalance({
    projectId, playerId, currencyId: "GEM",
    currencyModifyBalanceRequest: { amount: pkg.gem }
  });

  // ── 여기서부터는 "이미 지급된" 상태다 ──────────────────────────
  // 이 뒤의 어떤 작업이 실패하더라도 절대 예외를 던지면 안 된다.
  // 던지는 순간 클라이언트는 지급 실패로 표시하는데, 실제로는 젬이 들어가 있어서
  // "결제 실패라더니 젬은 있네" 같은 모순된 화면이 나온다.

  // 지급 직후 잔액 — 청약철회 시 "이 결제분을 썼는지" 판정 기준이 된다.
  // 젬은 사용으로만 줄어드므로, 나중에 잔액이 이 값 이상이면 소비하지 않은 것이고
  // 동시에 차감해도 음수가 되지 않는 것까지 보장된다. (증액 응답의 잔액을 그대로 사용 —
  // 여기서 또 조회하면 왕복이 늘어 스크립트 제한 시간을 잡아먹는다)
  let balanceAfter = -1;
  try { if (incRes && incRes.data && typeof incRes.data.balance === "number") balanceAfter = incRes.data.balance; } catch (e) {}

  // 환불에 필요한 capture ID — PayPal Refund API가 이 값을 받는다. 없으면 자동 철회 불가.
  const captureId = capture && capture.id ? capture.id : "";

  // 처리 기록 남기기(멱등 근거). 지급 후 기록 — 지급 전에 남기면 지급 실패 시 영영 못 받는다.
  // 실패해도 응답은 성공으로 돌려주되, 중복 지급 위험이 생기므로 로그로 크게 남긴다.
  const capturedAt = new Date().toISOString();
  try {
    await cloudSave.setItem(projectId, playerId, {
      key: ledgerKey,
      value: { sku: sku, gem: pkg.gem, capturedAt: capturedAt }
    });
  } catch (e) {
    logger.error("ledger write failed AFTER grant — 재요청 시 중복 지급 위험", {
      orderId: orderId, playerId: playerId, gem: pkg.gem, "error.message": describe(e)
    });
  }

  // 청약철회 화면용 결제 내역. 개별 ledger 키는 목록 조회가 안 되므로 배열로 따로 쌓는다.
  try {
    const prevHist = await cloudSave.getItems(projectId, playerId, [HISTORY_KEY]);
    let list = [];
    if (prevHist.data && prevHist.data.results && prevHist.data.results.length > 0) {
      const v = prevHist.data.results[0].value;
      if (Array.isArray(v)) list = v;
    }
    if (!list.some(function (e) { return e.orderId === orderId; })) {
      list.push({
        orderId: orderId,
        captureId: captureId,
        sku: sku,
        gem: pkg.gem,
        amount: pkg.priceUsd,
        currency: "USD",
        capturedAt: capturedAt,
        balanceAfter: balanceAfter,
        status: "completed"
      });
    }
    if (list.length > MAX_HISTORY) list = list.slice(list.length - MAX_HISTORY); // 무한 증가 방지
    await cloudSave.setItem(projectId, playerId, { key: HISTORY_KEY, value: list });
  } catch (e) {
    logger.error("purchase history write failed — 청약철회 목록에 안 뜸", {
      orderId: orderId, "error.message": describe(e)
    });
  }

  // 잔액은 반환하지 않는다. 클라이언트가 성공 후 프로필을 새로 받아가고(진실은 서버),
  // 여기서 한 번 더 조회하면 왕복만 늘어 스크립트 제한 시간을 잡아먹는다.
  await clearPending();   // 지급 완료 — 미해결 주문 아님
  return { granted: pkg.gem, sku: sku, alreadyProcessed: false };
};

module.exports.params = {
  orderId: { type: "STRING", required: true }
};
