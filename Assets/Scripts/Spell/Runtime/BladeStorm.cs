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
    private static bool usingArtSprite; // true면 Resources 도트 에셋 사용(틴트 없이 원색)

    // 도트 칼날(클레이모어)은 손잡이에서 칼끝으로 +43.9° 방향을 향해 그려져 있다
    // (밝은 날 픽셀 무게중심 − 어두운 손잡이 픽셀 무게중심으로 측정).
    // 손잡이가 플레이어 쪽, 칼끝이 바깥을 향하는 "회전 방벽" 배치를 만들 때 이만큼 빼 준다.
    // ⚠️ 스프라이트를 교체하면 이 값도 다시 재야 한다.
    // (폴백 타원은 방향성이 없어 회전시키지 않는다)
    const float SpriteAxisOffset = 43.9f;

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
            // 도트 에셋은 자체 색을 살리고(흰색 틴트), 폴백 타원만 은백색으로 칠한다.
            sr.color = usingArtSprite ? Color.white : new Color(0.9f, 0.95f, 1f, 0.95f);
            SortingLayer[] layers = SortingLayer.layers;
            if (layers != null && layers.Length > 0) sr.sortingLayerID = layers[layers.Length - 1].id;
            sr.sortingOrder = 32000;
            // 보이는 크기 = 실제 판정 크기. 도트 칼날은 hitRadius(지름 기준)에 맞춰 스케일을 역산한다.
            float scale = 0.5f;
            if (usingArtSprite && sr.sprite != null)
            {
                float spriteWorld = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
                if (spriteWorld > 0.001f) scale = (hitRadius * 2f) / spriteWorld;
            }
            go.transform.localScale = Vector3.one * scale;
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

            // 날이 바깥을 향하도록 공전각에 맞춰 회전 — 고정 방향으로 돌면 칼날 형태가 어색하다
            if (usingArtSprite)
            {
                float deg = (angle + 360f * i / blades.Length) - SpriteAxisOffset;
                blades[i].rotation = Quaternion.Euler(0f, 0f, deg);
            }

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

    // 칼날 스프라이트 — Resources의 도트 에셋 우선, 없으면 코드 생성 타원으로 폴백.
    static Sprite BladeSprite()
    {
        if (bladeSprite != null) return bladeSprite;

        bladeSprite = Resources.Load<Sprite>("VFX/BladeStormBlade");
        if (bladeSprite != null) { usingArtSprite = true; return bladeSprite; }

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
