using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Design Ref: CLAUDE.md — "플레이어는 사전에 스펠 마블 덱을 구성할 수 있음(15~25개)".
// 덱 편성 씬 전체 UI를 코드로 생성(SpellMiniIndicator 패턴 — 씬 배선 최소화).
// 좌측: 슈트 탭 + 트럼프 카드 카탈로그(스킬 아이콘 크게, 슈트 뱃지 작게, 설명, 슈트별 카드색.
//       클릭=덱에 추가, 중복 허용) / 우측: 덱 5×5 그리드(스킬 아이콘 미니 카드, 클릭=제거).
// 스킬 고유 아이콘(SpellMarble.icon→SpellAbility.icon)이 없으면 구슬 스프라이트로 폴백 —
// 아이콘 에셋을 넣고 SpellMarble.icon에 연결만 하면 카드에 자동 반영.
// 저장은 DeckSaveService(marbleName JSON, 계정별) — GameScene의 SpellCaster가 로드.
public class DeckBuilderUI : MonoBehaviour
{
    [Header("Data (필수)")]
    public SpellMarbleRegistry registry;  // 전체 마블 카탈로그(단일 진실원)
    public MarbleSkinTable skinTable;     // 슈트×등급 → 구슬 스프라이트(아이콘 폴백용)
    public DeckData defaultDeck;          // 저장 덱이 없을 때 초기 구성

    [Header("Card Layout")]
    public Vector2 cardSize = new Vector2(210f, 300f); // 카탈로그 카드 크기
    public float cardSpacingX = 240f;
    public float cardSpacingY = 330f;
    public int cardsPerRow = 4;           // 슈트당 최대 8종 기준 4×2

    [Header("Deck Slot Layout")]
    public float slotSize = 92f;
    public float slotSpacing = 104f;

    // ── 런타임 상태 ─────────────────────────────────────────
    private readonly List<SpellMarble> deckList = new List<SpellMarble>();
    private List<SpellMarble> catalog;            // registry 정렬본(슈트→등급→이름)
    private Suit currentSuit = Suit.Spade;        // 현재 탭

    private RectTransform collectionPanel;
    private RectTransform cardGridRoot;           // 탭 전환 시 파괴/재생성
    private Image[] tabBgs;
    private Text[] tabLabels;
    private ScrollRect cardScroll; // 카탈로그 세로 스크롤(마블 증가 대응)
    private readonly Dictionary<SpellMarble, Text> cardBadges = new Dictionary<SpellMarble, Text>(); // 현재 탭 카드의 ×N

    private Image[] slotRings;                    // 덱 슬롯 등급 테두리
    private Image[] slotFrames;                   // 덱 슬롯 카드 프레임(슈트색)
    private Image[] slotArts;                     // 덱 슬롯 스킬 아이콘
    private Image[] slotSuitTags;                 // 덱 슬롯 좌상단 슈트 아이콘(스프라이트)
    private Text counterText;
    private Text feedbackText;
    private Button saveButton;
    private float feedbackUntil;

    // 툴팁(덱 슬롯 호버 — 카탈로그 카드는 설명이 카드에 직접 보임)
    private RectTransform tooltipRoot;
    private Text tooltipText;
    private Image tooltipIcon;

    // 레전드 연출: 홀로그램 오버레이(UV 스크롤) + 등급 테두리 무지개 순환
    private readonly List<RawImage> holoOverlays = new List<RawImage>();
    private readonly List<Image> legendEdges = new List<Image>();
    private bool[] slotLegend;            // 덱 슬롯의 레전드 여부(테두리 무지개 순환용)

    private static Font uiFont;
    private static Sprite circleSprite;   // 원(슈트 뱃지/구슬 폴백)
    private static Sprite roundedSprite;  // 라운드 사각(카드 프레임, 9-slice)
    private static Sprite roundedOutlineSprite; // 라운드 사각 외곽선(핀스트라이프, 9-slice)
    private static Texture2D holoTex;     // 홀로그램 포일(무지개 대각 그라데이션, tileable)

    // SUBJECT:NULL 실험실 UI(Resources/UI, 9-slice) — 패널/버튼/카드 크롬을 통일된 SF 룩으로.
    private static Sprite pixelPanelSprite, pixelButtonSprite, pixelCardFrame;
    private static bool pixelLoaded;
    static void LoadPixelUI()
    {
        if (pixelLoaded) return;
        pixelLoaded = true;
        pixelPanelSprite = Resources.Load<Sprite>("UI/LabDossierPanel");
        pixelButtonSprite = Resources.Load<Sprite>("UI/SubjectNullButton");
        pixelCardFrame = Resources.Load<Sprite>("UI/SpecimenCardFrame");   // SF 카드 프레임
    }

    async void Start()
    {
        if (registry == null || skinTable == null)
        {
            Debug.LogError("[DeckBuilder] registry/skinTable 미연결 — UI 생성 중단");
            return;
        }

        BuildCatalog();
        LoadInitialDeck();   // 우선 로컬 미러(마지막 서버 소유)로 즉시 표시
        BuildUI();
        RebuildCardGrid();
        RefreshAll();

        // 서버 소유 최신화 → 재구성(진실은 서버). 미보유가 갱신되면 잠금/덱에서 정리됨.
        await ServicesBootstrap.WaitSignedInAsync(); // WebGL 안전(Task.Delay 금지)
        if (await PlayerProfileService.RefreshAsync())
        {
            LoadInitialDeck();
            RebuildCardGrid();
            RefreshAll();
        }
    }

