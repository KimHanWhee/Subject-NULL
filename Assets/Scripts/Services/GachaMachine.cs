using System.Collections.Generic;
using UnityEngine;

// 로컬 가챠 로직(MVP) — 클라 RNG로 Gold+ 마블 추첨 + 소유/조각 처리.
// TODO(Stage 2): RNG·재화·소유를 UGS Cloud Code(pullGacha)로 이관(서버 권위·치트 방지·확률 공시).
public static class GachaMachine
{
    public class PullResult
    {
        public SpellMarble marble;
        public bool isNew;       // 새로 획득했는지(중복이면 false)
        public int shardsGained; // 이번 뽑기로 얻은 조각
    }

    public static bool CanPull() => CanPull(1);
    public static bool CanPull(int count) => count > 0 && WalletService.Gem >= GachaConfig.Instance.pullCostGem * count;

    // 다연차 뽑기(예: 10연). 총 비용을 먼저 확인 후 count회 실행.
    public static List<PullResult> PullMulti(SpellMarbleRegistry registry, int count)
    {
        if (registry == null || count <= 0) return null;
        if (!CanPull(count)) return null;
        List<PullResult> results = new List<PullResult>();
        for (int i = 0; i < count; i++)
        {
            PullResult r = Pull(registry);
            if (r != null) results.Add(r);
        }
        return results;
    }

    // 1회 뽑기. GEM 부족이면 null.
    public static PullResult Pull(SpellMarbleRegistry registry)
    {
        if (registry == null) return null;
        GachaConfig cfg = GachaConfig.Instance;
        if (!WalletService.SpendGem(cfg.pullCostGem)) return null;

        Grade grade = RollGrade(cfg);
        SpellMarble marble = RandomOfGrade(registry, grade);
        if (marble == null) marble = RandomGoldPlus(registry); // 해당 등급 마블이 없으면 폴백
        if (marble == null) { WalletService.AddGem(cfg.pullCostGem); return null; } // 풀 자체가 비면 환불

        PullResult r = new PullResult { marble = marble };
        bool ownedBefore = OwnedMarblesService.IsOwned(marble);
        if (ownedBefore)
        {
            r.isNew = false;
            r.shardsGained = cfg.DupShards(marble.grade);
        }
        else
        {
            r.isNew = true;
            OwnedMarblesService.Grant(marble.marbleName);
            r.shardsGained = cfg.shardNewBonus;
        }
        WalletService.AddShards(r.shardsGained);
        return r;
    }

    // 확정 교환: 조각으로 특정 미보유 Gold+ 마블을 직접 획득.
    public static int ExchangeCost(SpellMarble m) => m != null ? GachaConfig.Instance.ExchangeCost(m.grade) : int.MaxValue;

    public static bool CanExchange(SpellMarble m)
        => m != null && m.grade != Grade.Normal && !OwnedMarblesService.IsOwned(m) && WalletService.Shards >= ExchangeCost(m);

    public static bool Exchange(SpellMarble m)
    {
        if (!CanExchange(m)) return false;
        if (!WalletService.SpendShards(ExchangeCost(m))) return false;
        OwnedMarblesService.Grant(m.marbleName);
        return true;
    }

    // ── 내부 ──────────────────────────────────────────────
    static Grade RollGrade(GachaConfig cfg)
    {
        int total = cfg.Weight(Grade.Gold) + cfg.Weight(Grade.Diamond) + cfg.Weight(Grade.Legend);
        if (total <= 0) return Grade.Gold;
        int roll = Random.Range(0, total);
        if (roll < cfg.Weight(Grade.Gold)) return Grade.Gold;
        roll -= cfg.Weight(Grade.Gold);
        if (roll < cfg.Weight(Grade.Diamond)) return Grade.Diamond;
        return Grade.Legend;
    }

    static SpellMarble RandomOfGrade(SpellMarbleRegistry reg, Grade grade)
    {
        List<SpellMarble> pool = new List<SpellMarble>();
        foreach (SpellMarble m in reg.allMarbles)
            if (m != null && m.grade == grade) pool.Add(m);
        return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
    }

    static SpellMarble RandomGoldPlus(SpellMarbleRegistry reg)
    {
        List<SpellMarble> pool = new List<SpellMarble>();
        foreach (SpellMarble m in reg.allMarbles)
            if (m != null && m.grade != Grade.Normal) pool.Add(m);
        return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
    }
}
