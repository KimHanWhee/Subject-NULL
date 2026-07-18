using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 메인 메뉴 버튼 호버 연출 — 흰 테두리 글로우 + 살짝 확대 + 호버 사운드.
// MainMenuIntro가 각 버튼에 런타임으로 부착(씬 배선 불필요). Init()으로 사운드 주입.
public class MenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    RectTransform rt;
    Image glow;                 // 버튼 테두리 흰 발광(첫 자식 — 버튼 그래픽 앞, 라벨 뒤)
    Vector3 baseScale;
    bool hover;
    float t;                    // 0=평상 → 1=호버
    AudioClip hoverClip;
    bool inited;

    const float HoverScale = 1.06f;
    const float Speed = 12f;    // unscaled 보간 속도

    static Sprite glowSprite;

    public void Init(AudioClip clip)
    {
        hoverClip = clip;
        if (inited) return;
        inited = true;

        rt = (RectTransform)transform;
        baseScale = rt.localScale;

        // 흰 테두리 글로우: 버튼보다 살짝 크게, 첫 자식(부모 이미지 위·라벨 아래)에 깔림.
        // 링 SDF라 중앙은 투명 → 라벨을 가리지 않고 가장자리만 빛난다.
        GameObject g = new GameObject("HoverGlow", typeof(RectTransform));
        RectTransform grt = g.GetComponent<RectTransform>();
        grt.SetParent(rt, false);
        grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.5f);
        grt.pivot = new Vector2(0.5f, 0.5f);
        grt.sizeDelta = rt.sizeDelta + new Vector2(40f, 40f);
        grt.anchoredPosition = Vector2.zero;
        grt.SetAsFirstSibling();
        glow = g.AddComponent<Image>();
        glow.sprite = GlowSprite();
        glow.type = Image.Type.Sliced;
        glow.raycastTarget = false;
        glow.color = new Color(1f, 1f, 1f, 0f);
    }

    void Update()
    {
        if (!inited) return;
        float target = hover ? 1f : 0f;
        t = Mathf.MoveTowards(t, target, Speed * Time.unscaledDeltaTime);
        if (glow != null) glow.color = new Color(1f, 1f, 1f, t * 0.9f);
        rt.localScale = Vector3.Lerp(baseScale, baseScale * HoverScale, t);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        hover = true;
        if (hoverClip != null) Sfx.Play2D(hoverClip, 0.35f);
    }

    public void OnPointerExit(PointerEventData e)
    {
        hover = false;
    }

    // 라운드 사각 테두리 발광 스프라이트 — 경계 부근에서 밝고 안/밖으로 부드럽게 감쇠.
    // 9-slice(border 30)라 어떤 버튼 크기에도 테두리 두께가 유지된다.
    static Sprite GlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        const int size = 96;
        const float radius = 26f;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float half = size / 2f;
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius), 0f);
                float d = Mathf.Sqrt(dx * dx + dy * dy) - radius; // 라운드 사각 경계에서 0
                float glowv = Mathf.Clamp01(1f - Mathf.Abs(d) / 12f);
                px[y * size + x] = new Color(1f, 1f, 1f, glowv * glowv);
            }
        tex.SetPixels(px);
        tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                                   100f, 0, SpriteMeshType.FullRect, new Vector4(30f, 30f, 30f, 30f));
        return glowSprite;
    }
}
