using UnityEngine;

// ♠ Flame Field 런타임 — 드롭 위치에 일정 시간 지속되는 불꽃 장판. 주기적으로 범위 내 적에게 피해.
// 피해 틱은 게임 시간(scaled) 기준 — 선택 슬로우 중엔 장판도 함께 느려짐(의도).
public class FlameField : MonoBehaviour
{
    private float radius;
    private float damagePerTick;
    private float tickInterval;
    private float endTime;
    private float nextTick;
    private float nextPulse;
    private Color color = new Color(1f, 0.45f, 0.1f, 1f);

    public static FlameField Spawn(Vector2 pos, float radius, float dps, float tickInterval, float duration)
    {
        GameObject go = new GameObject("FlameField");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        FlameField f = go.AddComponent<FlameField>();
        f.radius = radius;
        f.tickInterval = Mathf.Max(0.1f, tickInterval);
        f.damagePerTick = dps * f.tickInterval;
        f.endTime = Time.time + duration;
        f.nextTick = Time.time;   // 즉시 1틱
        f.nextPulse = Time.unscaledTime;
        return f;
    }

    void Update()
    {
        if (Time.time >= endTime) { Destroy(gameObject); return; }

        // 시각: 주기적 불꽃 링 펄스(실시간 재생)
        if (Time.unscaledTime >= nextPulse)
        {
            SpellVfx.SpawnRing(transform.position, radius, color, 0.6f);
            nextPulse = Time.unscaledTime + 0.45f;
        }

        // 피해 틱(게임 시간)
        if (Time.time >= nextTick)
        {
            nextTick = Time.time + tickInterval;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (!hits[i].CompareTag("Enemy")) continue;
                IDamageable dmg = hits[i].GetComponent<IDamageable>();
                if (dmg != null) { dmg.ApplyHit(damagePerTick); continue; }
                Character ch = hits[i].GetComponent<Character>();
                if (ch != null && !ch.Hit(damagePerTick)) hits[i].gameObject.SetActive(false);
            }
        }
    }
}
