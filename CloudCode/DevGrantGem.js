// DevGrantGem — 개발용 GEM 지급 스크립트 (UGS Cloud Code / JavaScript).
//
// ⚠️ 보안 경고: 이 스크립트는 클라이언트가 직접 호출할 수 있어 "무료 GEM 치트 통로"다.
//    → Sandbox 개발/밸런스 검증 용도로만 사용하고,
//      실서비스(라이브) 전환 전 반드시 삭제하거나 접근을 차단할 것.
//    실제 PayPal 결제 지급은 별도 스크립트(PayPalCapture)가 결제를 서버에서
//    검증(capture)한 뒤에만 GEM을 지급한다. (2단계)
//
// 인증: new CurrenciesApi(context) 로 "서비스 계정" 권한으로 호출한다.
//   → 프로젝트 Access Control에서 Player의 increment/decrement 를 Deny 해도
//     서비스 계정 호출은 통과한다(= 서버 권위). { accessToken } 만 넘기면
//     플레이어 권한이라 Deny 정책에 막히니 절대 그렇게 하지 말 것.

const { CurrenciesApi } = require("@unity-services/economy-2.4");

module.exports = async ({ params, context, logger }) => {
  const { projectId, playerId } = context;
  const currencyId = params.currencyId || "GEM";
  const amount = params.amount;

  if (!Number.isInteger(amount) || amount <= 0) {
    throw new Error("amount must be a positive integer");
  }

  const currencies = new CurrenciesApi(context); // 서비스 계정 인증

  try {
    await currencies.incrementPlayerCurrencyBalance({
      projectId,
      playerId,
      currencyId,
      currencyModifyBalanceRequest: { amount: amount }
    });

    // 갱신된 전체 잔액을 반환(클라에서 참고용). 진실은 항상 서버.
    const result = await currencies.getPlayerCurrencies({ projectId, playerId });
    return result.data;
  } catch (err) {
    logger.error("DevGrantGem failed", { "error.message": err.message });
    throw err;
  }
};

module.exports.params = {
  currencyId: { type: "STRING", required: false },
  amount: { type: "NUMERIC", required: true }
};
