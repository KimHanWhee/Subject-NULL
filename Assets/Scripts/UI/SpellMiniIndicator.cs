using UnityEngine;
using UnityEngine.UI;

// 하단 중앙 "▲ Ctrl" 힌트 위의 미니 인디케이터 — 벨트가 숨어 있어도 준비된 마블 5개를
// 작은 구슬(슈트×등급 스프라이트, SpellHandHUD.ResolveIcon 재사용 = 단일 진실원)로 표시.
// 빈 슬롯은 어두운 원이 리필 진행도에 따라 서서히 밝아진다.
// CtrlHint(CanvasGroup)의 자식이라 Ctrl 선택 중엔 힌트와 함께 페이드아웃.
public class SpellMiniIndicator : MonoBehaviour
{
    public SpellHandHUD hud;     // caster/skinTable 접근용
    public float iconSize = 34f;
    public float spacing = 40f;

    [Header("Refill Anim — 1단: 흰 점 수렴(시전 텔레그래프 컨셉) → 2단: 마블 팝 생성")]
    public float convergeDuration = 0.25f;                   // 흰 점이 모이는 시간(초, unscaled)
    public int convergeDotCount = 8;                         // 수렴 점 개수
    public float convergeRadius = 30f;                       // 수렴 시작 반경(px)
    public float convergeDotSize = 9f;                       // 수렴 점 크기(px)
    public float spawnDuration = 0.3f;                       // 팝 길이(초, unscaled)
    [Range(0.05f, 1f)] public float spawnStartScale = 0.25f; // 시작 스케일

    private Image[] icons;
    private Image[] flashes;      // 마블 생성 순간 흰색 플래시 오버레이(아이콘 자식)
    private Image[][] dots;       // [슬롯][k] 수렴용 흰 점(아이콘 자식, 평소 꺼짐)
    private bool[] filledPrev;    // 빈→채움 전이 감지
    private float[] convergeStart; // 수렴 시작 시각(unscaled), <0=비활성
    private float[] spawnStart;   // 팝 시작 시각(unscaled), <0=비활성
    private static Sprite dotSprite; // 원형 스프라이트(빈 슬롯/수렴 점 공용, 런타임 생성)

