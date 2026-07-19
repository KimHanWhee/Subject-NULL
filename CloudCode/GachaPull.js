// PullGacha — 서버 권위 가챠 1회. GEM 차감(서버) + 서버 RNG + 소유 지급(Cloud Save) + 조각.
// 클라는 결과 marbleName으로 SO를 찾아 연출만. RNG/재화/소유는 전적으로 서버.
//
// 저장소:
//   GEM     = Economy currency "GEM" (Access Control로 플레이어 증감 차단됨)
//   소유    = Cloud Save "ownedMarbles" (Gold+ marbleName 배열; Normal은 무료라 미저장)
//   조각    = Cloud Save "shards" (number)

const { CurrenciesApi } = require("@unity-services/economy-2.4");
const { DataApi } = require("@unity-services/cloud-save-1.4");

const COST = 100;
const CATALOG = {
  Gold:    ["freeze", "fortress", "second-wind", "adrenaline", "sniper-mode", "chain-lightning", "teleport", "thunder-bomb", "ghost-step"],
  Diamond: ["black-hole", "slow-aura", "railgun", "em-field", "safe-zone"],
  Legend:  ["resurrection", "mirror-world", "apocalypse", "time-stop"]
};
const WEIGHTS = { Gold: 80, Diamond: 17, Legend: 3 };
const SHARD_NEW = 5;
const SHARD_DUP = { Gold: 15, Diamond: 40, Legend: 120 };

function rollGrade() {
  const total = WEIGHTS.Gold + WEIGHTS.Diamond + WEIGHTS.Legend;
  let roll = Math.floor(Math.random() * total);
  if (roll < WEIGHTS.Gold) return "Gold";
  roll -= WEIGHTS.Gold;
  if (roll < WEIGHTS.Diamond) return "Diamond";
  return "Legend";
}

function readGem(currenciesData) {
  const arr = (currenciesData && currenciesData.results) || [];
  for (let i = 0; i < arr.length; i++) if (arr[i].currencyId === "GEM") return arr[i].balance;
  return 0;
}

module.exports = async ({ context, logger }) => {
  const projectId = context.projectId;
  const playerId = context.playerId;
  const currencies = new CurrenciesApi(context);
  const cloud = new DataApi(context);

  // 1) GEM 차감 시도 (잔액 부족이면 Economy가 min(0) 위반으로 예외 → 환급 불필요)
  try {
    await currencies.decrementPlayerCurrencyBalance({
      projectId, playerId, currencyId: "GEM",
      currencyModifyBalanceRequest: { amount: COST }
    });
  } catch (e) {
    return { error: "INSUFFICIENT_GEM" };
  }

  // 2) 서버 RNG
  const grade = rollGrade();
  const pool = CATALOG[grade];
  const marbleName = pool[Math.floor(Math.random() * pool.length)];

  // 3) 소유/조각 읽기
  let owned = [];
  let shards = 0;
  const items = await cloud.getItems(projectId, playerId, ["ownedMarbles", "shards"]);
  const results = (items.data && items.data.results) || [];
  for (let i = 0; i < results.length; i++) {
    const it = results[i];
    if (it.key === "ownedMarbles" && Array.isArray(it.value)) owned = it.value;
    if (it.key === "shards" && typeof it.value === "number") shards = it.value;
  }

  // 4) 신규/중복 처리
  const isNew = owned.indexOf(marbleName) === -1;
  let shardsGained;
  if (isNew) {
    owned.push(marbleName);
    shardsGained = SHARD_NEW;
    await cloud.setItem(projectId, playerId, { key: "ownedMarbles", value: owned });
  } else {
    shardsGained = SHARD_DUP[grade];
  }
  shards += shardsGained;
  await cloud.setItem(projectId, playerId, { key: "shards", value: shards });

  // 5) 갱신 GEM
  const after = await currencies.getPlayerCurrencies({ projectId, playerId });
  const gemAfter = readGem(after.data);

  return {
    marbleName: marbleName,
    grade: grade,
    isNew: isNew,
    shardsGained: shardsGained,
    gem: gemAfter,
    shards: shards
  };
};
