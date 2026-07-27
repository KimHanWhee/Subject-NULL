// GetPendingOrder — 이 플레이어에게 아직 정리되지 않은 PayPal 주문이 있는지 조회.
//
// 왜 필요한가(이중 결제 방어):
//   결제는 성립했는데 지급 단계에서 실패하면 "돈은 나갔고 젬은 없는" 상태가 된다.
//   그 상태에서 사용자가 다시 구매를 누르면 새 주문이 만들어져 두 번 결제된다.
//   주문 ID 단위 멱등 처리는 "같은 주문"만 막을 수 있어서 이 경우를 못 막는다.
//   → 주문 생성 시 남겨둔 기록을 클라이언트가 먼저 확인하고,
//     CapturePaypalOrder(orderId)로 이어받아 정리한 뒤에만 새 구매를 진행한다.
//
// 이 스크립트는 읽기만 한다. 실제 확정/지급/정리는 CapturePaypalOrder가 담당한다
// (그쪽이 이미 멱등 처리와 ORDER_ALREADY_CAPTURED 복구를 갖고 있어 로직을 한 곳에 둔다).

const { DataApi } = require("@unity-services/cloud-save-1.4");

const PENDING_KEY = "paypal_pending";

// PayPal 주문은 승인 없이 방치되면 몇 시간 뒤 만료된다.
// 그보다 오래된 기록은 이어받을 의미가 없으므로 없는 것으로 취급한다.
const MAX_AGE_MS = 6 * 60 * 60 * 1000;

module.exports = async ({ context, logger }) => {
  const { projectId, playerId } = context;

  try {
    const cloudSave = new DataApi(context);
    const res = await cloudSave.getItems(projectId, playerId, [PENDING_KEY]);
    const results = res.data && res.data.results ? res.data.results : [];
    if (results.length === 0) return { orderId: "", sku: "" };

    const v = results[0].value || {};
    const orderId = v.orderId || "";
    if (!orderId) return { orderId: "", sku: "" };   // 지급 완료 후 비워둔 상태

    if (v.createdAt) {
      const age = Date.now() - new Date(v.createdAt).getTime();
      if (age > MAX_AGE_MS) return { orderId: "", sku: "" };
    }

    return { orderId: orderId, sku: v.sku || "" };
  } catch (e) {
    // 조회 실패가 구매를 막아서는 안 된다 — 없는 것으로 보고 진행시킨다.
    logger.error("GetPendingOrder failed", { "error.message": e.message });
    return { orderId: "", sku: "" };
  }
};