    void Update()
    {
        // 툴팁은 커서 추종(우측 상단 오프셋). 새 Input System(Mouse.current) — 프로젝트 관례
        if (tooltipRoot != null && tooltipRoot.gameObject.activeSelf && Mouse.current != null)
            tooltipRoot.position = Mouse.current.position.ReadValue() + new Vector2(18f, 18f);

        // 저장/오류 피드백 자동 소멸
        if (feedbackText != null && feedbackText.enabled && Time.unscaledTime > feedbackUntil)
            feedbackText.enabled = false;

        // 레전드 연출: 홀로그램 UV 스크롤 + 무지개 테두리 순환
        float t = Time.unscaledTime;
        for (int i = 0; i < holoOverlays.Count; i++)
        {
            if (holoOverlays[i] == null) continue;
            Rect r = holoOverlays[i].uvRect;
            r.x = t * 0.10f;
            r.y = -t * 0.06f;
            holoOverlays[i].uvRect = r;
        }
        for (int i = 0; i < legendEdges.Count; i++)
            if (legendEdges[i] != null)
                legendEdges[i].color = Color.HSVToRGB((t * 0.15f) % 1f, 0.6f, 1f);
        if (slotLegend != null)
            for (int i = 0; i < slotLegend.Length; i++)
                if (slotLegend[i] && slotRings[i] != null)
                    slotRings[i].color = Color.HSVToRGB((t * 0.15f + i * 0.06f) % 1f, 0.6f, 1f);
    }

    // ── 데이터 ──────────────────────────────────────────────
    void BuildCatalog()
    {
        catalog = new List<SpellMarble>();
        foreach (SpellMarble m in registry.allMarbles)
            if (m != null) catalog.Add(m);
        catalog.Sort(CompareMarble);
    }

    static int CompareMarble(SpellMarble a, SpellMarble b)
    {
        int c = a.suit.CompareTo(b.suit);
        if (c != 0) return c;
        c = a.grade.CompareTo(b.grade);
        if (c != 0) return c;
        return string.Compare(a.marbleName, b.marbleName, System.StringComparison.Ordinal);
    }

    void LoadInitialDeck()
    {
        deckList.Clear();
        if (DeckSaveService.HasSave())
            deckList.AddRange(DeckSaveService.Load(registry));
        if (deckList.Count == 0 && defaultDeck != null && defaultDeck.marbles != null)
            foreach (SpellMarble m in defaultDeck.marbles)
                if (m != null && deckList.Count < DeckSaveService.MaxSize) deckList.Add(m);
        deckList.RemoveAll(m => m == null || !OwnedMarblesService.IsOwned(m)); // 미보유 마블 제외
        TrimToLimits(); // 등급별 한도 초과분 정리(구버전 저장/기본덱 방어)
        deckList.Sort(CompareMarble);
    }

    // 등급별 최대 중복 수를 넘는 복사본 제거(한도만큼만 유지)
    void TrimToLimits()
    {
        var counts = new Dictionary<SpellMarble, int>();
        for (int i = deckList.Count - 1; i >= 0; i--)
        {
            SpellMarble m = deckList[i];
            if (m == null) { deckList.RemoveAt(i); continue; }
            int c; counts.TryGetValue(m, out c);
            if (c >= DeckRules.Instance.MaxCopies(m.grade)) deckList.RemoveAt(i);
            else counts[m] = c + 1;
        }
    }

    int CountInDeck(SpellMarble m)
    {
        int n = 0;
        foreach (SpellMarble d in deckList) if (d == m) n++;
        return n;
    }

    void AddToDeck(SpellMarble m)
    {
        if (!OwnedMarblesService.IsOwned(m))
        {
            ShowFeedback("미보유 마블입니다 — 뽑기로 획득하세요", false);
            return;
        }
        if (deckList.Count >= DeckSaveService.MaxSize)
        {
            ShowFeedback("덱이 가득 찼습니다 (최대 " + DeckSaveService.MaxSize + "개)", false);
            return;
        }
        int max = DeckRules.Instance.MaxCopies(m.grade);
        if (CountInDeck(m) >= max)
        {
            string skill = m.ability != null ? m.ability.abilityName : m.marbleName;
            ShowFeedback("'" + skill + "'은(는) 최대 " + max + "개까지 (" + GradeLabel(m.grade) + " 등급)", false);
            return;
        }
        deckList.Add(m);
        deckList.Sort(CompareMarble);
        RefreshAll();
    }

    static string GradeLabel(Grade g)
    {
        switch (g)
        {
            case Grade.Normal:  return "일반";
            case Grade.Gold:    return "골드";
            case Grade.Diamond: return "다이아";
            case Grade.Legend:  return "레전드";
            default:            return g.ToString();
        }
    }

