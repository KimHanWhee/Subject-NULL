using UnityEngine;

// ♣ Confusion 상태 — 적 하나가 지속시간 동안 랜덤 방향으로 이동(AI 무시).
// 컨트롤러를 끄고 자체 이동으로 대체. 풀링 안전: OnDisable 시 복구+제거.
public class ConfusionStatus : MonoBehaviour
{
    private Behaviour[] disabled;   // 껐던 컨트롤러(원래 켜져 있던 것만)
    private EnemyBase enemy;         // 이동 속도를 매 프레임 라이브로 읽음(SlowStatus 등 감속 반영)
    private float moveSpeed;         // 폴백(enemy 없을 때)
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
        // 모든 적 타입 공통 처리 — 새 몬스터는 EnemyBase 상속만으로 자동 호환
        EnemyBase e = GetComponent<EnemyBase>();
        if (e != null && e.enabled) { enemy = e; moveSpeed = e.speed; e.enabled = false; list.Add(e); }
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
        float sp = enemy != null ? enemy.speed : moveSpeed; // 현재 속도(감속 반영)
        transform.Translate(dir * (sp * Time.deltaTime));
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
