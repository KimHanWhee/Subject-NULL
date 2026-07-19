using UnityEngine;

// ♠ Railgun 빔 연출 — 코드 생성 번개 궤적(에셋 불필요).
// 흰 코어 + 청백 글로우 두 겹의 LineRenderer, 중심선에 수직 지터로 번개 느낌.
// 짧게 번쩍이고 페이드아웃. unscaled 시간(타임스톱 중에도 연출은 보이게).
public class RailBeamVfx : MonoBehaviour
{
    private LineRenderer core;
    private LineRenderer glow;
    private float life = 0.28f;
    private float age;
    private static readonly Color coreColor = new Color(1f, 1f, 1f, 1f);
    private static readonly Color glowColor = new Color(0.5f, 0.85f, 1f, 0.8f);

    private SpriteRenderer beamSprite; // PixelLab 빔 스프라이트(Resources/VFX/RailBeam) — 없으면 라인만
    private float beamBaseAlpha = 0.95f;

    public static void Spawn(Vector2 origin, Vector2 dir, float length, float width)
    {
        GameObject go = new GameObject("RailBeam");
        go.transform.position = origin;
        RailBeamVfx v = go.AddComponent<RailBeamVfx>();

        // 본체: PixelLab 빔 스프라이트를 길이에 맞춰 늘림(화려한 코어) + 지터 라인 2겹(전기 느낌)
        Sprite s = Resources.Load<Sprite>("VFX/RailBeam");
        if (s != null)
        {
            GameObject sgo = new GameObject("BeamSprite");
            sgo.transform.SetParent(go.transform, false);
            v.beamSprite = sgo.AddComponent<SpriteRenderer>();
            v.beamSprite.sprite = s;
            SortingLayer[] layers = SortingLayer.layers;
            if (layers != null && layers.Length > 0) v.beamSprite.sortingLayerID = layers[layers.Length - 1].id;
            v.beamSprite.sortingOrder = 30990; // 라인(31000) 바로 아래
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            sgo.transform.position = origin + dir * (length * 0.5f);
            sgo.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            float wUnits = s.rect.width / s.pixelsPerUnit;
            float hUnits = s.rect.height / s.pixelsPerUnit;
            sgo.transform.localScale = new Vector3(length / wUnits, (width * 2.4f) / hUnits, 1f);
            v.beamSprite.color = new Color(1f, 1f, 1f, v.beamBaseAlpha);
        }

        v.glow = v.MakeLine(origin, dir, length, width * 1.6f, glowColor, jitter: 0.16f, segments: 14);
        v.core = v.MakeLine(origin, dir, length, width * 0.45f, coreColor, jitter: 0.1f, segments: 14);

        // 총구 스파크 + 빔 끝 착탄 버스트
        SpellParticleVfx.SpawnBurst(origin, 0.6f, glowColor, 16, 0.3f);
        SpellParticleVfx.SpawnBurst(origin + dir * length, 0.9f, glowColor, 22, 0.4f);
    }

    LineRenderer MakeLine(Vector2 origin, Vector2 dir, float length, float width, Color color, float jitter, int segments)
    {
        GameObject go = new GameObject("Line");
        go.transform.SetParent(transform, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = segments;
        lr.useWorldSpace = true;
        // 빌드 셰이더 스트리핑 대응 — Always Included 목록에 있는 것만 사용(JokerSpell 규약)
        Shader sh = Shader.Find("Sprites/Default");
        lr.material = new Material(sh != null ? sh : Shader.Find("Unlit/Transparent"));
        lr.startWidth = width;
        lr.endWidth = width * 0.6f;
        lr.startColor = color;
        lr.endColor = color;
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) lr.sortingLayerID = layers[layers.Length - 1].id;
        lr.sortingOrder = 31000;

        Vector2 normal = new Vector2(-dir.y, dir.x);
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            // 양 끝은 지터 없이 고정 — 총구/착탄점이 어긋나 보이지 않게
            float amp = (i == 0 || i == segments - 1) ? 0f : jitter * Mathf.Sin(t * Mathf.PI);
            Vector2 p = origin + dir * (length * t) + normal * Random.Range(-amp, amp);
            lr.SetPosition(i, p);
        }
        return lr;
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        float a = Mathf.Clamp01(1f - age / life);
        if (core != null) SetAlpha(core, a);
        if (glow != null) SetAlpha(glow, a * 0.8f);
        if (beamSprite != null)
        {
            Color c = beamSprite.color; c.a = beamBaseAlpha * a;
            beamSprite.color = c;
        }
        if (age >= life) Destroy(gameObject);
    }

    static void SetAlpha(LineRenderer lr, float a)
    {
        Color s = lr.startColor; s.a = a; lr.startColor = s;
        Color e = lr.endColor; e.a = a * 0.6f; lr.endColor = e;
    }
}
