using UnityEngine;

// ♦ Slow Aura — 지속시간 동안 플레이어 주변 반경 내의 "시간"을 늦춘다.
// 적 이동/돌진(개구리)/공격 타이머(localTimeScale)와 적 총알 속도를 함께 감속 → 범위 안이 슬로우모션처럼 보임.
// 범위를 벗어나면 즉시 원복. 소유는 SlowAura가 단독(localTimeScale는 이 효과 전용).
public class SlowAuraStatus : MonoBehaviour
{
    private float radius;
    private float slowFactor;      // 남는 시간 비율(0.1 = 90% 감속)
    private float remain;
    private float nextScan;

    private const float ScanInterval = 0.08f;

    public static void Apply(GameObject player, float radius, float slowFactor, float duration)
    {
        SlowAuraStatus s = player.GetComponent<SlowAuraStatus>();
        if (s == null) s = player.AddComponent<SlowAuraStatus>();
        s.radius = radius;
        s.slowFactor = Mathf.Clamp01(slowFactor);
        s.remain = Mathf.Max(s.remain, duration);
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) { ResetAll(); Destroy(this); return; }

        if (Time.time < nextScan) return;
        nextScan = Time.time + ScanInterval;
        Scan();
    }

    void Scan()
    {
        Vector2 center = transform.position;
        float r2 = radius * radius;

        foreach (EnemyBase e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
        {
            if (e == null || !e.gameObject.activeInHierarchy) continue;
            bool inRange = ((Vector2)e.transform.position - center).sqrMagnitude <= r2;
            e.localTimeScale = inRange ? slowFactor : 1f;
        }
        foreach (EnemyBullet b in Object.FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None))
        {
            if (b == null || !b.gameObject.activeInHierarchy) continue;
            bool inRange = ((Vector2)b.transform.position - center).sqrMagnitude <= r2;
            b.localTimeScale = inRange ? slowFactor : 1f;
        }
    }

    void ResetAll()
    {
        foreach (EnemyBase e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            if (e != null) e.localTimeScale = 1f;
        foreach (EnemyBullet b in Object.FindObjectsByType<EnemyBullet>(FindObjectsSortMode.None))
            if (b != null) b.localTimeScale = 1f;
    }

    void OnDisable() { ResetAll(); }
}
