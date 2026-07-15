using UnityEngine;

// ♦ Iron Skin 상태 — 지속시간 동안 받는 피해 감소(기본 50%).
public class IronSkinStatus : MonoBehaviour, IPlayerDamageModifier, IBuffDisplay
{
    private float reduction; // 0.5 = 50% 감소
    private float remain;
    private SpellMarble marble;

    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return true; } }
    public float BuffRemaining { get { return remain; } }
    public int BuffCharges { get { return 0; } }

    public static void Apply(GameObject player, float reduction, float duration, SpellMarble marble = null)
    {
        IronSkinStatus s = player.GetComponent<IronSkinStatus>();
        if (s == null) s = player.AddComponent<IronSkinStatus>();
        s.reduction = Mathf.Clamp01(reduction);
        s.marble = marble;
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
