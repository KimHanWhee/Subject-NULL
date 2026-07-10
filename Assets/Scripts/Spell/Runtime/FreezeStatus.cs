using UnityEngine;

// ♣ 빙결 상태이상 — 적 컨트롤러(이동+공격 AI)를 일시 비활성화해 완전 정지. 만료 시 원복.
// SlowStatus와 동일한 풀링 안전 규약: OnDisable(사망/반환) 시 원복 + 컴포넌트 제거.
public class FreezeStatus : MonoBehaviour
{
    private Behaviour[] frozen; // 껐던 컨트롤러들(원래 켜져 있던 것만)
    private Animator anim;      // 걷기 등 애니메이션도 함께 정지(완전 정지 연출)
    private float animSpeed0 = 1f;
    private float remain;

    public static void Apply(GameObject enemy, float duration)
    {
        FreezeStatus s = enemy.GetComponent<FreezeStatus>();
        if (s == null)
        {
            s = enemy.AddComponent<FreezeStatus>();
            if (!s.Bind()) { Destroy(s); return; } // 컨트롤러 없는 대상 무시
        }
        s.remain = Mathf.Max(s.remain, duration);
    }

    bool Bind()
    {
        var list = new System.Collections.Generic.List<Behaviour>();
        EnemyController ec = GetComponent<EnemyController>();
        RangedEnemyController rc = GetComponent<RangedEnemyController>();
        if (ec != null && ec.enabled) { ec.enabled = false; list.Add(ec); }
        if (rc != null && rc.enabled) { rc.enabled = false; list.Add(rc); }
        frozen = list.ToArray();
        if (frozen.Length == 0) return false;

        // 애니메이션 정지(걷기 모션 등) — 만료/사망 시 원래 속도로 복구
        anim = GetComponent<Animator>();
        if (anim != null)
        {
            animSpeed0 = anim.speed;
            anim.speed = 0f;
        }
        return true;
    }

    void Update()
    {
        remain -= Time.deltaTime; // 게임 시간 기준
        if (remain <= 0f) { Restore(); Destroy(this); }
    }

    void Restore()
    {
        if (frozen == null) return;
        foreach (Behaviour b in frozen)
            if (b != null) b.enabled = true;
        frozen = null;
        if (anim != null) anim.speed = animSpeed0;
        anim = null;
    }

    void OnDisable() { Restore(); Destroy(this); }
}
