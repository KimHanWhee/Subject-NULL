using UnityEngine;

// ♦ Iron Skin 상태 — 지속시간 동안 받는 피해 감소(기본 50%).
public class IronSkinStatus : MonoBehaviour, IPlayerDamageModifier
{
    private float reduction; // 0.5 = 50% 감소
    private float remain;

    public static void Apply(GameObject player, float reduction, float duration)
    {
        IronSkinStatus s = player.GetComponent<IronSkinStatus>();
        if (s == null) s = player.AddComponent<IronSkinStatus>();
        s.reduction = Mathf.Clamp01(reduction);
        s.remain = Mathf.Max(s.remain, duration);
    }

    public float ModifyIncomingDamage(float damage, GameObject attacker)
        => damage * (1f - reduction);

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) Destroy(this);
    }
}
