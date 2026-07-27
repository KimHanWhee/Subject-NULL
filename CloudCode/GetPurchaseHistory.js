// GetPurchaseHistory — 청약철회 화면용 결제 내역 조회.
//
// 철회 가능 여부까지 서버가 판단해서 내려준다. 클라이언트가 판단하면 조작할 수 있고,
// 판정 규칙이 서버(WithdrawPurchase)와 어긋나면 "버튼은 활성인데 눌리면 실패"가 된다.
// 규칙은 한 곳(여기와 WithdrawPurchase가 같은 조건)에서만 정의한다.
//
// 철회 조건 3가지:
//   ① status가 completed (이미 환불했거나 처리 중이면 불가)
//   ② 결제일로부터 7일 이내
//   ③ 현재 GEM 잔액 >= 결제 직후 잔액(balanceAfter)
//      → 젬은 사용으로만 줄어드니, 이 값 이상이면 그 결제분을 쓰지 않은 것이다.
//        동시에 차감해도 음수가 되지 않음이 보장된다.

const { CurrenciesApi } = require("@unity-services/economy-2.4");
const { DataApi } = require("@unity-services/cloud-save-1.4");

const HISTORY_KEY = "purchase_history";
const WITHDRAW_DAYS = 7;

module.exports = async ({ context, logger }) => {
  const { projectId, playerId } = context;

  let list = [];
  try {
    const cloudSave = new DataApi(context);
    const res = await cloudSave.getItems(projectId, playerId, [HISTORY_KEY]);
    if (res.data && res.data.results && res.data.results.length > 0) {
      const v = res.data.results[0].value;
      if (Array.isArray(v)) list = v;
    }
  } catch (e) {
    logger.error("purchase history 조회 실패", { "error.message": e.message });
    return { items: [] };
  }

  if (list.length === 0) return { items: [] };

  // 현재 잔액 — 사용 여부 판정에 필요
  let gem = 0;
  try {
    const currencies = new CurrenciesApi(context);
    const bal = await currencies.getPlayerCurrencies({ projectId, playerId });
    const balances = bal.data && bal.data.results ? bal.data.results : [];
    for (let i = 0; i < balances.length; i++) {
      if (balances[i].currencyId === "GEM") { gem = balances[i].balance; break; }
    }
  } catch (e) {
    logger.error("잔액 조회 실패", { "error.message": e.message });
    return { items: [] };
  }

  const now = Date.now();
  const items = [];

  // 최신순
  for (let i = list.length - 1; i >= 0; i--) {
    const e = list[i];
    const ageDays = e.capturedAt ? (now - new Date(e.capturedAt).getTime()) / 86400000 : 9999;

    let reason = "ok";
    if (e.status === "refunded") reason = "refunded";
    else if (e.status === "refunding") reason = "processing";
    else if (!e.captureId) reason = "no-capture";       // 기능 도입 전 결제 — 자동 철회 불가
    else if (ageDays > WITHDRAW_DAYS) reason = "expired";
    else if (typeof e.balanceAfter !== "number" || e.balanceAfter < 0) reason = "no-baseline";
    else if (gem < e.balanceAfter) reason = "used";     // 결제 이후 GEM을 사용함

    items.push({
      orderId: e.orderId,
      sku: e.sku,
      gem: e.gem,
      amount: e.amount,
      currency: e.currency || "USD",
      capturedAt: e.capturedAt,
      canWithdraw: reason === "ok",
      reason: reason
    });
  }

  return { items: items, gem: gem };
};
