// ExchangeMarble — 서버 권위 확정 교환. 조각을 소모해 미보유 Gold+ 마블을 직접 획득.
//   소유 = Cloud Save "ownedMarbles", 조각 = Cloud Save "shards".

const { DataApi } = require("@unity-services/cloud-save-1.4");

const CATALOG = {
  Gold:    ["freeze", "fortress", "second-wind", "adrenaline", "sniper-mode", "chain-lightning", "teleport", "thunder-bomb", "ghost-step", "void-slash"],
  Diamond: ["black-hole", "slow-aura", "railgun", "em-field", "safe-zone"],
  Legend:  ["resurrection", "mirror-world", "apocalypse", "time-stop"]
};
const EXCHANGE = { Gold: 90, Diamond: 240, Legend: 700 };

function gradeOf(name) {
  const grades = Object.keys(CATALOG);
  for (let i = 0; i < grades.length; i++) if (CATALOG[grades[i]].indexOf(name) >= 0) return grades[i];
  return null;
}

module.exports = async ({ params, context, logger }) => {
  const projectId = context.projectId;
  const playerId = context.playerId;
  const cloud = new DataApi(context);

  const marbleName = params.marbleName;
  const grade = gradeOf(marbleName);
  if (!grade) return { ok: false, error: "UNKNOWN_MARBLE" };
  const cost = EXCHANGE[grade];

  const items = await cloud.getItems(projectId, playerId, ["ownedMarbles", "shards"]);
  let owned = [];
  let shards = 0;
  const results = (items.data && items.data.results) || [];
  for (let i = 0; i < results.length; i++) {
    const it = results[i];
    if (it.key === "ownedMarbles" && Array.isArray(it.value)) owned = it.value;
    if (it.key === "shards" && typeof it.value === "number") shards = it.value;
  }

  if (owned.indexOf(marbleName) >= 0) return { ok: false, error: "ALREADY_OWNED", shards: shards };
  if (shards < cost) return { ok: false, error: "INSUFFICIENT_SHARDS", shards: shards };

  owned.push(marbleName);
  shards -= cost;
  await cloud.setItem(projectId, playerId, { key: "ownedMarbles", value: owned });
  await cloud.setItem(projectId, playerId, { key: "shards", value: shards });

  return { ok: true, shards: shards };
};

module.exports.params = {
  marbleName: { type: "STRING", required: true }
};
