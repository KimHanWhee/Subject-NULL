// WithdrawPurchase — 사용자가 직접 청약철회(전액 환불)를 실행한다.
//
// 실제 돈이 움직이므로 다음 원칙을 지킨다.
//
// ① 순서: GEM을 먼저 빼고 → 그다음 PayPal 환불.
//    반대로 하면 환불 성공 + 차감 실패 시 "돈도 받고 젬도 가진" 상태가 되고 되돌릴 수단이 없다.
//    이 순서면 환불이 실패해도 젬을 도로 넣어주면 원상복구된다.
//
// ② 멱등: PayPal을 호출하기 전에 기록을 "refunding"으로 표시한다.
//    버튼 연타나 재요청이 두 번째 환불로 이어지지 않는다.
//
// ③ 판정은 서버가 한다. 클라이언트는 orderId만 보낸다.
//    조건은 GetPurchaseHistory와 동일해야 한다("버튼은 켜졌는데 눌리면 실패"를 막기 위함).

const axios = require("axios-1.6");
const { CurrenciesApi } = require("@unity-services/economy-2.4");
const { DataApi } = require("@unity-services/cloud-save-1.4");

const HISTORY_KEY = "purchase_history";
const WITHDRAW_DAYS = 7;

// 대소문자·공백 정규화 — CreatePaypalOrder와 동일 규약.
function normalizeEnv(v) {
  return String(v || "").trim().toLowerCase() === "live" ? "live" : "sandbox";
}

function apiBase(env) {
  return normalizeEnv(env) === "live" ? "https://api-m.paypal.com" : "https://api-m.sandbox.paypal.com";
}

function describe(err) {
  if (err && err.response) return "HTTP " + err.response.status + " " + JSON.stringify(err.response.data);
  return err && err.message ? err.message : String(err);
}

async function getAccessToken(base, clientId, secret) {
  const res = await axios.post(
    base + "/v1/oauth2/token",
    "grant_type=client_credentials",
    { auth: { username: clientId, password: secret },
      headers: { "Content-Type": "application/x-www-form-urlencoded" } }
  );
  return res.data.access_token;
}

module.exports = async ({ params, context, logger, secretManager }) => {
  const { projectId, playerId } = context;
  const orderId = params.orderId;
  if (!orderId) throw new Error("missing-orderId");

  const cloudSave = new DataApi(context);
  const currencies = new CurrenciesApi(context);

  // ── 내역 로드 ──
  let list = [];
  const hist = await cloudSave.getItems(projectId, playerId, [HISTORY_KEY]);
  if (hist.data && hist.data.results && hist.data.results.length > 0) {
    const v = hist.data.results[0].value;
    if (Array.isArray(v)) list = v;
  }
  let idx = -1;
  for (let i = 0; i < list.length; i++) if (list[i].orderId === orderId) { idx = i; break; }
  if (idx < 0) throw new Error("order-not-found");

  const entry = list[idx];

  // ── 자격 검증(GetPurchaseHistory와 동일 조건) ──
  if (entry.status === "refunded") throw new Error("already-refunded");
  if (entry.status === "refunding") throw new Error("refund-in-progress");
  if (!entry.captureId) throw new Error("no-capture-id");

  const ageDays = entry.capturedAt ? (Date.now() - new Date(entry.capturedAt).getTime()) / 86400000 : 9999;
  if (ageDays > WITHDRAW_DAYS) throw new Error("withdraw-period-expired");
  if (typeof entry.balanceAfter !== "number" || entry.balanceAfter < 0) throw new Error("no-baseline");

  let gem = 0;
  const bal = await currencies.getPlayerCurrencies({ projectId, playerId });
  const balances = bal.data && bal.data.results ? bal.data.results : [];
  for (let i = 0; i < balances.length; i++) if (balances[i].currencyId === "GEM") { gem = balances[i].balance; break; }
  if (gem < entry.balanceAfter) throw new Error("gem-already-used");

  // ── ② 처리 중 표시(PayPal 호출 전) ──
  list[idx].status = "refunding";
  await cloudSave.setItem(projectId, playerId, { key: HISTORY_KEY, value: list });

  // 차감을 되돌리고 상태를 원복한다(환불이 성립하지 않았을 때만 호출).
  async function restoreGem() {
    try {
      await currencies.incrementPlayerCurrencyBalance({
        projectId, playerId, currencyId: "GEM",
        currencyModifyBalanceRequest: { amount: entry.gem }
      });
      list[idx].status = "completed";
      await cloudSave.setItem(projectId, playerId, { key: HISTORY_KEY, value: list });
    } catch (e2) {
      // 여기까지 실패하면 사용자는 젬을 잃고 환불도 못 받은 상태다. 반드시 사람이 봐야 한다.
      logger.error("치명적: GEM 복구 실패 — 수동 보정 필요", {
        orderId: orderId, playerId: playerId, gem: entry.gem, "error.message": describe(e2)
      });
    }
  }

  // ── ① GEM 먼저 차감 ──
  try {
    await currencies.decrementPlayerCurrencyBalance({
      projectId, playerId, currencyId: "GEM",
      currencyModifyBalanceRequest: { amount: entry.gem }
    });
  } catch (e) {
    list[idx].status = "completed";                       // 아무것도 안 일어났으니 원복
    await cloudSave.setItem(projectId, playerId, { key: HISTORY_KEY, value: list });
    logger.error("GEM 차감 실패 — 환불 중단", { orderId: orderId, "error.message": describe(e) });
    throw new Error("gem-decrement-failed");
  }

  // ── PayPal 환불 ──
  let clientId, secret;
  try {
    clientId = (await secretManager.getSecret("PAYPAL_CLIENT_ID")).value;
    secret = (await secretManager.getSecret("PAYPAL_SECRET")).value;
  } catch (e) {
    await restoreGem();
    throw new Error("paypal-secrets-missing");
  }
  let env = "sandbox";
  try { env = (await secretManager.getSecret("PAYPAL_ENV")).value || "sandbox"; } catch (e) {}
  const base = apiBase(env);

  let token;
  try {
    token = await getAccessToken(base, clientId, secret);
  } catch (e) {
    await restoreGem();
    logger.error("환불용 OAuth 실패", { orderId: orderId, "error.message": describe(e) });
    throw new Error("paypal-oauth-failed");
  }

  const refRes = await axios.post(
    base + "/v2/payments/captures/" + entry.captureId + "/refund",
    {},                                                   // 본문 없음 = 전액 환불
    { headers: { "Authorization": "Bearer " + token, "Content-Type": "application/json" },
      validateStatus: function () { return true; } }
  );

  const okRefund = refRes.status >= 200 && refRes.status < 300 &&
                   refRes.data && (refRes.data.status === "COMPLETED" || refRes.data.status === "PENDING");

  if (!okRefund) {
    logger.error("PayPal 환불 실패", {
      orderId: orderId, httpStatus: refRes.status, body: JSON.stringify(refRes.data)
    });
    await restoreGem();
    throw new Error("paypal-refund-failed");
  }

  // ── 완료 표시 ──
  list[idx].status = "refunded";
  list[idx].refundedAt = new Date().toISOString();
  list[idx].refundId = refRes.data.id || "";
  await cloudSave.setItem(projectId, playerId, { key: HISTORY_KEY, value: list });

  logger.info("청약철회 완료", { orderId: orderId, gem: entry.gem, amount: entry.amount });
  return { ok: true, gem: entry.gem, amount: entry.amount, currency: entry.currency || "USD" };
};

module.exports.params = {
  orderId: { type: "STRING", required: true }
};
