// GetProfile — 서버 권위 재화/소유 조회. 클라는 이걸로 GEM·조각·소유 마블을 읽는다.
// (클라에 Cloud Save 패키지가 없으므로 모든 소유/조각 읽기는 이 엔드포인트를 통한다.)

const { CurrenciesApi } = require("@unity-services/economy-2.4");
const { DataApi } = require("@unity-services/cloud-save-1.4");

module.exports = async ({ context, logger }) => {
  const projectId = context.projectId;
  const playerId = context.playerId;
  const currencies = new CurrenciesApi(context);
  const cloud = new DataApi(context);

  const bal = await currencies.getPlayerCurrencies({ projectId, playerId });
  let gem = 0;
  const arr = (bal.data && bal.data.results) || [];
  for (let i = 0; i < arr.length; i++) if (arr[i].currencyId === "GEM") gem = arr[i].balance;

  let owned = [];
  let shards = 0;
  const items = await cloud.getItems(projectId, playerId, ["ownedMarbles", "shards"]);
  const results = (items.data && items.data.results) || [];
  for (let i = 0; i < results.length; i++) {
    const it = results[i];
    if (it.key === "ownedMarbles" && Array.isArray(it.value)) owned = it.value;
    if (it.key === "shards" && typeof it.value === "number") shards = it.value;
  }

  return { gem: gem, shards: shards, owned: owned };
};
