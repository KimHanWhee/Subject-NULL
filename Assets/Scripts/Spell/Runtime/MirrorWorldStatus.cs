using UnityEngine;

// ♦ Mirror World 상태 — 지속시간 동안 받는 모든 피해가 주변 적에게 분산(본인 0).
// 반경 내 적이 없으면 피해를 그대로 받는다(무적 방지).
public class MirrorWorldStatus : MonoBehaviour, IPlayerDamageModifier
{
    private float radius;
    private float remain;

    public static void Apply(GameObject player, float radius, float duration)
    {
        MirrorWorldStatus s = player.GetComponent<MirrorWorldStatus>();
        if (s == null) s = player.AddComponent<MirrorWorldStatus>();
        s.radius = radius;
        s.remain = Mathf.Max(s.remain, duration);
    }

    public float ModifyIncomingDamage(float damage, GameObject attacker)
    {
        // 반경 내 적 수집
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        var enemies = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < hits.Length; i++)
            if (hits[i].CompareTag("Enemy")) enemies.Add(hits[i].gameObject);

        if (enemies.Count == 0) return damage; // 분산 대상 없음 → 본인이 받음

        float share = damage / enemies.Count;  // 피해를 균등 분산
        foreach (GameObject e in enemies)
        {
            IDamageable dmg = e.GetComponent<IDamageable>();
            if (dmg != null) { dmg.ApplyHit(share); continue; }
            Character ch = e.GetComponent<Character>();
            if (ch != null && !ch.Hit(share)) e.SetActive(false);
        }
        return 0f; // 본인 무피해
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) Destroy(this);
    }
}
