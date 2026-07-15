using UnityEngine;

// ♦ Mirror World 상태 — 지속시간 동안 완전 무적이 되며, 받았을 피해를 "그대로"(분산 아님)
// 반경 내 모든 적에게 되돌린다. (1 피해를 받으면 모든 적이 각각 1 피해)
public class MirrorWorldStatus : MonoBehaviour, IPlayerDamageModifier, IBuffDisplay
{
    private float radius;
    private float remain;
    private SpellMarble marble;

    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return true; } }
    public float BuffRemaining { get { return remain; } }
    public int BuffCharges { get { return 0; } }

    public static void Apply(GameObject player, float radius, float duration, SpellMarble marble = null)
    {
        MirrorWorldStatus s = player.GetComponent<MirrorWorldStatus>();
        if (s == null) s = player.AddComponent<MirrorWorldStatus>();
        s.radius = radius;
        s.marble = marble;
        s.remain = Mathf.Max(s.remain, duration);
    }

    public float ModifyIncomingDamage(float damage, GameObject attacker)
    {
        // 반경 내 모든 적에게 "받았을 피해 전량"을 각각 반사(분산 아님)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy")) continue;
            IDamageable dmg = hits[i].GetComponent<IDamageable>();
            if (dmg != null) dmg.ApplyHit(damage);
        }
        return 0f; // 완전 무적(반경 내 적이 없어도 무피해)
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) Destroy(this);
    }
}
