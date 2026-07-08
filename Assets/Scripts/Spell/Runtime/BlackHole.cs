using UnityEngine;

// ♠ Black Hole 런타임 — 드롭 위치에 블랙홀. 반경 내 적을 중심으로 끌어당기며 주기 피해.
// 인력은 transform 직접 이동(컨트롤러 이동과 합산). 피해/인력 모두 게임 시간 기준.
public class BlackHole : MonoBehaviour
{
    private float radius;
    private float pullSpeed;
    private float damagePerTick;
    private float tickInterval;
    private float endTime;
    private float nextTick;
    private float nextPulse;
    private static readonly Color color = new Color(0.45f, 0.15f, 0.8f, 1f); // 어두운 보라

    public static BlackHole Spawn(Vector2 pos, float radius, float pullSpeed, float dps, float tickInterval, float duration)
    {
        GameObject go = new GameObject("BlackHole");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        BlackHole b = go.AddComponent<BlackHole>();
        b.radius = radius;
        b.pullSpeed = pullSpeed;
        b.tickInterval = Mathf.Max(0.1f, tickInterval);
        b.damagePerTick = dps * b.tickInterval;
        b.endTime = Time.time + duration;
        b.nextTick = Time.time + b.tickInterval;
        b.nextPulse = Time.unscaledTime;
        return b;
    }

    void Update()
    {
        if (Time.time >= endTime) { Destroy(gameObject); return; }

        // 시각: 중심으로 수렴하는 보라 링(빨려드는 느낌)
        if (Time.unscaledTime >= nextPulse)
        {
            SpellVfx.SpawnConverge(transform.position, radius, color, 0.7f);
            nextPulse = Time.unscaledTime + 0.5f;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        bool tick = Time.time >= nextTick;
        if (tick) nextTick = Time.time + tickInterval;

        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy")) continue;
            Transform et = hits[i].transform;

            // 인력: 중심 방향으로 끌어당김(중심 근처는 정지)
            Vector2 to = (Vector2)transform.position - (Vector2)et.position;
            float dist = to.magnitude;
            if (dist > 0.15f)
            {
                float step = Mathf.Min(pullSpeed * Time.deltaTime, dist);
                et.position += (Vector3)(to / dist * step);
            }

            if (tick)
            {
                IDamageable dmg = hits[i].GetComponent<IDamageable>();
                if (dmg != null) { dmg.ApplyHit(damagePerTick); continue; }
                Character ch = hits[i].GetComponent<Character>();
                if (ch != null && !ch.Hit(damagePerTick)) hits[i].gameObject.SetActive(false);
            }
        }
    }
}
