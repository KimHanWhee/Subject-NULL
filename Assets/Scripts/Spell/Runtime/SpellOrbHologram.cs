using UnityEngine;
using UnityEngine.UI;

// Shift(선택 모드) 중 각 구슬 위에 능력 아이콘을 홀로그램으로 띄운다.
//
// 왜 구슬에 새기지 않는가:
//   벨트에 꽂힌 구슬은 "슈트 문양만 새겨진 상태"가 설정이다. 구슬 위에 능력 아이콘을 겹치면
//   그 설정이 깨진다. 그래서 구슬은 손대지 않고, 식별 정보는 별도 레이어(연구소 시스템의
//   판독 결과)로 띄운다. 캐릭터가 보는 것과 플레이어가 보는 것을 분리하는 구성.
//
// 호버하면 그 슬롯의 아이콘은 사라지고 같은 자리에 기존 툴팁(이름·등급·설명)이 뜬다.
// 아이콘 홀로그램이 상세로 "확장"되는 것처럼 보이게 하려는 것이라,
// 위치 기준(HologramGap)을 SpellDragHandler와 공유한다.
//
// 씬 배선 불필요 — 런타임 생성(프로젝트의 UI 생성 관례).
public class SpellOrbHologram : MonoBehaviour
{
    public SpellSelectionUI selection;
    public SpellHandHUD hud;
    public SpellCaster caster;
    public SpellDragHandler drag;

    [Header("모양")]
    // ⚠️ 슬롯 간격(현재 40.1px)보다 크면 이웃 홀로그램과 겹친다.
    //    Build()에서 실제 간격을 재어 자동으로 줄이므로 여기 값은 상한으로만 쓰인다.
    public float panelSize = 54f;        // 홀로그램 한 변(px) 상한
    public float panelGapRatio = 0.88f;  // 슬롯 간격 대비 패널 폭(1.0=딱 붙음)
    public float iconInset = 7f;         // 패널 안쪽 여백(테두리·브래킷을 피해 아이콘 배치)
    public float fadeSpeed = 12f;        // 나타나고 사라지는 속도(unscaled)

    static readonly Color HoloFill = new Color(0.35f, 0.85f, 1f, 0.16f);
    static readonly Color HoloEdge = new Color(0.45f, 0.92f, 1f, 0.75f);
    // 아이콘 틴트. Image.color는 곱셈이라 하늘색을 진하게 주면 원래 색이 죽는다.
    // 흰색에 가깝게 두되 파랑만 살짝 남겨, 원색을 보여주면서 홀로그램 기운만 얹는다.
    // (주사선·깜빡임이 이미 "투사된 상" 느낌을 만들고 있어 틴트는 약해도 된다)
    [Header("색감")]
    public Color iconTint = new Color(0.88f, 0.97f, 1f, 0.97f);

    const float ScanScrollSpeed = 0.35f;   // 주사선이 흐르는 속도(uv/초)
    const float ScanTiling = 6f;           // 패널당 주사선 반복 횟수

    // 툴팁(SpellDragHandler.TooltipSortingOrder)보다 낮아야 툴팁이 가려지지 않는다.
    const int HologramSortingOrder = 300;

    float curPanelSize;      // 이번 프레임에 적용할 패널 폭(슬롯 간격에서 매번 산출)

    RectTransform[] panels;
    Image[] icons;
    RawImage[] scans;
    CanvasGroup[] groups;
    static Sprite boxSprite;
    static Texture2D scanTex;

