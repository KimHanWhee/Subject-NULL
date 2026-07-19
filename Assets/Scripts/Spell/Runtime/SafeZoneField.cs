using System.Collections.Generic;
using UnityEngine;

// ♦ 안전지대 — 드롭한 위치에 보호 구역 생성.
//  · 구역 안의 플레이어는 받는 피해 전부 무효(SafeZoneField.Protects가 판정, PlayerController 훅)
//  · 적은 경계 밖으로 밀려나 진입 불가(부드럽게 밀어냄 — 순간이동 없음)
public class SafeZoneField : MonoBehaviour
{
    private static readonly List<SafeZoneField> zones = new List<SafeZoneField>();

    private float radius;
    private float endTime;
    private float pushSpeed = 6f; // 내부 적을 밖으로 밀어내는 속도
    private static readonly Color zoneColor = new Color(0.35f, 0.95f, 0.7f, 1f); // 안전 청록

    // 해당 위치가 활성 안전지대 안인가 — PlayerController.TakeHit에서 피해 무효 판정
    public static bool Protects(Vector2 pos)
    {
        for (int i = 0; i < zones.Count; i++)
            if (zones[i] != null && Vector2.Distance(pos, zones[i].transform.position) <= zones[i].radius)
                return true;
        return false;
    }

    public static SafeZoneField Spawn(Vector2 pos, float radius, float duration)
    {
        GameObject go = new GameObject("SafeZone");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        SafeZoneField f = go.AddComponent<SafeZoneField>();
        f.radius = radius;
        f.endTime = Time.time + duration;

        SpellVfx.SpawnRing(pos, radius, zoneColor, duration, 0.1f);        // 구역 외곽선
        SpellParticleVfx.SpawnField(pos, radius, zoneColor, duration);     // 은은한 보호 입자
        return f;
    }

    void OnEnable() { zones.Add(this); }
    void OnDisable() { zones.Remove(this); }

    void Update()
    {
        if (Time.time >= endTime) { Destroy(gameObject); return; }

        // 적 진입 차단 — 경계 안쪽의 적을 바깥 방향으로 밀어낸다
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy")) continue;
            Transform et = hits[i].transform;
            Vector2 from = (Vector2)et.position - (Vector2)transform.position;
            float dist = from.magnitude;
            Vector2 dir = dist > 0.001f ? from / dist : Random.insideUnitCircle.normalized;
            // 경계까지 남은 거리만큼, pushSpeed 상한으로 부드럽게
            float need = radius - dist + 0.05f;
            float step = Mathf.Min(need, pushSpeed * Time.deltaTime);
            et.position += (Vector3)(dir * step);
        }
    }
}
