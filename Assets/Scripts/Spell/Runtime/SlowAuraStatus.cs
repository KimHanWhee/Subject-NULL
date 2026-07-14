using UnityEngine;

// ♦ 감속 장막 — 지속시간 동안 플레이어 주변 반경 내 적들의 이동 속도를 감소.
// 범위 내 적에게 SlowStatus를 짧게 갱신 부착 → 범위를 벗어나면 곧 원복(SlowStatus가 speed 복구를 안전 처리).
public class SlowAuraStatus : MonoBehaviour
{
    private float radius;
    private float slowFactor;      // 남는 속도 비율(0.1 = 90% 감소)
    private float remain;
    private float nextScan;

    private const float RefreshWindow = 0.35f; // 범위 이탈 시 이 시간 뒤 원복
    private const float ScanInterval = 0.1f;

    public static void Apply(GameObject player, float radius, float slowFactor, float duration)
    {
        SlowAuraStatus s = player.GetComponent<SlowAuraStatus>();
        if (s == null) s = player.AddComponent<SlowAuraStatus>();
        s.radius = radius;
        s.slowFactor = Mathf.Clamp01(slowFactor);
        s.remain = Mathf.Max(s.remain, duration); // 재적용은 지속 갱신
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) { Destroy(this); return; }

        if (Time.time < nextScan) return;
        nextScan = Time.time + ScanInterval;

        Vector2 center = transform.position;
        foreach (EnemyBase e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
        {
            if (e == null || !e.gameObject.activeInHierarchy) continue;
            if (Vector2.Distance(e.transform.position, center) <= radius)
                SlowStatus.Apply(e.gameObject, slowFactor, RefreshWindow);
        }
    }
}
