using UnityEngine;
using UnityEngine.UI;

// 대시 v2 UI: 플레이어 스태미너를 채워지는 Image(fillAmount)로 표시
//
// 게이지 이미지 하나만 있으면 다 소모했을 때 화면에서 완전히 사라져,
// 얼마나 회복됐는지 가늠할 기준이 없어진다. 그래서 뒤에 트랙(빈 틀)을 깔아준다.
// 트랙은 런타임 생성 — 씬 배선 없이 동작한다(프로젝트 UI 관례).
public class StaminaBar : MonoBehaviour
{
    [Tooltip("스태미너를 읽어올 플레이어. 비우면 'Player' 태그로 자동 탐색")]
    public PlayerController player;

    [Tooltip("Image Type=Filled 로 설정된 게이지 이미지")]
    public Image fillImage;

    [Header("색상 (선택)")]
    public bool useColorFeedback = true;
    public Color readyColor = new Color(0.30f, 0.75f, 1f);   // 대시 가능(가득)
    public Color lowColor = new Color(1f, 0.55f, 0.15f);     // 부족

    [Header("트랙(배경)")]
    public bool autoCreateTrack = true;
    // 여백이 크면 게이지가 프레임 안을 못 채워 양 끝에 어두운 배경이 드러난다.
    // 테두리가 게이지를 얇게 감싸는 정도로만 둔다.
    [Tooltip("트랙이 게이지보다 얼마나 여유를 갖는지(px)")]
    public Vector2 trackPadding = new Vector2(2f, 4f);
    public Color trackColor = new Color(0.06f, 0.10f, 0.13f, 0.85f);

    [Tooltip("게이지 스프라이트를 코드로 생성 — 프레임 안쪽에 딱 맞는 둥근 막대")]
    public bool squareFill = true;

    [Tooltip("게이지 양 끝 둥글기(높이 대비 비율). 0=각짐, 0.5=캡슐")]
    [Range(0f, 0.5f)] public float fillRoundness = 0.42f;

    [Header("대시 기준선")]
    [Tooltip("대시 1회에 필요한 스태미너 지점을 세로선으로 표시")]
    public bool showDashMark = true;
    public Color dashMarkColor = Color.white;
    public float dashMarkWidth = 2f;

    RectTransform dashMark;

    Sprite fillSprite;   // 게이지 크기에 맞춰 생성 — 늘어나서 모서리가 찌그러지는 걸 막는다

    void Awake()
    {
        // fillImage 미지정 시 자기 자신에서 탐색
        if (fillImage == null)
            fillImage = GetComponent<Image>();

        // player 미지정 시 태그로 자동 탐색
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.GetComponent<PlayerController>();
        }

