using UnityEngine;

// ♠ Black Hole 런타임 — 2단계 구조.
//   1) 흡입: duration 동안 반경 내 적을 중심으로 끌어당긴다(피해 없음).
//   2) 붕괴: 시간이 끝나면 폭발해 반경 내 적을 소멸시킨다(대미지는 EnemyBase.ApplyHit 경유 →
//      사망 연출·점수·콤보가 정상 처리된다).
// 인력은 transform 직접 이동(컨트롤러 이동과 합산). 모든 타이밍은 게임 시간 기준이라
// 타임스톱/슬로우와 함께 느려진다.
public class BlackHole : MonoBehaviour
{
    private float radius;
    private float pullSpeed;
    private float explosionDamage;
    private float endTime;
    private float startTime;
    private float nextPulse;

    private static readonly Color color = new Color(0.45f, 0.15f, 0.8f, 1f);      // 어두운 보라(흡입)
    private static readonly Color burstColor = new Color(0.85f, 0.45f, 1f, 1f);   // 밝은 보라(붕괴)

    public static BlackHole Spawn(Vector2 pos, float radius, float pullSpeed, float explosionDamage, float duration)
    {
        GameObject go = new GameObject("BlackHole");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        BlackHole b = go.AddComponent<BlackHole>();
        b.radius = radius;
        b.pullSpeed = pullSpeed;
        b.explosionDamage = explosionDamage;
        b.startTime = Time.time;
        b.endTime = Time.time + duration;
        b.nextPulse = Time.unscaledTime;
        SpellParticleVfx.SpawnImplode(pos, radius, color, duration); // 지속 동안 중심으로 빨려드는 입자
        SpellVfx.SpawnRing(pos, radius, color, duration, 0.1f);      // 영향 범위 표시
        return b;
    }

    void Update()
    {
        if (Time.time >= endTime) { Detonate(); return; }

        // 시각: 중심으로 수렴하는 링. 붕괴가 가까울수록 빨라져 폭발을 예고한다.
        if (Time.unscaledTime >= nextPulse)
        {
            SpellVfx.SpawnConverge(transform.position, radius, color, 0.7f);
            float k = Mathf.InverseLerp(startTime, endTime, Time.time); // 0 → 1
            nextPulse = Time.unscaledTime + Mathf.Lerp(0.5f, 0.12f, k);
        }

        // 인력: 후반으로 갈수록 강해져 중심에 모인 채로 폭발을 맞게 한다
        float pullK = Mathf.Lerp(1f, 2.2f, Mathf.InverseLerp(startTime, endTime, Time.time));
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy")) continue;
            Transform et = hits[i].transform;
            Vector2 to = (Vector2)transform.position - (Vector2)et.position;
            float dist = to.magnitude;
            if (dist > 0.15f)
            {
                float step = Mathf.Min(pullSpeed * pullK * Time.deltaTime, dist);
                et.position += (Vector3)(to / dist * step);
            }
        }
    }

    // 붕괴 — 반경 내 적 소멸 + 폭발 연출
    void Detonate()
    {
        SpellParticleVfx.SpawnBurst(transform.position, radius * 1.15f, burstColor, 48, 0.6f);
        SpellVfx.SpawnRing(transform.position, radius, burstColor, 0.35f, 0.22f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy")) continue;

            IDamageable dmg = hits[i].GetComponent<IDamageable>();
            if (dmg != null) { dmg.ApplyHit(explosionDamage); continue; }
            Character ch = hits[i].GetComponent<Character>();
            if (ch != null && !ch.Hit(explosionDamage)) hits[i].gameObject.SetActive(false);
        }

        Destroy(gameObject);
    }
}
