using UnityEngine;

// Design Ref: §8.5 — 코드 기반 임시 VFX. 런타임 생성한 링 스프라이트를 SpriteRenderer로 그려
// 확장·페이드(공격)와 대상 추종 오라(버프/실드)를 표현. 정렬만 맞으면 확실히 보임.
// 실제 아트가 생기면 각 Ability의 effectPrefab 경로로 자연스럽게 교체.
public class SpellVfx : MonoBehaviour
{
    enum Mode { Ring, Aura, Converge }

    private SpriteRenderer sr;
    private Mode mode;
    private float duration;
    private float startTime;
    private float startRadius;
    private float endRadius;
    private Color color;
    private Transform follow;      // Aura/Converge: 대상 추종(SelfBuff는 플레이어)
    private bool rainbow;          // Converge: 레전드 등급 무지개 색상 순환

    private static Sprite ringSprite;

    // 셀프버프 VFX 기준점. 플레이어 스프라이트는 상단 여백이 커서 transform 원점이 몸통보다 위에 있음 —
    // 비-트리거 콜라이더 중심(몸통 위치)에 앵커 자식을 만들어 캐싱하고, VFX 추종/부착 기준으로 쓴다.
    public static Transform VisualAnchor(GameObject owner)
    {
        if (owner == null) return null;
        Transform anchor = owner.transform.Find("SpellVfxAnchor");
        if (anchor == null)
        {
            anchor = new GameObject("SpellVfxAnchor").transform;
            anchor.SetParent(owner.transform, false);
            Collider2D[] cols = owner.GetComponents<Collider2D>();
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i].isTrigger) continue;
                anchor.position = cols[i].bounds.center;
                break;
            }
        }
        return anchor;
    }

    // 드롭 위치에서 확장하며 사라지는 링 — 공격/폭발 느낌 (lineWidth는 호환용, 미사용)
    public static SpellVfx SpawnRing(Vector2 pos, float radius, Color color, float duration, float lineWidth = 0.12f)
    {
        SpellVfx fx = Create(color);
        fx.transform.position = new Vector3(pos.x, pos.y, 0f);
        fx.mode = Mode.Ring;
        fx.startRadius = Mathf.Max(0.05f, radius * 0.25f);
        fx.endRadius = Mathf.Max(0.1f, radius);
        fx.duration = Mathf.Max(0.05f, duration);
        fx.Begin();
        return fx;
    }

    // 대상을 따라다니는 오라 링 — 버프/실드 느낌. duration 동안 맥동 후 페이드 (lineWidth 미사용)
    public static SpellVfx SpawnAura(Transform target, float radius, Color color, float duration, float lineWidth = 0.1f)
    {
        SpellVfx fx = Create(color);
        fx.mode = Mode.Aura;
        fx.follow = target;
        fx.startRadius = Mathf.Max(0.1f, radius);
        fx.endRadius = fx.startRadius;
        fx.duration = Mathf.Max(0.05f, duration);
        if (target != null) fx.transform.position = target.position;
        fx.Begin();
        return fx;
    }

    // 시전 텔레그래프: startRadius에서 중심으로 수렴하며 사라지는 고리. 등급색(레전드는 무지개 순환).
    // follow != null이면 대상(SelfBuff=플레이어)을 추종, null이면 드롭 위치에 고정(Targeted).
    public static SpellVfx SpawnConverge(Vector2 pos, float startRadius, Color color, float duration, bool rainbow = false, Transform follow = null)
    {
        SpellVfx fx = Create(color);
        fx.mode = Mode.Converge;
        fx.rainbow = rainbow;
        fx.follow = follow;
        fx.startRadius = Mathf.Max(0.1f, startRadius);
        fx.endRadius = 0.08f;
        fx.duration = Mathf.Max(0.05f, duration);
        fx.transform.position = follow != null
            ? new Vector3(follow.position.x, follow.position.y, 0f)
            : new Vector3(pos.x, pos.y, 0f);
        fx.Begin();
        return fx;
    }

    static SpellVfx Create(Color color)
    {
        GameObject go = new GameObject("SpellVfx");
        SpellVfx fx = go.AddComponent<SpellVfx>();
        fx.color = color;
        return fx;
    }

    void Awake()
    {
        EnsureRenderer();
    }

    // SpriteRenderer 1회 준비. Awake 미실행 순서(에디터/스폰 직후)에도 안전.
    void EnsureRenderer()
    {
        if (sr != null) return;
        sr = gameObject.GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = RingSprite();

        // 프로젝트의 '최상단' Sorting Layer에 + 매우 높은 order → 어떤 월드 스프라이트에도 안 가려짐
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0)
            sr.sortingLayerID = layers[layers.Length - 1].id;
        sr.sortingOrder = 32000;
    }

    void Begin()
    {
        EnsureRenderer();
        startTime = Time.unscaledTime;
        sr.color = color;
        SetDiameter(startRadius * 2f);
    }

    void Update()
    {
        float p = (Time.unscaledTime - startTime) / duration; // 슬로우와 무관하게 실시간 재생
        if (p >= 1f) { Destroy(gameObject); return; }

        if (rainbow) // 레전드: Hue 순환. 알파는 각 모드의 SetAlpha가 관리.
        {
            Color rc = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.9f, 1f), 0.85f, 1f);
            color = new Color(rc.r, rc.g, rc.b, color.a);
        }

        if (mode == Mode.Ring)
        {
            float ease = 1f - (1f - p) * (1f - p);            // easeOut 확장
            SetDiameter(Mathf.Lerp(startRadius, endRadius, ease) * 2f);
            SetAlpha(1f - p);                                 // 점점 투명
        }
        else if (mode == Mode.Converge)                        // 시전 텔레그래프: 중심으로 수렴
        {
            if (follow != null) transform.position = new Vector3(follow.position.x, follow.position.y, 0f);
            float ease = p * p;                               // easeIn(끝으로 갈수록 빠르게 수렴)
            SetDiameter(Mathf.Lerp(startRadius, endRadius, ease) * 2f);
            transform.Rotate(0f, 0f, 180f * Time.unscaledDeltaTime); // 살짝 회전 → 시전 느낌
            SetAlpha(p < 0.85f ? 1f : Mathf.InverseLerp(1f, 0.85f, p)); // 끝 15% 페이드
        }
        else // Aura
        {
            if (follow != null) transform.position = follow.position;
            float pulse = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 12f);
            SetDiameter(startRadius * 2f * pulse);
            SetAlpha(p > 0.8f ? Mathf.InverseLerp(1f, 0.8f, p) : 1f); // 끝 20%만 페이드
        }
    }

    void SetDiameter(float d) => transform.localScale = new Vector3(d, d, 1f);

    void SetAlpha(float a)
    {
        Color c = color; c.a = Mathf.Clamp01(a);
        if (sr != null) sr.color = c;
    }

    // 흰색 링(annulus) 텍스처를 1회 생성 → 스프라이트(지름 1 world unit). localScale로 크기 조절.
    // public: TimeStopWave 등 외부 파동 연출도 같은 링을 재사용(단일 진실원).
    public static Sprite RingSprite()
    {
        if (ringSprite != null) return ringSprite;

        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float c = (size - 1) * 0.5f;
        float outer = size * 0.48f;   // 바깥 반지름(px)
        float inner = size * 0.34f;   // 안쪽 반지름(px) → 링 두께 = outer-inner
        float mid = (outer + inner) * 0.5f;
        float half = (outer - inner) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = 1f - Mathf.Clamp01(Mathf.Abs(d - mid) / half); // 링 중심에서 멀수록 투명
                a = Mathf.SmoothStep(0f, 1f, a);                          // 가장자리 부드럽게
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return ringSprite;
    }
}