    // 씬 배선 없이 자동 부착(MapBounds·MainMenuLocalizer와 같은 관례).
    // 벨트가 있는 씬에만 붙으면 되므로 SpellHandHUD 존재 여부로 판단한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => TryAttach();
        TryAttach();
    }

    static void TryAttach()
    {
        if (FindFirstObjectByType<SpellOrbHologram>() != null) return;
        SpellHandHUD hud = FindFirstObjectByType<SpellHandHUD>();
        if (hud == null) return;
        hud.gameObject.AddComponent<SpellOrbHologram>();
    }

    void Awake()
    {
        if (selection == null) selection = FindFirstObjectByType<SpellSelectionUI>();
        if (hud == null) hud = FindFirstObjectByType<SpellHandHUD>();
        if (caster == null) caster = FindFirstObjectByType<SpellCaster>();
        if (drag == null) drag = FindFirstObjectByType<SpellDragHandler>();
    }

    void Update()
    {
        if (hud == null || caster == null || selection == null) return;
        if (panels == null) { Build(); if (panels == null) return; }

        bool selecting = selection.IsSelecting;
        int hovered = drag != null ? drag.HoveredSlot : -1;

        // 패널 폭을 매 프레임 다시 계산한다.
        // Build() 시점에 한 번만 재면, 그때 벨트 레이아웃이 아직 확정되지 않은 경우
        // 측정에 실패해 상한값(54)이 그대로 남고 이웃과 겹친다. 실제로 그렇게 배포됐다.
        float spacing = MeasureSlotSpacing(panels.Length);
        curPanelSize = spacing > 1f ? Mathf.Min(panelSize, spacing * panelGapRatio) : panelSize;

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == null) continue;

            SpellMarble m = i < caster.Slots.Count ? caster.Slots[i] : null;
            Sprite ic = m != null ? m.icon : null;

            // 보일 조건: 선택 모드 + 마블 있음 + 아이콘 있음 + 지금 호버 중이 아님
            //   (호버 중인 슬롯은 툴팁이 같은 자리를 차지한다)
            // 나머지 슬롯은 툴팁과 겹치더라도 그대로 보여준다 — 툴팁이 앞에 그려진다.
            bool show = selecting && m != null && ic != null && i != hovered;

            CanvasGroup g = groups[i];
            // 미세한 깜빡임 — 완전히 안정된 밝기면 UI 판때기로 보이고, 흔들려야 투사처럼 보인다.
            float flicker = show ? 0.90f + 0.10f * Mathf.Sin(Time.unscaledTime * 9f + i * 1.7f) : 0f;
            g.alpha = Mathf.MoveTowards(g.alpha, flicker, fadeSpeed * Time.unscaledDeltaTime);
            if (g.alpha <= 0.001f) { panels[i].gameObject.SetActive(false); continue; }
            bool wasHidden = !panels[i].gameObject.activeSelf;
            panels[i].gameObject.SetActive(true);

            // 홀로그램도 툴팁과 같은 함정을 탄다 — 비활성일 때 켠 overrideSorting은 꺼진다.
            // 활성화된 직후에 다시 설정해야 정렬 순서가 실제로 적용된다.
            if (wasHidden)
            {
                Canvas hc = panels[i].GetComponent<Canvas>();
                if (hc != null) { hc.overrideSorting = true; hc.sortingOrder = HologramSortingOrder; }
            }

            if (ic != null && icons[i].sprite != ic) icons[i].sprite = ic;

            // 주사선을 위로 흘려 스캔되는 인상을 준다(unscaled — 슬로우와 무관하게 일정 속도).
            if (scans[i] != null)
                scans[i].uvRect = new Rect(0f, Time.unscaledTime * ScanScrollSpeed, 1f, ScanTiling);

            PlaceAboveSlot(i);
        }
    }

    // 구슬 위쪽에 붙인다. 벨트는 Shift에서 위로 올라오고 확대되므로 매 프레임 따라가야 한다.
    void PlaceAboveSlot(int i)
    {
        RectTransform slot = hud.GetSlotRect(i);
        if (slot == null) { panels[i].gameObject.SetActive(false); return; }

        // 폭을 매 프레임 반영 — 해상도/전체화면 전환으로 벨트 간격이 바뀌어도 겹치지 않는다.
        if (curPanelSize > 1f) panels[i].sizeDelta = new Vector2(curPanelSize, curPanelSize);

        Vector3[] c = new Vector3[4];
        slot.GetWorldCorners(c);
        float topY = Mathf.Max(c[1].y, c[2].y);
        float midX = (c[0].x + c[3].x) * 0.5f;

        // 벨트가 확대되면 홀로그램도 같은 배율로 커져야 어색하지 않다.
        //
        // ⚠️ lossyScale을 그대로 쓰면 안 된다. 그 값에는 CanvasScaler 배율이 이미 들어 있는데,
        //    홀로그램의 부모(캔버스 루트)도 같은 배율을 갖고 있어 곱이 두 번 적용된다.
        //    기준 해상도(1920x1080)에서는 캔버스 배율이 1이라 드러나지 않다가,
        //    전체 화면에서 배율이 1.5가 되면 2.25배로 커져 이웃과 겹친다.
        //    부모 배율로 나눠 "벨트 자체의 확대분"만 남긴다.
        float worldScale = slot.lossyScale.y;                                    // 캔버스 배율 포함(월드 기준)
        float parentScale = panels[i].parent != null ? panels[i].parent.lossyScale.y : 1f;
        panels[i].localScale = Vector3.one * (worldScale / Mathf.Max(0.0001f, parentScale));

        // 위치는 월드 좌표이므로 여기서는 캔버스 배율이 포함된 worldScale을 써야 한다.
        // (localScale과 서로 다른 배율을 쓰는 게 맞다 — 좌표계가 다르다)
        float halfH = (curPanelSize > 1f ? curPanelSize : panelSize) * 0.5f * worldScale;
        panels[i].position = new Vector3(midX, topY + halfH + SpellDragHandler.HologramGap * worldScale, panels[i].position.z);
    }

    void Build()
    {
        int n = hud.SlotCount;
        if (n <= 0) return;

        // 벨트와 같은 캔버스에 올려야 정렬·스케일이 일관된다.
        RectTransform parent = hud.GetSlotRect(0);
        Canvas canvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
        if (canvas == null) return;
        RectTransform root = canvas.GetComponent<RectTransform>();

        panels = new RectTransform[n];
        icons = new Image[n];
        scans = new RawImage[n];
        groups = new CanvasGroup[n];

        for (int i = 0; i < n; i++)
        {
            GameObject go = new GameObject("OrbHologram" + i, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(root, false);
            rt.sizeDelta = new Vector2(panelSize, panelSize);

            Image bg = go.AddComponent<Image>();
            bg.sprite = BoxSprite();
            bg.type = Image.Type.Sliced;
            // 도트 패널은 자체 색을 살리고(흰색 틴트), 폴백 사각형만 하늘색으로 칠한다.
            bg.color = usingArtPanel ? Color.white : HoloFill;
            bg.raycastTarget = false;   // 클릭을 가로채면 드래그가 막힌다

            // 폴백일 때만 테두리를 따로 그린다 — 도트 패널은 테두리가 이미 그려져 있다.
            if (!usingArtPanel)
            {
                GameObject eg = new GameObject("Edge", typeof(RectTransform));
                RectTransform ert = eg.GetComponent<RectTransform>();
                ert.SetParent(rt, false);
                Stretch(ert);
                Image edge = eg.AddComponent<Image>();
                edge.sprite = BoxSprite();
                edge.type = Image.Type.Sliced;
                edge.color = HoloEdge;
                edge.raycastTarget = false;
                edge.fillCenter = false;
            }

            // 주사선 + 노이즈. 아이콘보다 "먼저" 만들어 아래에 깔리게 한다.
            // 위에 덮으면 아이콘 색이 한 겹 더 흐려져서 틴트를 밝게 해도 색이 안 산다.
            // 배경 위에서만 흐르게 두면 홀로그램 느낌은 유지되면서 아이콘은 또렷해진다.
            // RawImage를 쓰는 이유: uvRect를 굴려 주사선을 흐르게 하려면 Image로는 안 된다.
            GameObject sgo = new GameObject("Scan", typeof(RectTransform));
            RectTransform srt = sgo.GetComponent<RectTransform>();
            srt.SetParent(rt, false);
            Stretch(srt);
            RawImage scan = sgo.AddComponent<RawImage>();
            scan.texture = ScanTexture();
            scan.color = new Color(0.7f, 0.95f, 1f, usingArtPanel ? 0.16f : 0.30f);
            scan.raycastTarget = false;

            GameObject igo = new GameObject("Icon", typeof(RectTransform));
            RectTransform irt = igo.GetComponent<RectTransform>();
            irt.SetParent(rt, false);
            Stretch(irt);
            irt.offsetMin = new Vector2(iconInset, iconInset);
            irt.offsetMax = new Vector2(-iconInset, -iconInset);
            Image icon = igo.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = iconTint;

            // 벨트 위에는 뜨되 툴팁보다는 아래. 순서를 캔버스로 고정해야
            // 생성 시점(계층 순서)에 좌우되지 않는다.
            Canvas hc = go.AddComponent<Canvas>();
            hc.overrideSorting = true;
            hc.sortingOrder = HologramSortingOrder;

            CanvasGroup g = go.AddComponent<CanvasGroup>();
            g.alpha = 0f;
            g.blocksRaycasts = false;
            g.interactable = false;

            go.SetActive(false);
            panels[i] = rt; icons[i] = icon; scans[i] = scan; groups[i] = g;
        }

        // 홀로그램을 캔버스 뒤 형제로 붙였으므로 툴팁을 다시 앞으로 올린다.
        // (툴팁이 떠 있는 상태에서만 유효 — 꺼져 있으면 다음에 뜰 때 처리된다)
        if (drag != null) drag.EnsureTooltipOnTop();
    }

    // 이웃 슬롯 중심 사이의 최소 거리(캔버스 기준). lossyScale로 나눠 배율 영향을 뺀다 —
    // 벨트는 Shift에서 확대되는데, 패널 폭은 확대 전 기준으로 정해야 비율이 유지된다.
    float MeasureSlotSpacing(int n)
    {
        float best = float.MaxValue;
        Vector3[] a = new Vector3[4], b = new Vector3[4];
        for (int i = 1; i < n; i++)
        {
            RectTransform r0 = hud.GetSlotRect(i - 1), r1 = hud.GetSlotRect(i);
            if (r0 == null || r1 == null) continue;
            r0.GetWorldCorners(a); r1.GetWorldCorners(b);
            float c0 = (a[0].x + a[3].x) * 0.5f, c1 = (b[0].x + b[3].x) * 0.5f;
            float scale = Mathf.Max(0.0001f, r1.lossyScale.x);
            float d = Mathf.Abs(c1 - c0) / scale;
            if (d > 1f && d < best) best = d;
        }
        return best == float.MaxValue ? 0f : best;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    // 주사선 + 미세 노이즈 텍스처. 세로로 반복(Repeat)시켜 uvRect로 흘린다.
    // 홀로그램의 "완전히 선명하지 않은" 느낌은 이 오버레이가 만든다.
    static Texture2D ScanTexture()
    {
        if (scanTex != null) return scanTex;
        const int W = 8, H = 8;
        scanTex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        scanTex.wrapMode = TextureWrapMode.Repeat;
        scanTex.filterMode = FilterMode.Bilinear;
        UnityEngine.Random.InitState(20260727);
        for (int y = 0; y < H; y++)
        {
            // 짝수 줄만 밝게 → 가로 주사선
            float line = (y % 2 == 0) ? 0.85f : 0.15f;
            for (int x = 0; x < W; x++)
            {
                float noise = UnityEngine.Random.Range(-0.18f, 0.18f);
                float a = Mathf.Clamp01(line + noise);
                scanTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        scanTex.Apply();
        return scanTex;
    }

    // 홀로그램 패널 배경.
    // Resources의 도트 에셋을 우선 쓰고, 없으면 코드로 그린 사각형으로 폴백한다
    // (BladeStorm 칼날과 같은 규약 — 에셋이 없어도 게임이 깨지지 않게).
    static Sprite BoxSprite()
    {
        if (boxSprite != null) return boxSprite;

        boxSprite = Resources.Load<Sprite>("UI/HologramPanel");
        if (boxSprite != null) { usingArtPanel = true; return boxSprite; }

        return FallbackBox();
    }

    // 도트 에셋을 쓰는 중이면 코드로 그린 테두리/채움 색을 덧입히지 않는다(원본 색 유지).
    static bool usingArtPanel;

    static Sprite FallbackBox()
    {
        const int N = 16, R = 3;
        Texture2D t = new Texture2D(N, N, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                bool cut = (x < R && y < R && (R - x) + (R - y) > R)
                        || (x >= N - R && y < R && (x - (N - 1 - R)) + (R - y) > R)
                        || (x < R && y >= N - R && (R - x) + (y - (N - 1 - R)) > R)
                        || (x >= N - R && y >= N - R && (x - (N - 1 - R)) + (y - (N - 1 - R)) > R);
                t.SetPixel(x, y, cut ? Color.clear : Color.white);
            }
        t.Apply();
        boxSprite = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 16f, 0,
                                  SpriteMeshType.FullRect, new Vector4(R + 1, R + 1, R + 1, R + 1));
        return boxSprite;
    }
}
