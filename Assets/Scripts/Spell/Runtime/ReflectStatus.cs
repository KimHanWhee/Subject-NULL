using UnityEngine;

// ♦ Reflect 상태 — 지속시간 동안 받는 피해를 공격자에게 반사(피해는 그대로 받음).
// 총알 피격 등 공격자 불명이면 가장 가까운 적에게 반사.
public class ReflectStatus : MonoBehaviour, IPlayerDamageModifier
{
    private float remain;
    private float searchRadius = 6f; // 공격자 불명 시 반사 대상 탐색 반경

    public static void Apply(GameObject player, float duration)
    {
        ReflectStatus s = player.GetComponent<ReflectStatus>();
        if (s == null) s = player.AddComponent<ReflectStatus>();
        s.remain = Mathf.Max(s.remain, duration);
    }

    public float ModifyIncomingDamage(float damage, GameObject attacker)
    {
        GameObject target = attacker != null ? attacker : FindNearestEnemy();
        if (target != null)
        {
            IDamageable dmg = target.GetComponent<IDamageable>();
            if (dmg != null) dmg.ApplyHit(damage);
            else
            {
                Character ch = target.GetComponent<Character>();
                if (ch != null && !ch.Hit(damage)) target.SetActive(false);
            }
        }
        return damage; // 반사해도 본인 피해는 그대로
    }

    GameObject FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, searchRadius);
        GameObject best = null;
        float bestSq = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy")) continue;
            float sq = ((Vector2)hits[i].transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = hits[i].gameObject; }
        }
        return best;
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) Destroy(this);
    }
}
