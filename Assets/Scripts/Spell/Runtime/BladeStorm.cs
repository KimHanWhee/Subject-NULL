using System.Collections.Generic;
using UnityEngine;

// ♠ Blade Storm 런타임 — 플레이어 주변을 도는 칼날들. 닿는 적에게 피해(적별 재타격 쿨다운).
// 회전/피해는 게임 시간(scaled) 기준. 시각은 코드 생성 스프라이트(최상단 정렬 — SpellVfx 선례).
public class BladeStorm : MonoBehaviour
{
    private Transform owner;
    private float orbitRadius;
    private float damage;
    private float hitRadius;
    private float rehitCooldown;
    private float endTime;
    private float angularSpeed; // deg/sec
    private float angle;
    private Transform[] blades;
    private readonly Dictionary<int, float> lastHit = new Dictionary<int, float>(); // 적 instanceID → 마지막 타격 시각

    private static Sprite bladeSprite;

    public static BladeStorm Spawn(Transform owner, int bladeCount, float orbitRadius, float damage, float duration, float angularSpeed)
    {
        GameObject go = new GameObject("BladeStorm");
        go.transform.position = owner.position;
        BladeStorm b = go.AddComponent<BladeStorm>();
        b.owner = owner;
        b.orbitRadius = orbitRadius;
        b.damage = damage;
        b.hitRadius = 0.35f;
        b.rehitCooldown = 0.4f;
        b.endTime = Time.time + duration;
        b.angularSpeed = angularSpeed;
        b.BuildBlades(Mathf.Max(1, bladeCount));
        return b;
    }

    void BuildBlades(int count)
    {
        blades = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Blade");
            go.transform.SetParent(transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BladeSprite();
            sr.color = new Color(0.9f, 0.95f, 1f, 0.95f); // 은백색 칼날
            SortingLayer[] layers = SortingLayer.layers;
            if (layers != null && layers.Length > 0) sr.sortingLayerID = layers[layers.Length - 1].id;
            sr.sortingOrder = 32000;
            go.transform.localScale = Vector3.one * 0.5f;
            blades[i] = go.transform;
        }
    }

    void Update()
    {
        if (owner == null || Time.time >= endTime) { Destroy(gameObject); return; }

        transform.position = owner.position;                 // 플레이어 추종
        angle += angularSpeed * Time.deltaTime;              // 게임 시간 기준 회전

        for (int i = 0; i < blades.Length; i++)
        {
            float a = (angle + 360f * i / blades.Length) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * orbitRadius;
            blades[i].position = transform.position + offset;

            // 칼날 위치에서 적 타격(적별 재타격 쿨다운으로 다단히트 방지)
            Collider2D[] hits = Physics2D.OverlapCircleAll(blades[i].position, hitRadius);
            for (int h = 0; h < hits.Length; h++)
            {
                if (!hits[h].CompareTag("Enemy")) continue;
                int id = hits[h].gameObject.GetInstanceID();
                float last;
                if (lastHit.TryGetValue(id, out last) && Time.time - last < rehitCooldown) continue;
                lastHit[id] = Time.time;

                // 타격 스파크(재타격 쿨다운으로 빈도 제한됨 → 부담 없음)
                SpellParticleVfx.SpawnBurst(hits[h].transform.position, 0.35f, new Color(0.9f, 0.95f, 1f, 1f), 6, 0.18f);

                IDamageable dmg = hits[h].GetComponent<IDamageable>();
                if (dmg != null) { dmg.ApplyHit(damage); continue; }
                Character ch = hits[h].GetComponent<Character>();
                if (ch != null && !ch.Hit(damage)) hits[h].gameObject.SetActive(false);
            }
        }
    }

    // 작은 타원(칼날 느낌) 스프라이트 1회 생성
    static Sprite BladeSprite()
    {
        if (bladeSprite != null) return bladeSprite;
        const int size = 48;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float cx = (size - 1) * 0.5f, cy = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / (size * 0.5f);        // 가로로 김
                float dy = (y - cy) / (size * 0.22f);       // 세로로 얇음
                float d = dx * dx + dy * dy;
                float aVal = Mathf.Clamp01(1f - d);
                aVal = Mathf.SmoothStep(0f, 1f, aVal);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, aVal));
            }
        }
        tex.Apply();
        bladeSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return bladeSprite;
    }
}
