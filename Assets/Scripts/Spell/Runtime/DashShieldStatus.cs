using UnityEngine;

// ♦ Dash Shield 상태 — 지속시간 동안 대시 무적 판정 시간(dashGrace) 연장(기본 2배 체감).
// dashGrace를 (dashDuration + 원래 grace)만큼 추가 → 대시 무적창이 약 2배.
public class DashShieldStatus : MonoBehaviour
{
    private PlayerController pc;
    private float originalGrace;
    private float remain;

    public static void Apply(GameObject player, float duration)
    {
        DashShieldStatus s = player.GetComponent<DashShieldStatus>();
        if (s == null)
        {
            s = player.AddComponent<DashShieldStatus>();
            if (!s.Bind()) { Destroy(s); return; }
        }
        s.remain = Mathf.Max(s.remain, duration);
    }

    bool Bind()
    {
        pc = GetComponent<PlayerController>();
        if (pc == null) return false;
        originalGrace = pc.dashGrace;
        pc.dashGrace = originalGrace + pc.dashDuration + originalGrace; // 무적창 ≈ 2배
        return true;
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) { Restore(); Destroy(this); }
    }

    void Restore()
    {
        if (pc != null) pc.dashGrace = originalGrace;
        pc = null;
    }

    void OnDisable() { Restore(); }
}
