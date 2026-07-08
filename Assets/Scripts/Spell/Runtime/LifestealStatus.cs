using UnityEngine;

// ♥ Lifesteal 상태 — 지속시간 동안 총알이 적에게 명중할 때마다 체력 회복(기본 1).
// Bullet이 NotifyBulletHit(static)으로 통지(플레이어 1인 전제).
public class LifestealStatus : MonoBehaviour
{
    private static LifestealStatus active; // 현재 활성 인스턴스(플레이어 1인)

    private Character ch;
    private float healPerHit;
    private float remain;

    public static void Apply(GameObject player, float healPerHit, float duration)
    {
        LifestealStatus s = player.GetComponent<LifestealStatus>();
        if (s == null)
        {
            s = player.AddComponent<LifestealStatus>();
            s.ch = player.GetComponent<Character>();
        }
        s.healPerHit = healPerHit;
        s.remain = Mathf.Max(s.remain, duration);
        active = s;
    }

    // Bullet → 적 명중 시 호출. 활성 상태 없으면 무시.
    public static void NotifyBulletHit(float damage)
    {
        if (active != null && active.ch != null) active.ch.Heal(active.healPerHit);
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) Destroy(this);
    }

    void OnDisable() { if (active == this) active = null; }
    void OnDestroy() { if (active == this) active = null; }
}
