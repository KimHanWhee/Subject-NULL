using UnityEngine;

// ♦ 전자기장 상태 — 지속시간 동안 플레이어 주변 반경에 들어온 적 원거리 공격을 무력화.
//  · 적 총알: 반경 진입 즉시 스파크 폭발과 함께 제거(풀 반환)
//  · 레이저: LaserEnemyController.BeamRange가 ClipBeam을 호출해 장막 경계에서 빔이 끊긴 것처럼 보임
public class EMFieldStatus : MonoBehaviour, IBuffDisplay
{
    // 레이저 클리핑용 활성 인스턴스(플레이어 1인 전제 — PlayerBulletEvents와 동일 규약)
    private static EMFieldStatus active;

    private float radius;
    private float remain;
    private SpellMarble marble;
    private static readonly Color sparkColor = new Color(0.5f, 0.9f, 1f, 1f); // 전기 청백

    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return true; } }
    public float BuffRemaining { get { return remain; } }
    public int BuffCharges { get { return 0; } }

    public static void Apply(GameObject player, float radius, float duration, SpellMarble marble = null)
    {
        EMFieldStatus s = player.GetComponent<EMFieldStatus>();
        if (s == null)
        {
            s = player.AddComponent<EMFieldStatus>();
            active = s;
            Transform anchor = SpellVfx.VisualAnchor(player);
            SpellVfx.SpawnAura(anchor, radius, sparkColor, duration, 0.08f);      // 장막 외곽선
            SpellParticleVfx.SpawnOrbit(anchor, radius, sparkColor, duration);    // 전기 입자 궤도
        }
        s.radius = radius;
        s.marble = marble;
        s.remain = Mathf.Max(s.remain, duration);
    }

    // 레이저 빔 사거리 클리핑 — 빔이 장막에 닿으면 경계 진입점까지로 줄인다.
    // 반환: 클리핑된 사거리(장막과 무관하면 range 그대로)
    public static float ClipBeam(Vector2 origin, Vector2 dir, float range)
    {
        if (active == null) return range;
        Vector2 c = active.transform.position;

        // 광선-원 교차(진입점) — |O + tD - C|² = r²
        Vector2 oc = origin - c;
        float b = Vector2.Dot(oc, dir);
        float det = b * b - (oc.sqrMagnitude - active.radius * active.radius);
        if (det < 0f) return range;             // 교차 없음
        float t = -b - Mathf.Sqrt(det);         // 가까운 교차점(진입)
        if (t < 0f || t > range) return range;  // 뒤쪽이거나 사거리 밖

        // 차단 스파크 — 경계에 막히는 연출
        SpellParticleVfx.SpawnBurst(origin + dir * t, 0.4f, sparkColor, 8, 0.25f);
        return t;
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) { Destroy(this); return; }

        // 반경 내 적 총알 제거 — 스파크와 함께 소멸(풀 반환)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("EnemyBullet")) continue;
            if (!hits[i].gameObject.activeSelf) continue;
            SpellParticleVfx.SpawnBurst(hits[i].transform.position, 0.35f, sparkColor, 8, 0.25f);
            hits[i].gameObject.SetActive(false); // EnemyBullet 풀 반환 규약
        }
    }

    void OnDisable()
    {
        if (active == this) active = null;
    }
}