    void Start()
    {
        int n = hud != null ? hud.SlotCount : 5;
        if (n <= 0) n = 5;
        icons = new Image[n];
        flashes = new Image[n];
        dots = new Image[n][];
        filledPrev = new bool[n];
        convergeStart = new float[n];
        spawnStart = new float[n];
        float x0 = -(n - 1) * spacing * 0.5f;
        for (int i = 0; i < n; i++)
        {
            convergeStart[i] = -1f;
            spawnStart[i] = -1f;

            GameObject go = new GameObject("Mini" + i, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.sizeDelta = new Vector2(iconSize, iconSize);
            rt.anchoredPosition = new Vector2(x0 + spacing * i, 0f);
            Image img = go.AddComponent<Image>();
            img.raycastTarget = false;
            icons[i] = img;

            // 플래시 오버레이: 아이콘과 동일 위치·크기(자식이라 팝 스케일도 함께 적용)
            GameObject fgo = new GameObject("Flash", typeof(RectTransform));
            RectTransform frt = fgo.GetComponent<RectTransform>();
            frt.SetParent(rt, false);
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            Image fimg = fgo.AddComponent<Image>();
            fimg.raycastTarget = false;
            fimg.enabled = false;
            flashes[i] = fimg;

            // 수렴용 흰 점들: 아이콘 자식으로 미리 만들어 두고 평소엔 꺼둠
            dots[i] = new Image[Mathf.Max(1, convergeDotCount)];
            for (int k = 0; k < dots[i].Length; k++)
            {
                GameObject dgo = new GameObject("Dot" + k, typeof(RectTransform));
                RectTransform drt = dgo.GetComponent<RectTransform>();
                drt.SetParent(rt, false);
                drt.sizeDelta = new Vector2(convergeDotSize, convergeDotSize);
                Image dimg = dgo.AddComponent<Image>();
                dimg.raycastTarget = false;
                dimg.sprite = DotSprite();
                dimg.enabled = false;
                dots[i][k] = dimg;
            }
        }
    }

    void Update()
    {
        if (icons == null || hud == null || hud.caster == null || hud.caster.Slots == null) return;
        var slots = hud.caster.Slots;
        for (int i = 0; i < icons.Length; i++)
        {
            if (icons[i] == null) continue;
            SpellMarble m = i < slots.Count ? slots[i] : null;
            Sprite s = m != null ? hud.ResolveIcon(m) : null;
            if (s != null)
            {
                if (!filledPrev[i]) StartConverge(i); // 리필 순간 — 1단: 흰 점 수렴부터

                if (convergeStart[i] >= 0f)
                {
                    float p = (Time.unscaledTime - convergeStart[i]) / Mathf.Max(0.0001f, convergeDuration);
                    if (p < 1f)
                    {
                        ShowEmpty(i); // 수렴이 끝나기 전엔 마블을 아직 보여주지 않음
                        TickConverge(i, p);
                    }
                    else
                    {
                        EndConverge(i);
                        icons[i].sprite = s;
                        icons[i].color = Color.white;
                        StartSpawn(i); // 2단: 마블 팝 생성 + 플래시
                    }
                }
                else
                {
                    icons[i].sprite = s;
                    icons[i].color = Color.white;
                }
            }
            else
            {
                EndConverge(i); // 수렴 도중 소비된 경우 취소
                ShowEmpty(i);
                icons[i].rectTransform.localScale = Vector3.one;
                if (flashes[i] != null) flashes[i].enabled = false;
                spawnStart[i] = -1f;
            }
            filledPrev[i] = (m != null);

            if (spawnStart[i] >= 0f) TickSpawn(i);
        }
    }

    void ShowEmpty(int i)
    {
        icons[i].sprite = DotSprite();
        float a = Mathf.Lerp(0.15f, 0.45f, hud.caster.RefillProgress(i)); // 리필 임박할수록 밝게
        icons[i].color = new Color(1f, 1f, 1f, a);
    }

    // 1단: 아이콘 둘레의 흰 점들이 중심으로 빨려드는 수렴 연출(시전 텔레그래프와 동일 컨셉)
    void StartConverge(int i)
    {
        convergeStart[i] = Time.unscaledTime;
        icons[i].rectTransform.localScale = Vector3.one;
        for (int k = 0; k < dots[i].Length; k++)
            if (dots[i][k] != null) dots[i][k].enabled = true;
        TickConverge(i, 0f);
    }

    void TickConverge(int i, float p)
    {
        float e = p * p; // easeInQuad — 점점 빨라지며 중심으로 빨려듦
        float r = convergeRadius * (1f - e);
        for (int k = 0; k < dots[i].Length; k++)
        {
            Image d = dots[i][k];
            if (d == null) continue;
            float ang = (Mathf.PI * 2f * k) / dots[i].Length;
            d.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
            d.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.35f, 1f, p)); // 모일수록 선명
        }
    }

    void EndConverge(int i)
    {
        if (convergeStart[i] < 0f) return;
        convergeStart[i] = -1f;
        for (int k = 0; k < dots[i].Length; k++)
            if (dots[i][k] != null) dots[i][k].enabled = false;
    }

    void StartSpawn(int i)
    {
        spawnStart[i] = Time.unscaledTime;
        icons[i].rectTransform.localScale = Vector3.one * spawnStartScale;
        if (flashes[i] != null)
        {
            flashes[i].sprite = icons[i].sprite; // 마블 모양 그대로 하얗게
            flashes[i].enabled = true;
            flashes[i].color = Color.white;
        }
    }

    void TickSpawn(int i)
    {
        float p = (Time.unscaledTime - spawnStart[i]) / Mathf.Max(0.0001f, spawnDuration);
        bool done = p >= 1f;
        p = Mathf.Clamp01(p);

        // easeOutBack — 살짝 오버슈트하며 톡 튀어나와 리필이 눈에 띔
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float e = 1f + c3 * Mathf.Pow(p - 1f, 3f) + c1 * Mathf.Pow(p - 1f, 2f);
        icons[i].rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(spawnStartScale, 1f, e);

        if (flashes[i] != null)
        {
            Color c = flashes[i].color; c.a = 1f - p; flashes[i].color = c;
            if (done) flashes[i].enabled = false;
        }
        if (done)
        {
            icons[i].rectTransform.localScale = Vector3.one;
            spawnStart[i] = -1f;
        }
    }

    // 빈 슬롯 표시용 부드러운 원(외부 에셋 불필요 — SpellVfx.ringSprite와 같은 런타임 생성 방식)
    static Sprite DotSprite()
    {
        if (dotSprite != null) return dotSprite;
        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
        Color[] px = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                float aa = Mathf.Clamp01((1f - d) * 6f); // 가장자리만 살짝 안티앨리어싱
                px[y * size + x] = new Color(1f, 1f, 1f, aa);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        dotSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return dotSprite;
    }
}
