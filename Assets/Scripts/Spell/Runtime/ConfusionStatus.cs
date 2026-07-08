using UnityEngine;

// ♣ Confusion 상태 — 적 하나가 지속시간 동안 랜덤 방향으로 이동(AI 무시).
// 컨트롤러를 끄고 자체 이동으로 대체. 풀링 안전: OnDisable 시 복구+제거.
public class ConfusionStatus : MonoBehaviour
{
    private Behaviour[] disabled;   // 껐던 컨트롤러(원래 켜져 있던 것만)
    private float moveSpeed;
    private float remain;
    private Vector2 dir;
    private float nextTurn;

    public static void Apply(GameObject enemy, float duration)
    {
        ConfusionStatus s = enemy.GetComponent<ConfusionStatus>();
        if (s == null)
        {
            s = enemy.AddComponent<ConfusionStatus>();
            if (!s.Bind()) { Destroy(s); return; }
        }
        s.remain = Mathf.Max(s.remain, duration);
    }

    bool Bind()
    {
        var list = new System.Collections.Generic.List<Behaviour>();
        EnemyController ec = GetComponent<EnemyController>();
        RangedEnemyController rc = GetComponent<RangedEnemyController>();
        if (ec != null && ec.enabled) { moveSpeed = ec.speed; ec.enabled = false; list.Add(ec); }
        if (rc != null && rc.enabled) { moveSpeed = rc.speed; rc.enabled = false; list.Add(rc); }
        disabled = list.ToArray();
        if (disabled.Length == 0) return false;
        PickDirection();
        return true;
    }

    void PickDirection()
    {
        dir = Random.insideUnitCircle.normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
        nextTurn = Time.time + Random.Range(0.4f, 0.8f); // 랜덤 주기로 방향 전환
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) { Restore(); Destroy(this); return; }

        if (Time.time >= nextTurn) PickDirection();
        transform.Translate(dir * (moveSpeed * Time.deltaTime));
    }

    void Restore()
    {
        if (disabled == null) return;
        foreach (Behaviour b in disabled)
            if (b != null) b.enabled = true;
        disabled = null;
    }

    void OnDisable() { Restore(); Destroy(this); }
}