    void RemoveAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= deckList.Count) return;
        deckList.RemoveAt(slotIndex);
        HideTooltip(); // 제거된 마블 툴팁 잔상 방지
        RefreshAll();
    }

    void SaveDeck()
    {
        if (deckList.Count < DeckSaveService.MinSize || deckList.Count > DeckSaveService.MaxSize)
        {
            ShowFeedback("덱은 " + DeckSaveService.MinSize + "~" + DeckSaveService.MaxSize + "개여야 합니다", false);
            return;
        }
        DeckSaveService.Save(deckList);
        ShowFeedback("덱 저장 완료!", true);
    }

    void AutoFill()
    {
        if (catalog.Count == 0) return;
        int guard = 0;
        while (deckList.Count < DeckSaveService.MaxSize && guard++ < 2000)
        {
            SpellMarble pick = null;
            int start = Random.Range(0, catalog.Count);
            for (int k = 0; k < catalog.Count; k++) // 한도 안 찬 마블만 후보
            {
                SpellMarble c = catalog[(start + k) % catalog.Count];
                if (OwnedMarblesService.IsOwned(c) && CountInDeck(c) < DeckRules.Instance.MaxCopies(c.grade)) { pick = c; break; }
            }
            if (pick == null) break; // 더 넣을 수 있는 마블 없음
            deckList.Add(pick);
        }
        deckList.Sort(CompareMarble);
        RefreshAll();
        ShowFeedback("남은 칸을 무작위로 채웠습니다", true);
    }

    void ClearDeck()
    {
        deckList.Clear();
        HideTooltip();
        RefreshAll();
    }

    // 카드 아트: 스킬 고유 아이콘 우선, 없으면 구슬(슈트×등급)로 폴백
    Sprite ArtOf(SpellMarble m)
    {
        if (m.icon != null) return m.icon;
        if (m.ability != null && m.ability.icon != null) return m.ability.icon;
        return skinTable.Get(m.suit, m.grade);
    }

    // 슈트별 카드 배경색(트럼프 컨셉 — 어두운 톤 위에 아트가 뜨도록)
    static Color SuitCardColor(Suit s)
    {
        switch (s)
        {
            case Suit.Spade:   return new Color(0.23f, 0.23f, 0.31f); // ♠ 차콜
            case Suit.Heart:   return new Color(0.40f, 0.18f, 0.24f); // ♥ 다크 레드
            case Suit.Club:    return new Color(0.16f, 0.33f, 0.22f); // ♣ 다크 그린
            case Suit.Diamond: return new Color(0.16f, 0.29f, 0.42f); // ♦ 스틸 블루
            default:           return new Color(0.2f, 0.2f, 0.25f);
        }
    }

    // ── UI 생성 ──────────────────────────────────────────────
    void BuildUI()
    {
        RectTransform root = (RectTransform)transform;

        // 배경: 어두운 펠트(카드 테이블 느낌)
        Image bg = MakeImage(root, "Background", Vector2.zero, new Vector2(1920f, 1080f), new Color(0.075f, 0.075f, 0.11f));
        bg.rectTransform.anchorMin = Vector2.zero;
        bg.rectTransform.anchorMax = Vector2.one;
        bg.rectTransform.offsetMin = Vector2.zero;
        bg.rectTransform.offsetMax = Vector2.zero;

        // 타이틀
        Text title = MakeText(root, "Title", new Vector2(0f, 495f), new Vector2(900f, 60f), 40, TextAnchor.MiddleCenter);
        title.text = "<b>스펠 마블 덱 편성</b>";
        title.color = new Color(0.95f, 0.93f, 0.85f);

        BuildCollectionPanel(root);
        BuildDeckPanel(root);
        BuildTooltip(root);
    }

    void BuildCollectionPanel(RectTransform root)
    {
        collectionPanel = MakePanel(root, "Collection", new Vector2(-320f, -75f), new Vector2(1220f, 890f));

        Text header = MakeText(collectionPanel, "Header", new Vector2(0f, 418f), new Vector2(1150f, 32f), 20, TextAnchor.MiddleLeft);
        header.text = "<b>마블 카탈로그</b>  <size=15><color=#9999AA>카드 클릭=덱 추가 · <b>스킬마다</b> 개별 중복 제한(등급 높을수록 적게: 일반6/골드3/다이아2/레전드1)</color></size>";

        // 슈트 탭 4개(중앙 정렬)
        tabBgs = new Image[4];
        tabLabels = new Text[4];
        float tabW = 180f;
        float tabGap = 196f;
        float x0 = -tabGap * 1.5f;
        for (int s = 0; s < 4; s++)
        {
            Suit suit = (Suit)s;
            Image tabImg = MakeImage(collectionPanel, "Tab_" + suit, new Vector2(x0 + s * tabGap, 360f), new Vector2(tabW, 52f), Color.white);
            tabImg.sprite = RoundedSprite();
            tabImg.type = Image.Type.Sliced;
            tabImg.raycastTarget = true;
            tabBgs[s] = tabImg;

            // 문양은 스프라이트 아이콘(폰트 글리프 아님) + 역할어 텍스트를 나란히 배치
            // 아이콘은 왼쪽 여백, 라벨은 남은 폭 중앙 — 서로 겹치지 않게 분리
            Image tabIcon = MakeImage(tabImg.rectTransform, "SuitIcon", new Vector2(-tabW * 0.5f + 30f, 0f), new Vector2(24f, 24f), SuitInfo.ColorOf(suit));
            tabIcon.sprite = SuitInfo.Icon(suit);
            tabIcon.raycastTarget = false;

            Text tabLbl = MakeText(tabImg.rectTransform, "Label", new Vector2(16f, 0f), new Vector2(tabW - 60f, 52f), 22, TextAnchor.MiddleCenter);
            tabLbl.text = SuitInfo.Role(suit);
            tabLabels[s] = tabLbl;

            Suit captured = suit;
            DeckItemEvents ev = tabImg.gameObject.AddComponent<DeckItemEvents>();
            ev.onClick = () => SelectTab(captured);
        }
        StyleTabs();

        // 카드 그리드 — 마블이 늘어 2행(8종)을 넘으면 세로 스크롤(마스크 뷰포트 + ScrollRect)
        GameObject viewportGo = new GameObject("CardViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        RectTransform viewport = (RectTransform)viewportGo.transform;
        viewport.SetParent(collectionPanel, false);
        viewport.anchorMin = viewport.anchorMax = new Vector2(0.5f, 0.5f);
        viewport.pivot = new Vector2(0.5f, 0.5f);
        viewport.sizeDelta = new Vector2(1150f, 700f);
        viewport.anchoredPosition = new Vector2(0f, -70f);
        Image vpImg = viewportGo.GetComponent<Image>();
        vpImg.color = new Color(0f, 0f, 0f, 0.01f); // 휠/드래그 스크롤 수신용(투명 레이캐스트)
        vpImg.raycastTarget = true;

        // 컨텐츠(탭 전환 시 통째로 재생성) — 높이는 RebuildCardGrid가 행 수에 맞춰 갱신
        cardGridRoot = MakeRect(viewport, "CardGrid", Vector2.zero, new Vector2(1150f, 700f));
        cardGridRoot.anchorMin = new Vector2(0.5f, 1f);
        cardGridRoot.anchorMax = new Vector2(0.5f, 1f);
        cardGridRoot.pivot = new Vector2(0.5f, 1f);
        cardGridRoot.anchoredPosition = Vector2.zero;

        cardScroll = viewportGo.AddComponent<ScrollRect>();
        cardScroll.viewport = viewport;
        cardScroll.content = cardGridRoot;
        cardScroll.horizontal = false;
        cardScroll.vertical = true;
        cardScroll.movementType = ScrollRect.MovementType.Clamped;
        cardScroll.scrollSensitivity = 28f;

        // 슬림 스크롤바(우측) — 내용이 넘칠 때만 표시
        GameObject sbGo = new GameObject("CardScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        RectTransform sbRt = (RectTransform)sbGo.transform;
        sbRt.SetParent(collectionPanel, false);
        sbRt.anchorMin = sbRt.anchorMax = new Vector2(0.5f, 0.5f);
        sbRt.pivot = new Vector2(0.5f, 0.5f);
        sbRt.sizeDelta = new Vector2(7f, 700f);
        sbRt.anchoredPosition = new Vector2(586f, -70f);
        sbGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

        GameObject handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        RectTransform handleRt = (RectTransform)handleGo.transform;
        handleRt.SetParent(sbRt, false);
        handleRt.anchorMin = Vector2.zero; handleRt.anchorMax = Vector2.one;
        handleRt.offsetMin = Vector2.zero; handleRt.offsetMax = Vector2.zero;
        Image handleImg = handleGo.GetComponent<Image>();
        handleImg.color = new Color(0.55f, 0.9f, 1f, 0.4f);

        Scrollbar sb = sbGo.GetComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;
        sb.handleRect = handleRt;
        sb.targetGraphic = handleImg;
        cardScroll.verticalScrollbar = sb;
        cardScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    void SelectTab(Suit suit)
    {
        if (currentSuit == suit) return;
        currentSuit = suit;
        StyleTabs();
        RebuildCardGrid();
        RefreshAll();
    }

    void StyleTabs()
    {
        for (int s = 0; s < 4; s++)
        {
            bool sel = (Suit)s == currentSuit;
            Color c = SuitCardColor((Suit)s);
            tabBgs[s].color = sel ? c : new Color(c.r * 0.4f, c.g * 0.4f, c.b * 0.4f, 0.85f);
            tabLabels[s].color = sel ? Color.white : new Color(0.65f, 0.65f, 0.7f);
        }
    }

    void RebuildCardGrid()
    {
        // 기존 카드 제거
        for (int i = cardGridRoot.childCount - 1; i >= 0; i--)
            Destroy(cardGridRoot.GetChild(i).gameObject);
        cardBadges.Clear();
        holoOverlays.Clear();
        legendEdges.Clear();

        List<SpellMarble> list = new List<SpellMarble>();
        foreach (SpellMarble m in catalog)
            if (m.suit == currentSuit) list.Add(m);

        // 행 중앙 정렬 그리드 — 2행까지는 뷰포트에 세로 중앙, 3행부터는 위 정렬 + 스크롤
        int rows = Mathf.Max(1, (list.Count + cardsPerRow - 1) / cardsPerRow);
        float block = rows * cardSpacingY;
        float contentH = Mathf.Max(block + 16f, 700f); // 뷰포트(700)보다 작아지지 않게
        cardGridRoot.sizeDelta = new Vector2(1150f, contentH);
        float topPad = (contentH - block) * 0.5f;

        for (int k = 0; k < list.Count; k++)
        {
            int row = k / cardsPerRow;
            int col = k % cardsPerRow;
            int rowCount = Mathf.Min(cardsPerRow, list.Count - row * cardsPerRow);
            float rowX0 = -(rowCount - 1) * cardSpacingX * 0.5f;
            // 카드 앵커는 컨텐츠 중앙 기준 — 중앙에서 위/아래로 배치
            float y = contentH * 0.5f - topPad - (row + 0.5f) * cardSpacingY;
            BuildCard(list[k], new Vector2(rowX0 + col * cardSpacingX, y));
        }

        if (cardScroll != null) cardScroll.verticalNormalizedPosition = 1f; // 탭 전환 시 맨 위로
    }

    // 카드 1장: SF 카드 프레임(SpecimenCardFrame, 등급색 틴트) + 스킬 아트(크게) + 슈트 뱃지(좌상단)
    //          + 등급 보석(우상단) + 이름 + 설명 + ×N 뱃지 — 레이아웃은 원래 스타일 그대로, 프레임만 교체.
    void BuildCard(SpellMarble m, Vector2 pos)
    {
        float w = cardSize.x, h = cardSize.y;
        Color gradeCol = GradePalette.ColorOf(m.grade);
        LoadPixelUI();

        // 카드 본체: 등급색으로 틴트한 SF 프레임(등급별 프레임 색). 레전드는 Update에서 무지개 순환
        Image frame = MakeImage(cardGridRoot, "Card_" + m.marbleName, pos, new Vector2(w, h), Color.white);
        if (pixelCardFrame != null)
        {
            frame.sprite = pixelCardFrame;
            frame.color = Color.Lerp(Color.white, gradeCol, 0.8f);
            frame.pixelsPerUnitMultiplier = 1.5f; // 테두리 두께를 카드 크기에 맞게(원본 비율 유지)
        }
        else { frame.sprite = RoundedSprite(); frame.color = SuitCardColor(m.suit); }
        frame.type = Image.Type.Sliced;
        frame.raycastTarget = true; // 클릭/호버 히트 영역
        RectTransform card = frame.rectTransform;
        if (m.grade == Grade.Legend) legendEdges.Add(frame);

        // 아트 영역(어두운 패널 위에 스킬 아이콘 — 프레임 내부창: 상단 헤더밴드 아래에 맞춤)
        Image artBg = MakeImage(card, "ArtBg", new Vector2(0f, 44f), new Vector2(w - 44f, 130f), new Color(0.06f, 0.06f, 0.09f, 0.85f));
        artBg.sprite = RoundedSprite();
        artBg.type = Image.Type.Sliced;
        Image art = MakeImage(card, "Art", new Vector2(0f, 44f), new Vector2(112f, 112f), Color.white);
        art.sprite = ArtOf(m);
        art.preserveAspect = true;

        // 레전드 홀로그램 포일(카드 위에 은은히, UV 스크롤은 Update에서)
        if (m.grade == Grade.Legend)
        {
            RectTransform holoRt = MakeRect(card, "Holo", Vector2.zero, new Vector2(w - 36f, h - 36f));
            RawImage holo = holoRt.gameObject.AddComponent<RawImage>();
            holo.texture = HoloTexture();
            holo.color = new Color(1f, 1f, 1f, 0.28f);
            holo.raycastTarget = false;
            holo.uvRect = new Rect(0f, 0f, 1.4f, 2f); // 카드 비율에 맞춰 타일
            holoOverlays.Add(holo);
        }

        // 슈트 뱃지(좌상단, 작게)
        Image badgeBg = MakeImage(card, "SuitBadge", new Vector2(-w * 0.5f + 24f, h * 0.5f - 24f), new Vector2(34f, 34f), new Color(0.05f, 0.05f, 0.08f, 0.9f));
        badgeBg.sprite = CircleSprite();
        Image suitIcon = MakeImage(badgeBg.rectTransform, "Symbol", Vector2.zero, new Vector2(20f, 20f), SuitInfo.ColorOf(m.suit));
        suitIcon.sprite = SuitInfo.Icon(m.suit);
        suitIcon.raycastTarget = false;

        // 등급 보석(우상단)
        Image gem = MakeImage(card, "GradeGem", new Vector2(w * 0.5f - 24f, h * 0.5f - 24f), new Vector2(20f, 20f), gradeCol);
        gem.sprite = CircleSprite();

        // 이름
        Text name = MakeText(card, "Name", new Vector2(0f, -46f), new Vector2(w - 40f, 28f), 19, TextAnchor.MiddleCenter);
        name.text = "<b>" + (m.ability != null ? m.ability.abilityName : m.marbleName) + "</b>";

        // 설명(하단 — 넘치면 잘림)
        Text desc = MakeText(card, "Desc", new Vector2(0f, -102f), new Vector2(w - 48f, 74f), 13, TextAnchor.UpperCenter);
        desc.text = m.ability != null ? m.ability.description : "";
        desc.color = new Color(0.82f, 0.82f, 0.88f);
        desc.verticalOverflow = VerticalWrapMode.Truncate;

        // 덱 포함 수(우하단 ×N)
        Text count = MakeText(card, "Count", new Vector2(w * 0.5f - 34f, -h * 0.5f + 20f), new Vector2(52f, 26f), 17, TextAnchor.MiddleRight);
        count.color = new Color(1f, 0.84f, 0.3f);
        cardBadges[m] = count;

        // 클릭=추가, 호버=살짝 확대(설명이 카드에 있으므로 툴팁 없음)
        DeckItemEvents ev = frame.gameObject.AddComponent<DeckItemEvents>();
        ev.onClick = () => AddToDeck(m);
        ev.onEnter = () => card.localScale = Vector3.one * 1.05f;
        ev.onExit = () => card.localScale = Vector3.one;

        // 미보유(Gold+ 미획득): 잠금 오버레이 + 라벨 (맨 위에 그려지도록 마지막에 추가)
        if (!OwnedMarblesService.IsOwned(m))
        {
            Image lockOv = MakeImage(card, "Lock", Vector2.zero, new Vector2(w - 20f, h - 20f), new Color(0.03f, 0.03f, 0.05f, 0.62f));
            lockOv.sprite = RoundedSprite();
            lockOv.type = Image.Type.Sliced;
            Text lockTxt = MakeText(card, "LockTxt", new Vector2(0f, 8f), new Vector2(w - 20f, 64f), 20, TextAnchor.MiddleCenter);
            lockTxt.text = "<b>미보유</b>\n<size=13><color=#CFC080>뽑기로 획득</color></size>";
            lockTxt.color = new Color(1f, 0.85f, 0.45f);
        }
    }

    void BuildDeckPanel(RectTransform root)
    {
        RectTransform panel = MakePanel(root, "Deck", new Vector2(620f, -75f), new Vector2(600f, 890f));

        Text header = MakeText(panel, "Header", new Vector2(0f, 418f), new Vector2(540f, 32f), 20, TextAnchor.MiddleLeft);
        header.text = "<b>내 덱</b>  <size=15><color=#9999AA>클릭하면 제거됩니다</color></size>";

        counterText = MakeText(panel, "Counter", new Vector2(0f, 418f), new Vector2(540f, 32f), 22, TextAnchor.MiddleRight);

        // 5×5 슬롯 그리드(미니 카드: 슈트색 프레임 + 스킬 아이콘 + 슈트 기호 + 등급 테두리)
        int n = DeckSaveService.MaxSize;
        slotRings = new Image[n];
        slotFrames = new Image[n];
        slotArts = new Image[n];
        slotSuitTags = new Image[n];
        slotLegend = new bool[n];
        float gx0 = -slotSpacing * 2f; // 5칸 중앙 정렬
        float gy0 = 290f;
        for (int i = 0; i < n; i++)
        {
            int col = i % 5;
            int row = i / 5;
            Vector2 pos = new Vector2(gx0 + col * slotSpacing, gy0 - row * slotSpacing);

            Image ring = MakeImage(panel, "SlotRing" + i, pos, new Vector2(slotSize + 8f, slotSize + 8f), new Color(1f, 1f, 1f, 0.06f));
            ring.sprite = RoundedSprite();
            ring.type = Image.Type.Sliced;
            slotRings[i] = ring;

            Image frame = MakeImage(panel, "Slot" + i, pos, new Vector2(slotSize, slotSize), new Color(1f, 1f, 1f, 0.04f));
            frame.sprite = RoundedSprite();
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = true;
            slotFrames[i] = frame;

            Image art = MakeImage(frame.rectTransform, "Art", new Vector2(0f, -4f), new Vector2(slotSize - 28f, slotSize - 28f), Color.white);
            art.preserveAspect = true;
            slotArts[i] = art;

            Image tag = MakeImage(frame.rectTransform, "SuitTag", new Vector2(-slotSize * 0.5f + 13f, slotSize * 0.5f - 12f), new Vector2(16f, 16f), Color.white);
            tag.raycastTarget = false;
            slotSuitTags[i] = tag;

            int captured = i;
            DeckItemEvents ev = frame.gameObject.AddComponent<DeckItemEvents>();
            ev.onClick = () => RemoveAt(captured);
            ev.onEnter = () => { if (captured < deckList.Count) ShowTooltip(deckList[captured]); };
            ev.onExit = HideTooltip;
        }

        // 하단 버튼들
        saveButton = MakeButton(panel, "SaveBtn", new Vector2(-150f, -350f), new Vector2(220f, 62f), "덱 저장", new Color(0.22f, 0.5f, 0.3f), SaveDeck);
        MakeButton(panel, "AutoBtn", new Vector2(90f, -350f), new Vector2(200f, 62f), "자동 채우기", new Color(0.28f, 0.3f, 0.45f), AutoFill);
        MakeButton(panel, "ClearBtn", new Vector2(240f, -350f), new Vector2(80f, 62f), "비우기", new Color(0.45f, 0.25f, 0.25f), ClearDeck);
        // 뒤로 버튼: 좌상단 통일 규격(덱/가챠/플레이방법 동일)
        MakeButton(root, "BackBtn", new Vector2(-810f, 476f), new Vector2(220f, 68f), "← 뒤로", new Color(0.28f, 0.3f, 0.36f), () => SceneLoader.Load("MainMenuScene"));

        // 저장/오류 피드백(버튼 위)
        feedbackText = MakeText(panel, "Feedback", new Vector2(0f, -298f), new Vector2(540f, 30f), 18, TextAnchor.MiddleCenter);
        feedbackText.enabled = false;
    }

    void BuildTooltip(RectTransform root)
    {
        tooltipRoot = MakeRect(root, "Tooltip", Vector2.zero, new Vector2(340f, 150f));
        tooltipRoot.pivot = new Vector2(0f, 0f); // 커서 우상단으로 펼침

        Image bg = MakeImage(tooltipRoot, "Bg", Vector2.zero, Vector2.zero, new Color(0.05f, 0.05f, 0.08f, 0.95f));
        bg.rectTransform.anchorMin = Vector2.zero;
        bg.rectTransform.anchorMax = Vector2.one;
        bg.rectTransform.offsetMin = Vector2.zero;
        bg.rectTransform.offsetMax = Vector2.zero;

        tooltipIcon = MakeImage(tooltipRoot, "Icon", Vector2.zero, new Vector2(48f, 48f), Color.white);
        tooltipIcon.rectTransform.anchorMin = new Vector2(0f, 1f);
        tooltipIcon.rectTransform.anchorMax = new Vector2(0f, 1f);
        tooltipIcon.rectTransform.anchoredPosition = new Vector2(34f, -34f);
        tooltipIcon.preserveAspect = true;

        tooltipText = MakeText(tooltipRoot, "Text", Vector2.zero, new Vector2(320f, 140f), 16, TextAnchor.UpperLeft);
        tooltipText.rectTransform.anchorMin = new Vector2(0f, 1f);
        tooltipText.rectTransform.anchorMax = new Vector2(0f, 1f);
        tooltipText.rectTransform.pivot = new Vector2(0f, 1f);
        tooltipText.rectTransform.anchoredPosition = new Vector2(66f, -12f);
        tooltipText.rectTransform.sizeDelta = new Vector2(264f, 130f);

        tooltipRoot.SetAsLastSibling();
        tooltipRoot.gameObject.SetActive(false);
    }

    // ── 갱신 ────────────────────────────────────────────────
    void RefreshAll()
    {
        // 덱 그리드(미니 카드)
        for (int i = 0; i < slotFrames.Length; i++)
        {
            if (i < deckList.Count)
            {
                SpellMarble m = deckList[i];
                slotFrames[i].color = SuitCardColor(m.suit);
                slotRings[i].color = GradePalette.ColorOf(m.grade);
                slotArts[i].sprite = ArtOf(m);
                slotArts[i].enabled = slotArts[i].sprite != null;
                slotArts[i].color = Color.white;
                slotSuitTags[i].sprite = SuitInfo.Icon(m.suit);
                slotSuitTags[i].color = SuitInfo.ColorOf(m.suit);
                slotSuitTags[i].enabled = true;
                slotLegend[i] = m.grade == Grade.Legend; // Update에서 무지개 순환
            }
            else
            {
                slotFrames[i].color = new Color(1f, 1f, 1f, 0.04f); // 빈 칸(희미한 카드)
                slotRings[i].color = new Color(1f, 1f, 1f, 0.06f);
                slotArts[i].enabled = false;
                slotSuitTags[i].enabled = false;
                slotLegend[i] = false;
            }
        }

        // 현재 탭 카드들의 ×N/최대 뱃지 (한도 도달 시 붉게)
        foreach (KeyValuePair<SpellMarble, Text> kv in cardBadges)
        {
            int cnt = CountInDeck(kv.Key);
            int max = DeckRules.Instance.MaxCopies(kv.Key.grade);
            kv.Value.text = "×" + cnt + "/" + max;
            kv.Value.color = cnt >= max ? new Color(1f, 0.5f, 0.4f) : new Color(1f, 0.84f, 0.3f);
        }

        // 카운터 + 저장 버튼 활성
        bool valid = deckList.Count >= DeckSaveService.MinSize && deckList.Count <= DeckSaveService.MaxSize;
        if (counterText != null)
        {
            string col = valid ? "#7FD98A" : "#FF7A7A";
            counterText.text = "<color=" + col + "><b>" + deckList.Count + "</b></color> / " +
                               DeckSaveService.MinSize + "~" + DeckSaveService.MaxSize;
        }
        if (saveButton != null) saveButton.interactable = valid;
    }

    void ShowFeedback(string msg, bool ok)
    {
        if (feedbackText == null) return;
        feedbackText.text = msg;
        feedbackText.color = ok ? new Color(0.55f, 0.9f, 0.6f) : new Color(1f, 0.55f, 0.55f);
        feedbackText.enabled = true;
        feedbackUntil = Time.unscaledTime + 2f;
    }

    void ShowTooltip(SpellMarble m)
    {
        if (tooltipRoot == null || m == null || m.ability == null) return;
        tooltipRoot.gameObject.SetActive(true);
        tooltipText.text =
            "<b>" + m.ability.abilityName + "</b>\n" +
            SuitInfo.RichLabel(m.suit) + "  <color=#FFD24A>[" + m.grade + "]</color>\n" +
            m.ability.description;
        tooltipIcon.sprite = ArtOf(m);
        tooltipIcon.enabled = tooltipIcon.sprite != null;
        if (Mouse.current != null)
            tooltipRoot.position = Mouse.current.position.ReadValue() + new Vector2(18f, 18f);
    }

    void HideTooltip()
    {
        if (tooltipRoot != null) tooltipRoot.gameObject.SetActive(false);
    }

    // ── UI 프리미티브 ────────────────────────────────────────
    static RectTransform MakeRect(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }

    static Image MakeImage(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform rt = MakeRect(parent, name, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Text MakeText(RectTransform parent, string name, Vector2 pos, Vector2 size, int fontSize, TextAnchor align)
    {
        RectTransform rt = MakeRect(parent, name, pos, size);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = UiFont();
        t.fontSize = fontSize;
        t.alignment = align;
        t.supportRichText = true;
        t.color = Color.white;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    RectTransform MakePanel(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        LoadPixelUI();
        Image img = MakeImage(parent, name, pos, size, new Color(0.11f, 0.11f, 0.17f, 0.9f));
        if (pixelPanelSprite != null) { img.sprite = pixelPanelSprite; img.color = Color.white; } // 실험실 네온 도시어 패널
        else img.sprite = RoundedSprite();
        img.type = Image.Type.Sliced;
        return img.rectTransform;
    }

    Button MakeButton(RectTransform parent, string name, Vector2 pos, Vector2 size, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        LoadPixelUI();
        Image img = MakeImage(parent, name, pos, size, color);
        img.raycastTarget = true;
        if (pixelButtonSprite != null)
        {
            img.sprite = pixelButtonSprite;                       // SF 네온 버튼(기능색은 밝게 틴트해 유지)
            img.color = Color.Lerp(color, Color.white, 0.4f);
        }
        else img.sprite = RoundedSprite();
        img.type = Image.Type.Sliced;
        Button btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.highlightedColor = new Color(0.82f, 0.96f, 1f);
        cb.pressedColor = new Color(0.66f, 0.86f, 0.96f);
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        Text t = MakeText(img.rectTransform, "Label", Vector2.zero, size, 20, TextAnchor.MiddleCenter);
        t.text = label;
        return btn;
    }

    // 한글 표시용 legacy 폰트(프로젝트 툴팁과 동일 계열 — TMP 기본 폰트는 한글 글리프 없음)
    static Font UiFont()
    {
        if (uiFont == null) uiFont = Resources.Load<Font>("Fonts/Pretendard-Regular"); // 한글 폰트 — WebGL은 OS 폰트 폴백이 없어 내장 필수
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return uiFont;
    }

    // 부드러운 원(슈트 뱃지/등급 보석) — SpellMiniIndicator.DotSprite와 같은 런타임 생성 방식
    static Sprite CircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
        float radius = size / 2f;
        Color[] px = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / radius;
                float aa = Mathf.Clamp01((1f - d) * radius * 0.5f);
                px[y * size + x] = new Color(1f, 1f, 1f, aa);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return circleSprite;
    }

    // 라운드 사각(카드/탭/버튼 프레임) — 9-slice border로 어떤 크기든 모서리 유지
    static Sprite RoundedSprite()
    {
        if (roundedSprite != null) return roundedSprite;
        const int size = 64;
        const float radius = 14f;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        float half = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 라운드 사각 SDF: 모서리 반경 밖이면 투명, 경계 1px AA
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius), 0f);
                float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                                      100f, 0, SpriteMeshType.FullRect, new Vector4(20f, 20f, 20f, 20f));
        return roundedSprite;
    }

    // 라운드 사각 외곽선(2px 라인) — 카드 안쪽 핀스트라이프용. 9-slice로 어떤 크기든 라인 유지
    static Sprite RoundedOutlineSprite()
    {
        if (roundedOutlineSprite != null) return roundedOutlineSprite;
        const int size = 64;
        const float radius = 12f;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] px = new Color[size * size];
        float half = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius), 0f);
                float d = Mathf.Sqrt(dx * dx + dy * dy) - radius; // 경계에서 0
                float line = Mathf.Clamp01(1.6f - Mathf.Abs(d));  // 경계 주변 ~2px 라인
                px[y * size + x] = new Color(1f, 1f, 1f, line);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        roundedOutlineSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                                             100f, 0, SpriteMeshType.FullRect, new Vector4(20f, 20f, 20f, 20f));
        return roundedOutlineSprite;
    }

    // 홀로그램 포일 텍스처(대각 무지개 + 광택 스트릭, 완전 tileable — RawImage UV 스크롤용)
    static Texture2D HoloTexture()
    {
        if (holoTex != null) return holoTex;
        const int size = 256;
        holoTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        holoTex.wrapMode = TextureWrapMode.Repeat;
        Color[] px = new Color[size * size];
        const float tau = Mathf.PI * 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 대각선 방향 무지개(한 변당 정수 사이클 → 이음매 없음)
                float hue = Mathf.Repeat((x + y) / (float)size, 1f);
                Color c = Color.HSVToRGB(hue, 0.8f, 1f);
                // 반대 대각선 광택 스트릭(정수 사이클)
                float streak = 0.5f + 0.5f * Mathf.Sin((x - y) * tau * 2f / size);
                c.a = 0.35f + 0.65f * streak * streak; // 스트릭 부분만 강하게
                px[y * size + x] = c;
            }
        }
        holoTex.SetPixels(px);
        holoTex.Apply();
        return holoTex;
    }
}