        // 순서 주의: 트랙이 게이지 스프라이트를 폴백으로 참조하므로 트랙을 먼저 만든다.
        if (autoCreateTrack) BuildTrack();
        if (squareFill) MakeFillSquare();
        if (showDashMark) BuildDashMark();
    }

    // 대시 1회에 필요한 스태미너 지점에 세로선을 세운다.
    // 이 선을 넘겨야 대시가 나가므로, 회복 중 "언제 다시 쓸 수 있는지"를 눈으로 알 수 있다.
    // 게이지 위에 얹어야 채워진 구간에서도 보인다.
    void BuildDashMark()
    {
        if (fillImage == null || player == null) return;
        if (player.maxStamina <= 0f) return;

        RectTransform fill = fillImage.rectTransform;
        if (fill.parent == null) return;
        if (fill.parent.Find("DashMark") != null) return;

        GameObject go = new GameObject("DashMark", typeof(RectTransform));
        dashMark = go.GetComponent<RectTransform>();
        dashMark.SetParent(fill.parent, false);
        dashMark.anchorMin = fill.anchorMin;
        dashMark.anchorMax = fill.anchorMax;
        dashMark.pivot = fill.pivot;

        float ratio = Mathf.Clamp01(player.dashStaminaCost / player.maxStamina);
        Vector2 size = fill.sizeDelta;
        // 게이지는 좌→우로 차므로, 왼쪽 끝에서 ratio만큼 떨어진 위치
        float x = fill.anchoredPosition.x - size.x * 0.5f + size.x * ratio;
        dashMark.anchoredPosition = new Vector2(x, fill.anchoredPosition.y);
        dashMark.sizeDelta = new Vector2(dashMarkWidth, size.y);

        Image img = go.AddComponent<Image>();
        img.color = dashMarkColor;
        img.raycastTarget = false;

        // 게이지보다 뒤 형제 = 위에 그려짐. 채워진 구간에서도 선이 보여야 한다.
        dashMark.SetSiblingIndex(fill.GetSiblingIndex() + 1);
    }

    // 게이지 스프라이트를 게이지 "실제 크기"로 만든다.
    //
    // 기본 UISprite는 둥글기가 크기와 안 맞아 트랙이 비쳤다. 그렇다고 작은 둥근 스프라이트를
    // 늘려 쓰면(32x32 → 620x22) 모서리가 길쭉하게 찌그러진다.
    // 실제 픽셀 크기로 그려 1:1로 쓰면 둥글기가 의도대로 유지된다.
    void MakeFillSquare()
    {
        if (fillImage == null) return;

        Vector2 size = fillImage.rectTransform.sizeDelta;
        int w = Mathf.Max(8, Mathf.RoundToInt(size.x));
        int h = Mathf.Max(4, Mathf.RoundToInt(size.y));
        int r = Mathf.RoundToInt(h * fillRoundness);

        fillSprite = RoundedBar(w, h, r);
        fillImage.sprite = fillSprite;
        fillImage.type = Image.Type.Filled;          // 채움 방식은 유지
        fillImage.fillMethod = Image.FillMethod.Horizontal;
    }

    // 양 끝이 둥근 막대. 프레임 안쪽 마감과 결을 맞춘다.
    static Sprite RoundedBar(int w, int h, int r)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool cut = false;
                if (r > 0)
                {
                    int dx = (x < r) ? (r - x) : (x >= w - r ? x - (w - 1 - r) : 0);
                    int dy = (y < r) ? (r - y) : (y >= h - r ? y - (h - 1 - r) : 0);
                    if (dx > 0 && dy > 0 && dx * dx + dy * dy > r * r) cut = true;
                }
                px[y * w + x] = cut ? Color.clear : Color.white;
            }
        t.SetPixels(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    // 프레임 안쪽 경계까지의 두께를 잰다.
    // 스프라이트 중앙에서 바깥으로 훑어 밝은 테두리를 처음 만나는 지점이 빈 내부의 끝이다.
    // (어두운 픽셀을 세는 방식은 브래킷 내부의 어두운 색까지 잡혀서 틀린 값이 나온다)
    static Vector2 MeasureFrameInset(Sprite s)
    {
        if (s == null || s.texture == null || !s.texture.isReadable) return Vector2.zero;
        Texture2D tex = s.texture;
        int W = tex.width, H = tex.height, cx = W / 2, cy = H / 2;

        int left = 0, bottom = 0;
        for (int x = cx; x >= 0; x--)
            if (Bright(tex.GetPixel(x, cy))) { left = x + 1; break; }
        for (int y = cy; y >= 0; y--)
            if (Bright(tex.GetPixel(cx, y))) { bottom = y + 1; break; }

        // 9슬라이스 테두리는 pixelsPerUnit에 반비례해 렌더된다.
        // ppu를 올려 프레임을 얇게 만들었다면 여백도 같은 비율로 줄여야 맞물린다.
        float scale = 100f / Mathf.Max(1f, s.pixelsPerUnit);
        return new Vector2(left * scale, bottom * scale);
    }

    static bool Bright(Color c)
    {
        return c.a > 0.35f && (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f) > 0.28f;
    }


    // 게이지 "뒤"에 빈 틀을 만든다.
    // 게이지와 같은 부모의 앞 형제로 넣어야 게이지에 가려지지 않고 뒤에 깔린다.
    void BuildTrack()
    {
        if (fillImage == null) return;
        RectTransform fill = fillImage.rectTransform;
        if (fill.parent == null) return;
        if (fill.parent.Find("StaminaTrack") != null) return; // 중복 생성 방지

        GameObject go = new GameObject("StaminaTrack", typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(fill.parent, false);
        rt.anchorMin = fill.anchorMin;
        rt.anchorMax = fill.anchorMax;
        rt.pivot = fill.pivot;
        rt.anchoredPosition = fill.anchoredPosition;
        rt.sizeDelta = fill.sizeDelta + trackPadding * 2f;
        rt.SetSiblingIndex(fill.GetSiblingIndex()); // 게이지 바로 앞 = 뒤에 그려짐

        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;

        // 픽셀아트 트랙 에셋이 있으면 쓰고, 없으면 게이지와 같은 스프라이트를 어둡게 깐다.
        Sprite art = Resources.Load<Sprite>("UI/StaminaTrack");
        if (art != null)
        {
            img.sprite = art;
            img.type = Image.Type.Sliced;
            img.color = Color.white;   // 도트 원본 색 유지

            // 여백은 트랙 스프라이트의 "빈 내부"와 맞춰야 한다.
            // ⚠️ border를 그대로 쓰면 안 된다 — border는 늘리지 않을 가장자리 두께일 뿐이라
            //    프레임 두께와 다르다. 실제로 border(22,8)를 썼더니 가로는 넘치고
            //    세로는 모자라 사방이 어긋났다.
            // 스프라이트에서 프레임 안쪽 경계를 직접 재어 쓴다(에셋을 바꿔도 자동으로 맞음).
            Vector2 inset = MeasureFrameInset(art);
            if (inset.x > 0f || inset.y > 0f)
            {
                trackPadding = inset;
                rt.sizeDelta = fill.sizeDelta + trackPadding * 2f;
            }
        }
        else
        {
            img.sprite = fillImage.sprite;
            img.type = Image.Type.Sliced;
            img.color = trackColor;
        }
    }

    void Update()
    {
        if (player == null || fillImage == null) return;

        float ratio = player.StaminaRatio;
        fillImage.fillAmount = ratio;

        if (useColorFeedback)
            fillImage.color = Color.Lerp(lowColor, readyColor, ratio);
    }
}
