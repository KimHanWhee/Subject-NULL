using UnityEngine;

// 가챠 설정 — Resources/GachaConfig.asset에서 인스펙터로 조절. 없으면 기본값 폴백(씬 배선 불필요).
// 뽑기 풀 = Gold 이상(Normal은 무료라 제외).
[CreateAssetMenu(fileName = "GachaConfig", menuName = "Spell/GachaConfig")]
public class GachaConfig : ScriptableObject
{
    [Header("비용")]
    public int pullCostGem = 100;   // 1회 뽑기 비용(GEM)

    [Header("등급 확률(상대 가중치)")]
    public int weightGold = 80;
    public int weightDiamond = 17;
    public int weightLegend = 3;

    [Header("구슬 조각 획득")]
    public int shardNewBonus = 5;    // 새 마블 획득 시 보너스 조각
    public int shardDupGold = 15;    // 중복 시 환급 조각
    public int shardDupDiamond = 40;
    public int shardDupLegend = 120;

    [Header("확정 교환 비용(조각)")]
    public int exchangeGold = 90;
    public int exchangeDiamond = 240;
    public int exchangeLegend = 700;

    public int Weight(Grade g)
    {
        switch (g) { case Grade.Gold: return Mathf.Max(0, weightGold); case Grade.Diamond: return Mathf.Max(0, weightDiamond); case Grade.Legend: return Mathf.Max(0, weightLegend); default: return 0; }
    }
    public int DupShards(Grade g)
    {
        switch (g) { case Grade.Gold: return shardDupGold; case Grade.Diamond: return shardDupDiamond; case Grade.Legend: return shardDupLegend; default: return 0; }
    }
    public int ExchangeCost(Grade g)
    {
        switch (g) { case Grade.Gold: return exchangeGold; case Grade.Diamond: return exchangeDiamond; case Grade.Legend: return exchangeLegend; default: return int.MaxValue; }
    }

    private static GachaConfig instance;
    public static GachaConfig Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<GachaConfig>("GachaConfig");
            if (instance == null) instance = CreateInstance<GachaConfig>();
            return instance;
        }
    }
}
