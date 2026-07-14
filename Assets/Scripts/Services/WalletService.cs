using UnityEngine;

// 재화 지갑(로컬 MVP) — GEM(뽑기 재화) + 구슬 조각(Shard, 확정 교환용). 계정별 PlayerPrefs.
// TODO(Stage 2): GEM은 UGS Economy Currency로, 조각도 Economy 또는 Cloud Save로 이관(서버 권위).
public static class WalletService
{
    const string GemBase = "Wallet.gem";
    const string ShardBase = "Wallet.shard";
    const string InitBase = "Wallet.init";

    public const int StartingGem = 1500; // 신규 계정 테스트 지급(실제론 구매/Economy로 대체)

    public static int Gem
    {
        get { EnsureInit(); return PlayerPrefs.GetInt(AccountScope.Key(GemBase), 0); }
    }

    public static int Shards
    {
        get { EnsureInit(); return PlayerPrefs.GetInt(AccountScope.Key(ShardBase), 0); }
    }

    public static bool SpendGem(int amount)
    {
        if (amount <= 0) return true;
        int bal = Gem;
        if (bal < amount) return false;
        PlayerPrefs.SetInt(AccountScope.Key(GemBase), bal - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static void AddGem(int amount)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(AccountScope.Key(GemBase), Gem + amount);
        PlayerPrefs.Save();
    }

    public static void AddShards(int amount)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(AccountScope.Key(ShardBase), Shards + amount);
        PlayerPrefs.Save();
    }

    public static bool SpendShards(int amount)
    {
        if (amount <= 0) return true;
        int bal = Shards;
        if (bal < amount) return false;
        PlayerPrefs.SetInt(AccountScope.Key(ShardBase), bal - amount);
        PlayerPrefs.Save();
        return true;
    }

    // 테스트/디버그
    public static void ResetForTest()
    {
        PlayerPrefs.DeleteKey(AccountScope.Key(GemBase));
        PlayerPrefs.DeleteKey(AccountScope.Key(ShardBase));
        PlayerPrefs.DeleteKey(AccountScope.Key(InitBase));
        PlayerPrefs.Save();
    }

    static void EnsureInit()
    {
        string initKey = AccountScope.Key(InitBase);
        if (PlayerPrefs.HasKey(initKey)) return;
        PlayerPrefs.SetInt(AccountScope.Key(GemBase), StartingGem);
        PlayerPrefs.SetInt(AccountScope.Key(ShardBase), 0);
        PlayerPrefs.SetInt(initKey, 1);
        PlayerPrefs.Save();
    }
}
