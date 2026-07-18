using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// 가챠 상점 씬 — 코드 생성 UI(HowToPlayUI/DeckBuilderUI 패턴).
// 왼쪽: 뽑기(GEM 소비 → 결과 연출). 오른쪽: 도감 + 조각 확정 교환.
// 씬엔 이 컴포넌트 + 카메라만 있으면 됨(Canvas/EventSystem 자동 생성). registry/skinTable은 인스펙터 연결.
public class GachaShopUI : MonoBehaviour
{
    public SpellMarbleRegistry registry;
    public MarbleSkinTable skinTable;

    static readonly Color BG = new Color(0.07f, 0.075f, 0.11f, 1f);
    static readonly Color PANEL = new Color(0.12f, 0.12f, 0.18f, 0.95f);
    static readonly Color GEM_COL = new Color(0.55f, 0.85f, 1f);
    static readonly Color SHARD_COL = new Color(0.8f, 0.65f, 1f);

    Text gemText, shardText, feedbackText, revealPlaceholder;
    RectTransform revealSlot;
    Button pullButton, pullButton10;
    RectTransform canvasRoot;
    float feedbackUntil;

    RectTransform collectionGrid;

    // 도감 호버 툴팁(덱 빌더와 동일 패턴 — 커서 추종)
    RectTransform tooltipRoot;
    Text tooltipText;
    Image tooltipIcon;

    static Font uiFont;
    static Sprite whiteSprite;

    // SUBJECT:NULL 실험실 UI(Resources/UI, 9-slice) — 패널/버튼/카드 크롬 통일.
    static Sprite pixelPanelSprite, pixelButtonSprite, pixelCardFrame;
    static bool pixelLoaded;
    static void LoadPixelUI()
    {
        if (pixelLoaded) return;
        pixelLoaded = true;
        pixelPanelSprite = Resources.Load<Sprite>("UI/LabDossierPanel");
        pixelButtonSprite = Resources.Load<Sprite>("UI/SubjectNullButton");
        pixelCardFrame = Resources.Load<Sprite>("UI/SpecimenCardFrame");
    }

    bool pulling;

    async void Start()
    {
        if (registry == null || skinTable == null) { Debug.LogError("[Gacha] registry/skinTable 미연결"); return; }
        EnsureEventSystem();
        BuildUI();
        RefreshCurrency();   // 우선 캐시(마지막 서버값) 표시
        RebuildCollection();

        // 서버 프로필 최신화 → 재표시(진실은 서버)
        await ServicesBootstrap.WaitSignedInAsync(); // WebGL 안전(Task.Delay 금지)
        await PlayerProfileService.RefreshAsync();
        RefreshCurrency();
        RebuildCollection();
    }

    // 서버 결과(marbleName/grade 문자열)를 카드 UI가 쓰는 형태로 변환.
    class Reward { public SpellMarble marble; public bool isNew; public int shardsGained; }

    Reward ToReward(GachaService.PullResult r)
    {
        return new Reward { marble = FindMarble(r.marbleName), isNew = r.isNew, shardsGained = r.shardsGained };
    }

    SpellMarble FindMarble(string marbleName)
    {
        if (string.IsNullOrEmpty(marbleName)) return null;
        foreach (SpellMarble m in registry.allMarbles)
            if (m != null && m.marbleName == marbleName) return m;
        return null;
    }

    void Update()
    {
        if (feedbackText != null && feedbackText.enabled && Time.unscaledTime > feedbackUntil)
            feedbackText.enabled = false;

        // 툴팁 커서 추종(우측 상단 오프셋)
        if (tooltipRoot != null && tooltipRoot.gameObject.activeSelf && Mouse.current != null)
            tooltipRoot.position = Mouse.current.position.ReadValue() + new Vector2(18f, 18f);
    }

    void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    void BuildUI()
    {
        GameObject canvasGo = new GameObject("GachaCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler sc = canvasGo.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        RectTransform root = canvas.GetComponent<RectTransform>();
        canvasRoot = root;

        Image bg = MakeImage(root, "BG", Vector2.zero, new Vector2(1920f, 1080f), BG);
        Stretch(bg.rectTransform);

        MakeText(root, "Title", new Vector2(0f, 495f), new Vector2(900f, 60f), 40, TextAnchor.MiddleCenter, "<b>마블 뽑기</b>", new Color(1f, 0.86f, 0.4f));

        // 재화(상단 우측, 세로로 쌓기 — 화면 안에)
        gemText = MakeText(root, "Gem", new Vector2(770f, 505f), new Vector2(320f, 36f), 24, TextAnchor.MiddleRight, "", GEM_COL);
        shardText = MakeText(root, "Shard", new Vector2(770f, 468f), new Vector2(320f, 34f), 22, TextAnchor.MiddleRight, "", SHARD_COL);
        MakeButton(root, "Charge", new Vector2(700f, 418f), new Vector2(250f, 56f), "＋ GEM 충전", new Color(0.2f, 0.5f, 0.42f), OpenChargeOverlay);

        BuildPullPanel(root);
        BuildCollectionPanel(root);

        feedbackText = MakeText(root, "Feedback", new Vector2(0f, -500f), new Vector2(1000f, 34f), 20, TextAnchor.MiddleCenter, "", new Color(1f, 0.6f, 0.55f));
        feedbackText.enabled = false;

        // 뒤로 버튼: 좌상단 통일 규격(덱/가챠/플레이방법 동일)
        MakeButton(root, "Back", new Vector2(-810f, 476f), new Vector2(220f, 68f), "← 뒤로", new Color(0.28f, 0.3f, 0.36f), () => SceneLoader.Load("MainMenuScene"));

        BuildTooltip(root); // 마지막에 생성 → 항상 최상단
    }

    void BuildTooltip(RectTransform root)
    {
        tooltipRoot = MakeRect(root, "Tooltip", Vector2.zero, new Vector2(360f, 170f));
        tooltipRoot.pivot = new Vector2(0f, 0f); // 커서 우상단으로 펼침

        Image bg = MakeImage(tooltipRoot, "Bg", Vector2.zero, Vector2.zero, new Color(0.05f, 0.05f, 0.08f, 0.95f));
        Stretch(bg.rectTransform);

        tooltipIcon = MakeImage(tooltipRoot, "Icon", Vector2.zero, new Vector2(48f, 48f), Color.white);
        tooltipIcon.rectTransform.anchorMin = new Vector2(0f, 1f);
        tooltipIcon.rectTransform.anchorMax = new Vector2(0f, 1f);
        tooltipIcon.rectTransform.anchoredPosition = new Vector2(34f, -34f);
        tooltipIcon.preserveAspect = true;

        tooltipText = MakeText(tooltipRoot, "Text", Vector2.zero, new Vector2(280f, 150f), 16, TextAnchor.UpperLeft, "", Color.white);
        tooltipText.rectTransform.anchorMin = new Vector2(0f, 1f);
        tooltipText.rectTransform.anchorMax = new Vector2(0f, 1f);
        tooltipText.rectTransform.pivot = new Vector2(0f, 1f);
        tooltipText.rectTransform.anchoredPosition = new Vector2(66f, -12f);

        tooltipRoot.SetAsLastSibling();
        tooltipRoot.gameObject.SetActive(false);
    }

    void ShowTooltip(SpellMarble m)
    {
        if (tooltipRoot == null || m == null || m.ability == null) return;
        tooltipRoot.gameObject.SetActive(true);
        tooltipRoot.SetAsLastSibling(); // 이후에 만들어진 오버레이/피드백 위로
        tooltipText.text =
            "<b>" + m.ability.abilityName + "</b>\n" +
            SuitInfo.RichLabel(m.suit) + "  <color=#FFD24A>[" + GradeLabel(m.grade) + "]</color>\n" +
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

    void BuildPullPanel(RectTransform root)
    {
        RectTransform panel = MakePanel(root, "PullPanel", new Vector2(-540f, -30f), new Vector2(720f, 860f));
        MakeText(panel, "PullHeader", new Vector2(0f, 380f), new Vector2(660f, 34f), 24, TextAnchor.MiddleCenter, "<b>뽑기</b>", Color.white);

        // 결과 카드 슬롯(뽑을 때마다 플립 카드 생성)
        revealSlot = MakeRect(panel, "RevealSlot", new Vector2(0f, 70f), new Vector2(320f, 440f));
        revealPlaceholder = MakeText(revealSlot, "Placeholder", Vector2.zero, new Vector2(300f, 60f), 20, TextAnchor.MiddleCenter, "여기에 결과가 표시됩니다", new Color(0.6f, 0.6f, 0.72f));

        pullButton = MakeButton(panel, "Pull1Btn", new Vector2(-175f, -330f), new Vector2(330f, 84f), "", new Color(0.32f, 0.28f, 0.5f), () => OnPull(1));
        pullButton10 = MakeButton(panel, "Pull10Btn", new Vector2(175f, -330f), new Vector2(330f, 84f), "", new Color(0.42f, 0.3f, 0.55f), () => OnPull(10));

        MakeButton(panel, "RateBtn", new Vector2(0f, -243f), new Vector2(230f, 50f), "확률 상세 보기", new Color(0.24f, 0.3f, 0.42f), OpenRateOverlay);
    }

    // ── 확률 상세(공시) ────────────────────────────────────
    // 서버 RNG와 동일 규칙으로 계산: 등급 가중치(GachaConfig) → 등급 내 균등.
    // 값이 서버(GachaPull.js)와 어긋나지 않도록 GachaConfig 가중치를 서버와 같게 유지할 것.
    void OpenRateOverlay()
    {
        Image dim = MakeImage(canvasRoot, "RateOverlay", Vector2.zero, new Vector2(1920f, 1080f), new Color(0f, 0f, 0f, 0.88f));
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        RectTransform panel = MakePanel(dim.rectTransform, "RatePanel", Vector2.zero, new Vector2(780f, 880f));
        MakeText(panel, "RT", new Vector2(0f, 385f), new Vector2(700f, 40f), 30, TextAnchor.MiddleCenter, "<b>뽑기 확률 상세</b>", new Color(1f, 0.86f, 0.4f));
        MakeText(panel, "RTsub", new Vector2(0f, 345f), new Vector2(700f, 26f), 15, TextAnchor.MiddleCenter, "<color=#9AA>등급을 먼저 추첨한 뒤, 같은 등급 안에서는 균등 확률로 결정됩니다</color>", Color.white);
        MakeButton(panel, "Close", new Vector2(330f, 400f), new Vector2(60f, 52f), "✕", new Color(0.3f, 0.3f, 0.38f), () => Destroy(dim.gameObject));

        // 뽑기 풀(도감과 동일: Normal 제외, 등급→이름 정렬)
        List<SpellMarble> pool = new List<SpellMarble>();
        foreach (SpellMarble m in registry.allMarbles)
            if (m != null && m.grade != Grade.Normal) pool.Add(m);
        pool.Sort((a, b) => { int c = a.grade.CompareTo(b.grade); return c != 0 ? c : string.Compare(a.marbleName, b.marbleName, System.StringComparison.Ordinal); });

        GachaConfig cfg = GachaConfig.Instance;
        float totalWeight = cfg.Weight(Grade.Gold) + cfg.Weight(Grade.Diamond) + cfg.Weight(Grade.Legend);
        Dictionary<Grade, int> countOf = new Dictionary<Grade, int>();
        foreach (SpellMarble m in pool)
        {
            if (!countOf.ContainsKey(m.grade)) countOf[m.grade] = 0;
            countOf[m.grade]++;
        }

        // 표: 등급 | 스킬명 | 확률(%)
        const float colGrade = -260f, colName = -40f, colRate = 240f;
        float y = 295f;
        const float rowH = 38f;
        Color headCol = new Color(0.65f, 0.7f, 0.85f);
        MakeText(panel, "HGrade", new Vector2(colGrade, y), new Vector2(160f, 30f), 18, TextAnchor.MiddleCenter, "<b>등급</b>", headCol);
        MakeText(panel, "HName", new Vector2(colName, y), new Vector2(300f, 30f), 18, TextAnchor.MiddleLeft, "<b>스킬명</b>", headCol);
        MakeText(panel, "HRate", new Vector2(colRate, y), new Vector2(160f, 30f), 18, TextAnchor.MiddleCenter, "<b>확률(%)</b>", headCol);
        MakeImage(panel, "HLine", new Vector2(0f, y - rowH * 0.55f), new Vector2(680f, 2f), new Color(1f, 1f, 1f, 0.25f));
        y -= rowH + 6f;

        for (int i = 0; i < pool.Count; i++)
        {
            SpellMarble m = pool[i];
            if ((i & 1) == 0) MakeImage(panel, "RowBG" + i, new Vector2(0f, y), new Vector2(680f, rowH - 4f), new Color(1f, 1f, 1f, 0.045f));
            int cnt = countOf[m.grade];
            float pct = totalWeight > 0f && cnt > 0 ? cfg.Weight(m.grade) / totalWeight / cnt * 100f : 0f;
            Color gc = GradePalette.ColorOf(m.grade);
            string name = m.ability != null ? m.ability.abilityName : m.marbleName;
            MakeText(panel, "RG" + i, new Vector2(colGrade, y), new Vector2(160f, 30f), 17, TextAnchor.MiddleCenter, "<b>" + GradeLabel(m.grade) + "</b>", gc);
            MakeText(panel, "RN" + i, new Vector2(colName, y), new Vector2(300f, 30f), 17, TextAnchor.MiddleLeft, name, Color.white);
            MakeText(panel, "RR" + i, new Vector2(colRate, y), new Vector2(160f, 30f), 17, TextAnchor.MiddleCenter, pct.ToString("0.##") + "%", new Color(0.85f, 0.9f, 1f));
            y -= rowH;
        }

        // 등급 합계 요약
        MakeImage(panel, "FLine", new Vector2(0f, y + rowH * 0.45f), new Vector2(680f, 2f), new Color(1f, 1f, 1f, 0.25f));
        string sum = "등급 합계 — ";
        Grade[] grades = { Grade.Gold, Grade.Diamond, Grade.Legend };
        for (int g = 0; g < grades.Length; g++)
        {
            float gp = totalWeight > 0f ? cfg.Weight(grades[g]) / totalWeight * 100f : 0f;
            Color gc = GradePalette.ColorOf(grades[g]);
            sum += "<color=#" + ColorUtility.ToHtmlStringRGB(gc) + ">" + GradeLabel(grades[g]) + " " + gp.ToString("0.##") + "%</color>";
            if (g < grades.Length - 1) sum += " · ";
        }
        MakeText(panel, "Sum", new Vector2(0f, y - 6f), new Vector2(700f, 30f), 17, TextAnchor.MiddleCenter, sum, Color.white);
        MakeText(panel, "Note", new Vector2(0f, y - 40f), new Vector2(700f, 26f), 14, TextAnchor.MiddleCenter, "<color=#8A8A98>일반 등급 14종은 기본 보유로 뽑기 대상이 아닙니다 · 중복 획득 시 조각으로 환급됩니다</color>", Color.white);
    }

    void BuildCollectionPanel(RectTransform root)
    {
        RectTransform panel = MakePanel(root, "CollectionPanel", new Vector2(560f, -30f), new Vector2(760f, 860f));
        MakeText(panel, "ColHeader", new Vector2(0f, 385f), new Vector2(700f, 34f), 22, TextAnchor.MiddleLeft, "<b>도감 · 조각 교환</b>", Color.white);
        collectionGrid = MakeRect(panel, "Grid", new Vector2(0f, -30f), new Vector2(720f, 720f));
    }

    // ── 뽑기 ──────────────────────────────────────────────
    // 서버 권위 뽑기: GachaService.PullAsync (GEM 차감·RNG·소유는 서버). 결과로 연출만.
    async void OnPull(int count)
    {
        if (pulling) return;
        int unit = GachaConfig.Instance.pullCostGem;
        int cost = unit * count;
        if (PlayerProfileService.Gem < cost) { ShowFeedback("GEM이 부족합니다 (필요 " + cost + ")"); return; }

        pulling = true;
        SetPullButtons(false);
        LoadingOverlay.Get().Show("로딩 중"); // 서버 뽑기 동안 반투명 로딩 오버레이
        try
        {
            if (count <= 1)
            {
                GachaService.PullResult r = await GachaService.PullAsync();
                if (r == null || !string.IsNullOrEmpty(r.error))
                { ShowFeedback(r != null && r.error == "INSUFFICIENT_GEM" ? "GEM이 부족합니다" : "뽑기에 실패했습니다"); return; }
                PlayerProfileService.ApplyPull(r);
                StopAllCoroutines();
                SingleReveal(ToReward(r));
            }
            else
            {
                List<Reward> rewards = new List<Reward>();
                for (int i = 0; i < count; i++)
                {
                    if (PlayerProfileService.Gem < unit) break;
                    GachaService.PullResult r = await GachaService.PullAsync();
                    if (r == null || !string.IsNullOrEmpty(r.error)) break;
                    PlayerProfileService.ApplyPull(r);
                    rewards.Add(ToReward(r));
                }
                if (rewards.Count == 0) { ShowFeedback("뽑기에 실패했습니다"); return; }
                ShowMultiResults(rewards);
            }
        }
        catch (Exception e) { Debug.LogError("[Gacha] pull 실패: " + e); ShowFeedback("네트워크 오류로 뽑기 실패"); }
        finally
        {
            LoadingOverlay.Get().Hide();
            pulling = false;
            RefreshCurrency();
            RebuildCollection(); // 새 획득 반영
        }
    }

    // 1회 뽑기: 슬롯에 뒷면 카드 생성 → 뒷면 오오라(등급 예고) 시작. 클릭하면 뒤집힘.
    void SingleReveal(Reward r)
    {
        if (revealPlaceholder != null) revealPlaceholder.enabled = false;
        for (int i = revealSlot.childCount - 1; i >= 0; i--)
            if (revealSlot.GetChild(i).name == "Card") Destroy(revealSlot.GetChild(i).gameObject);
        CardView cv = CreateCard(revealSlot, Vector2.zero, 300f, 420f, r);
        StartCoroutine(BackAuraLoop(cv));
    }

    // 10연 등 다연차 결과 — 전체화면 오버레이 + 클릭 플립(개별) + 전체 공개.
    void ShowMultiResults(List<Reward> results)
    {
        Image dim = MakeImage(canvasRoot, "MultiOverlay", Vector2.zero, new Vector2(1920f, 1080f), new Color(0f, 0f, 0f, 0.9f));
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        MakeText(dim.rectTransform, "Title", new Vector2(0f, 430f), new Vector2(1000f, 52f), 34, TextAnchor.MiddleCenter, "<b>" + results.Count + "연 뽑기</b>", new Color(1f, 0.86f, 0.4f));

        int newCnt = 0, shardSum = 0;
        foreach (var r in results) { if (r.isNew) newCnt++; shardSum += r.shardsGained; }

        int cols = 5;
        float cw = 176f, ch = 240f, gx = 200f, gy = 264f;
        List<CardView> cards = new List<CardView>();
        for (int k = 0; k < results.Count; k++)
        {
            int row = k / cols, col = k % cols;
            float x = (col - (cols - 1) * 0.5f) * gx;
            float y = 175f - row * gy;
            CardView cv = CreateCard(dim.rectTransform, new Vector2(x, y), cw, ch, results[k]);
            StartCoroutine(BackAuraLoop(cv));
            cards.Add(cv);
        }

        Text sum = MakeText(dim.rectTransform, "Sum", new Vector2(0f, -388f), new Vector2(1000f, 34f), 20, TextAnchor.MiddleCenter, "", Color.white);
        Button action = MakeButton(dim.rectTransform, "Action", new Vector2(0f, -440f), new Vector2(220f, 56f), "전체 공개", new Color(0.32f, 0.28f, 0.5f), null);
        action.onClick.AddListener(() => StartCoroutine(RevealAll(cards)));
        StartCoroutine(MultiWatch(cards, sum, action, dim, newCnt, shardSum));
    }

    // [전체 공개] — 아직 안 뒤집힌 카드들을 살짝 시차를 두고 뒤집음.
    IEnumerator RevealAll(List<CardView> cards)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            CardView cv = cards[i];
            if (cv != null && cv.root != null && !cv.flipped && !cv.revealing)
            {
                StartCoroutine(FlipCard(cv));
                yield return new WaitForSecondsRealtime(0.08f);
            }
        }
    }

    // 모든 카드가 뒤집히면 요약 표시 + 버튼을 [확인]으로 전환.
    IEnumerator MultiWatch(List<CardView> cards, Text sum, Button action, Image overlay, int newCnt, int shardSum)
    {
        while (true)
        {
            if (overlay == null) yield break;
            bool all = true;
            foreach (CardView c in cards)
                if (c != null && c.root != null && !c.flipped) { all = false; break; }
            if (all) break;
            yield return null;
        }
        sum.text = "신규 <color=#7FE08A>" + newCnt + "종</color>  ·  <color=#C8A0FF>+조각 " + shardSum + "</color>";
        Text sl = action.GetComponentInChildren<Text>(); if (sl != null) sl.text = "확인";
        action.onClick.RemoveAllListeners();
        action.onClick.AddListener(() => Destroy(overlay.gameObject));
    }

    // 뒷면 카드 생성(앞면 내용은 채워두고 숨김) + 클릭 타깃. CardView 반환.
    CardView CreateCard(RectTransform parent, Vector2 pos, float w, float h, Reward r)
    {
        Grade grade = r.marble != null ? r.marble.grade : Grade.Gold;
        Color gc = GradePalette.ColorOf(grade);
        RectTransform root = MakeRect(parent, "Card", pos, new Vector2(w, h));
        CardView cv = new CardView { root = root, grade = grade };

        cv.glow = MakeImage(root, "Glow", Vector2.zero, new Vector2(w * 1.45f, h * 1.3f), new Color(gc.r, gc.g, gc.b, 0f));
        cv.glow.sprite = GlowSprite(); // 부드러운 방사형(겹쳐도 자연스럽게 번짐)
        RectTransform pivot = MakeRect(root, "Pivot", Vector2.zero, new Vector2(w, h));
        cv.pivot = pivot;

        // 뒷면 — 등급색 프레임 + 오오라(등급은 색/오오라로만, 글자 없음).
        cv.back = MakeImage(pivot, "Back", Vector2.zero, new Vector2(w, h), gc); // 프레임(등급색)
        Image backBody = MakeImage(cv.back.rectTransform, "BackBody", Vector2.zero, new Vector2(w - 10f, h - 10f), new Color(0.12f, 0.11f, 0.19f));
        cv.q = MakeText(backBody.rectTransform, "Q", Vector2.zero, new Vector2(w, h * 0.5f), Mathf.RoundToInt(h * 0.34f), TextAnchor.MiddleCenter, "<b>?</b>", new Color(gc.r, gc.g, gc.b, 0.55f));
        // 클릭 시 ? → 스펠마블 아이콘(suit/grade, 타입만 표현). 처음엔 숨김.
        float backArtS = Mathf.Min(w - 40f, h * 0.5f);
        cv.typeArt = MakeImage(backBody.rectTransform, "TypeArt", Vector2.zero, new Vector2(backArtS, backArtS), Color.white);
        if (r.marble != null) { cv.typeArt.sprite = skinTable.Get(r.marble.suit, r.marble.grade); cv.typeArt.preserveAspect = true; }
        cv.typeArt.gameObject.SetActive(false);

        // 앞면(처음 숨김) — 레이아웃은 원래 스타일, 프레임만 SF 카드(등급색 틴트)로 교체
        Image edge = MakeImage(pivot, "Front", Vector2.zero, new Vector2(w, h), gc);
        RectTransform content;
        if (pixelCardFrame != null)
        {
            edge.sprite = pixelCardFrame;
            edge.color = Color.Lerp(Color.white, gc, 0.8f);
            edge.type = Image.Type.Sliced;
            edge.pixelsPerUnitMultiplier = 1.5f; // 테두리 두께를 카드 크기에 맞게
            content = edge.rectTransform;
        }
        else
        {
            Image body = MakeImage(edge.rectTransform, "Body", Vector2.zero, new Vector2(w - 6f, h - 6f), new Color(0.13f, 0.13f, 0.2f));
            content = body.rectTransform;
        }
        float artS = Mathf.Min(w - 64f, h * 0.4f);
        Image art = MakeImage(content, "Art", new Vector2(0f, h * 0.16f), new Vector2(artS, artS), Color.white);
        if (r.marble != null) { art.sprite = ArtOf(r.marble); art.preserveAspect = true; }
        string name = r.marble == null ? "?" : (r.marble.ability != null ? r.marble.ability.abilityName : r.marble.marbleName);
        int nameSize = Mathf.RoundToInt(Mathf.Clamp(w * 0.085f, 13f, 22f));
        MakeText(content, "Name", new Vector2(0f, -h * 0.15f), new Vector2(w - 24f, 44f), nameSize, TextAnchor.UpperCenter, "<b>" + name + "</b>", Color.white);
        MakeText(content, "Grade", new Vector2(0f, -h * 0.30f), new Vector2(w - 24f, 26f), Mathf.RoundToInt(nameSize * 0.75f), TextAnchor.UpperCenter, "<color=#FFD24A>[" + GradeLabel(grade) + "]</color>", Color.white);
        string tag = r.isNew ? "<color=#7FE08A><b>NEW</b></color>" : "<color=#CFC080>중복 +조각 " + r.shardsGained + "</color>";
        MakeText(content, "Tag", new Vector2(0f, -h * 0.5f + 20f), new Vector2(w - 24f, 24f), Mathf.RoundToInt(nameSize * 0.72f), TextAnchor.MiddleCenter, tag, Color.white);
        edge.gameObject.SetActive(false);
        cv.front = edge.rectTransform;

        // 클릭 타깃(최상단) — 클릭하면 뒤집힘.
        Image click = MakeImage(root, "Click", Vector2.zero, new Vector2(w, h), new Color(1f, 1f, 1f, 0f));
        click.raycastTarget = true;
        Button b = click.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        CardView captured = cv;
        b.onClick.AddListener(() => OnCardClicked(captured));
        cv.click = click;
        return cv;
    }

    void OnCardClicked(CardView cv)
    {
        if (cv == null || cv.root == null || cv.flipped || cv.revealing) return;
        StartCoroutine(FlipCard(cv));
    }

    // 뒷면 오오라(등급 예고, 뒤집힐 때까지 지속). 레전드는 무지개빛으로 회전.
    IEnumerator BackAuraLoop(CardView cv)
    {
        Color gc = GradePalette.ColorOf(cv.grade);
        float baseA = cv.grade == Grade.Legend ? 0.5f : (cv.grade == Grade.Diamond ? 0.34f : 0.18f);
        float amp = cv.grade == Grade.Legend ? 0.4f : (cv.grade == Grade.Diamond ? 0.24f : 0.13f);
        float speed = cv.grade == Grade.Legend ? 3.4f : (cv.grade == Grade.Diamond ? 2.2f : 1.4f);
        float breatheAmp = cv.grade == Grade.Legend ? 0.14f : (cv.grade == Grade.Diamond ? 0.07f : 0.03f);
        while (cv != null && cv.root != null && !cv.flipped)
        {
            float t = Time.unscaledTime;
            float a = baseA + amp * (0.5f + 0.5f * Mathf.Sin(t * speed));
            Color col = gc;
            if (cv.grade == Grade.Legend)
            {
                col = Color.HSVToRGB(Mathf.Repeat(t * 0.28f, 1f), 0.85f, 1f);
                if (cv.back != null) cv.back.color = col; // 프레임도 무지개로
            }
            cv.glow.color = new Color(col.r, col.g, col.b, a);
            float s = 1f + breatheAmp * (0.5f + 0.5f * Mathf.Sin(t * speed));
            cv.glow.rectTransform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }

    // 카드 뒤집기(클릭 시): ①? → 마블 스프라이트(타입 공개) ②에너지 모으기+빛 ③scale.x 1→0→1 앞면 교체.
    IEnumerator FlipCard(CardView cv)
    {
        if (cv == null || cv.root == null || cv.flipped || cv.revealing) yield break;
        cv.revealing = true;
        cv.flipped = true; // BackAuraLoop 종료 신호
        if (cv.click != null) cv.click.raycastTarget = false;
        Color gc = GradePalette.ColorOf(cv.grade);
        bool legend = cv.grade == Grade.Legend;
        bool dia = cv.grade == Grade.Diamond;

        // ① 타입 공개: ? 숨기고 마블 스프라이트 팝인 + 슈트 라벨.
        if (cv.q != null) cv.q.gameObject.SetActive(false);
        if (cv.typeArt != null && cv.typeArt.sprite != null)
        { cv.typeArt.gameObject.SetActive(true); yield return PopIn(cv.typeArt.rectTransform); }
        yield return new WaitForSecondsRealtime(legend ? 0.25f : 0.4f); // 타입 확인 여유

        // ② 에너지 모으기(+빛). 레전드는 길게 응축 후 번쩍.
        if (legend)
        {
            float dur = 0.9f, t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime; float k = t / dur;
                float s = Mathf.Lerp(1.6f, 0.8f, k); // 바깥→안으로 응축
                cv.glow.rectTransform.localScale = new Vector3(s, s, 1f);
                Color col = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.7f, 1f), 0.85f, 1f);
                cv.glow.color = new Color(col.r, col.g, col.b, Mathf.Lerp(0.5f, 1f, k));
                yield return null;
            }
            StartCoroutine(Flash(cv, 0.4f)); // 번쩍이며 뒤집힘
            yield return new WaitForSecondsRealtime(0.08f);
        }
        else if (dia)
        {
            float dur = 0.45f, t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime; float k = t / dur;
                float s = Mathf.Lerp(1.4f, 0.9f, k);
                cv.glow.rectTransform.localScale = new Vector3(s, s, 1f);
                cv.glow.color = new Color(gc.r, gc.g, gc.b, Mathf.Lerp(0.4f, 0.95f, k));
                yield return null;
            }
            StartCoroutine(Flash(cv, 0.22f));
        }
        else
        {
            float dur = 0.2f, t = 0f;
            while (t < dur) { t += Time.unscaledDeltaTime; cv.glow.color = new Color(gc.r, gc.g, gc.b, Mathf.Lerp(0.3f, 0.7f, t / dur)); yield return null; }
        }

        // ③ 뒤집기
        const float half = 0.11f;
        float f = 0f;
        while (f < half) { f += Time.unscaledDeltaTime; cv.pivot.localScale = new Vector3(Mathf.Lerp(1f, 0f, f / half), 1f, 1f); yield return null; }
        cv.back.gameObject.SetActive(false);
        cv.front.gameObject.SetActive(true);
        f = 0f;
        while (f < half) { f += Time.unscaledDeltaTime; cv.pivot.localScale = new Vector3(Mathf.Lerp(0f, 1f, f / half), 1f, 1f); yield return null; }
        cv.pivot.localScale = Vector3.one;
        cv.glow.rectTransform.localScale = Vector3.one;
        cv.glow.color = new Color(gc.r, gc.g, gc.b, cv.grade >= Grade.Diamond ? 0.4f : 0.3f);
        cv.revealing = false;

        if (legend) StartCoroutine(FrontLegendGlow(cv)); // 앞면도 무지개 잔광
    }

    // 타입 스프라이트 팝인(0.3→1.12→1.0).
    IEnumerator PopIn(RectTransform rt)
    {
        float t = 0f, up = 0.14f;
        while (t < up) { t += Time.unscaledDeltaTime; rt.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.12f, t / up); yield return null; }
        t = 0f; float down = 0.07f;
        while (t < down) { t += Time.unscaledDeltaTime; rt.localScale = Vector3.one * Mathf.Lerp(1.12f, 1f, t / down); yield return null; }
        rt.localScale = Vector3.one;
    }

    // 흰 빛 번쩍(카드 위로 짧게 터짐) — 응축된 에너지가 터지며 뒤집히는 순간을 덮음.
    IEnumerator Flash(CardView cv, float dur)
    {
        if (cv == null || cv.root == null) yield break;
        Vector2 sz = cv.root.sizeDelta;
        Image fl = MakeImage(cv.root, "Flash", Vector2.zero, new Vector2(sz.x * 1.7f, sz.y * 1.5f), new Color(1f, 1f, 1f, 0f));
        fl.sprite = GlowSprite();
        fl.rectTransform.SetAsLastSibling();
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime; float k = t / dur;
            float a = k < 0.35f ? Mathf.Lerp(0f, 1f, k / 0.35f) : Mathf.Lerp(1f, 0f, (k - 0.35f) / 0.65f);
            fl.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        if (fl != null) Destroy(fl.gameObject);
    }

    // 레전드 앞면 잔광 — 카드가 살아있는 동안 은은한 무지개.
    IEnumerator FrontLegendGlow(CardView cv)
    {
        while (cv != null && cv.root != null && cv.flipped)
        {
            Color col = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.2f, 1f), 0.7f, 1f);
            cv.glow.color = new Color(col.r, col.g, col.b, 0.32f + 0.1f * Mathf.Sin(Time.unscaledTime * 2f));
            yield return null;
        }
    }

    class CardView
    {
        public RectTransform root, pivot, front;
        public Image glow, back, click, typeArt;
        public Text q;
        public Grade grade;
        public bool flipped, revealing;
    }

    // ── 도감 · 교환 ───────────────────────────────────────
    void RebuildCollection()
    {
        HideTooltip(); // 아이템 재생성 중 툴팁 잔상 방지
        for (int i = collectionGrid.childCount - 1; i >= 0; i--) Destroy(collectionGrid.GetChild(i).gameObject);

        List<SpellMarble> pool = new List<SpellMarble>();
        foreach (SpellMarble m in registry.allMarbles)
            if (m != null && m.grade != Grade.Normal) pool.Add(m);
        pool.Sort((a, b) => { int c = a.grade.CompareTo(b.grade); return c != 0 ? c : string.Compare(a.marbleName, b.marbleName, System.StringComparison.Ordinal); });

        int cols = 3;
        float cw = 232f, ch = 138f, gapx = 240f, gapy = 146f;
        for (int k = 0; k < pool.Count; k++)
        {
            int row = k / cols, col = k % cols;
            float x = (col - (cols - 1) * 0.5f) * gapx;
            float y = 300f - row * gapy;
            BuildCollectionItem(pool[k], new Vector2(x, y), cw, ch);
        }
    }

    void BuildCollectionItem(SpellMarble m, Vector2 pos, float w, float h)
    {
        bool owned = OwnedMarblesService.IsOwned(m);
        Color gc = GradePalette.ColorOf(m.grade);
        LoadPixelUI();

        Image edge = MakeImage(collectionGrid, "Item_" + m.marbleName, pos, new Vector2(w, h), gc);
        edge.raycastTarget = true; // 호버 감지(자식 버튼 클릭은 그대로 동작 — 이벤트는 부모로도 전달됨)
        DeckItemEvents hover = edge.gameObject.AddComponent<DeckItemEvents>();
        SpellMarble hovered = m;
        hover.onEnter = () => ShowTooltip(hovered);
        hover.onExit = HideTooltip;
        Image body;
        if (pixelCardFrame != null)
        {
            edge.sprite = pixelCardFrame;
            edge.color = Color.Lerp(Color.white, gc, 0.8f); // 등급별 프레임 색
            edge.type = Image.Type.Sliced;
            edge.pixelsPerUnitMultiplier = 3f;              // 작은 아이템이라 테두리 더 얇게
            body = MakeImage(edge.rectTransform, "Body", Vector2.zero, new Vector2(w - 30f, h - 30f), new Color(0.1f, 0.12f, 0.16f, 0.75f));
        }
        else
            body = MakeImage(edge.rectTransform, "Body", Vector2.zero, new Vector2(w - 6f, h - 6f), new Color(0.15f, 0.15f, 0.22f));

        Image art = MakeImage(body.rectTransform, "Art", new Vector2(-w * 0.5f + 44f, 18f), new Vector2(64f, 64f), Color.white);
        art.sprite = ArtOf(m);
        art.preserveAspect = true;

        string name = m.ability != null ? m.ability.abilityName : m.marbleName;
        MakeText(body.rectTransform, "Name", new Vector2(24f, 30f), new Vector2(w - 100f, 30f), 17, TextAnchor.MiddleLeft, "<b>" + name + "</b>", Color.white);
        MakeText(body.rectTransform, "Grade", new Vector2(24f, 6f), new Vector2(w - 100f, 22f), 13, TextAnchor.MiddleLeft, "<color=#FFD24A>[" + GradeLabel(m.grade) + "]</color>", Color.white);

        if (owned)
        {
            MakeText(body.rectTransform, "Owned", new Vector2(0f, -42f), new Vector2(w - 20f, 28f), 17, TextAnchor.MiddleCenter, "<color=#7FE08A><b>보유중</b></color>", Color.white);
        }
        else
        {
            int cost = GachaConfig.Instance.ExchangeCost(m.grade);
            Button ex = MakeButton(body.rectTransform, "Exchange", new Vector2(0f, -42f), new Vector2(w - 30f, 42f), "조각 " + cost + " 교환", new Color(0.32f, 0.28f, 0.5f), null);
            SpellMarble captured = m;
            ex.onClick.AddListener(() => OnExchange(captured));
            // 어둡게(미보유)
            body.color = new Color(0.1f, 0.1f, 0.14f);
            art.color = new Color(1f, 1f, 1f, 0.5f);
        }
    }

    async void OnExchange(SpellMarble m)
    {
        if (OwnedMarblesService.IsOwned(m)) return;
        int cost = GachaConfig.Instance.ExchangeCost(m.grade);
        if (PlayerProfileService.Shards < cost) { ShowFeedback("조각이 부족합니다 (필요 " + cost + ")"); return; }
        try
        {
            GachaService.ExchangeResult res = await GachaService.ExchangeAsync(m.marbleName);
            if (res != null && res.ok)
            {
                OwnedMarblesService.MirrorGrant(m.marbleName);
                await PlayerProfileService.RefreshAsync();
                string name = m.ability != null ? m.ability.abilityName : m.marbleName;
                ShowFeedback("'" + name + "' 교환 완료!", true);
            }
            else
            {
                ShowFeedback("교환 실패" + (res != null && !string.IsNullOrEmpty(res.error) ? " (" + res.error + ")" : ""));
            }
        }
        catch (Exception e) { Debug.LogError("[Gacha] 교환 실패: " + e); ShowFeedback("네트워크 오류로 교환 실패"); }
        finally { RefreshCurrency(); RebuildCollection(); }
    }

    // ── GEM 충전(결제) ─────────────────────────────────────
    // 상품표를 서버에서 받아 오버레이로 표시. 실제 결제(PayPal)는 단계 3b에서 연결.
    async void OpenChargeOverlay()
    {
        Image dim = MakeImage(canvasRoot, "ChargeOverlay", Vector2.zero, new Vector2(1920f, 1080f), new Color(0f, 0f, 0f, 0.88f));
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        RectTransform panel = MakePanel(dim.rectTransform, "ChargePanel", Vector2.zero, new Vector2(720f, 720f));
        MakeText(panel, "CH", new Vector2(0f, 300f), new Vector2(680f, 40f), 30, TextAnchor.MiddleCenter, "<b>GEM 충전</b>", new Color(1f, 0.86f, 0.4f));
        MakeText(panel, "CHsub", new Vector2(0f, 258f), new Vector2(660f, 28f), 15, TextAnchor.MiddleCenter, "<color=#9AA>결제하면 서버 계정에 GEM이 즉시 지급됩니다.</color>", Color.white);
        MakeButton(panel, "Close", new Vector2(300f, 320f), new Vector2(60f, 52f), "✕", new Color(0.3f, 0.3f, 0.38f), () => Destroy(dim.gameObject));

        Text loading = MakeText(panel, "Loading", Vector2.zero, new Vector2(600f, 40f), 20, TextAnchor.MiddleCenter, "상품 불러오는 중...", new Color(0.7f, 0.7f, 0.8f));
        List<GachaService.GemPackage> pkgs = null;
        try { pkgs = await GachaService.GetGemPackagesAsync(); }
        catch (Exception e) { Debug.LogError("[Gacha] packages: " + e); }
        if (dim == null) return;
        if (loading != null) Destroy(loading.gameObject);
        if (pkgs == null || pkgs.Count == 0)
        { MakeText(panel, "Err", Vector2.zero, new Vector2(600f, 40f), 20, TextAnchor.MiddleCenter, "상품을 불러오지 못했습니다", new Color(1f, 0.6f, 0.55f)); return; }

        for (int i = 0; i < pkgs.Count; i++)
            BuildChargeCard(panel, pkgs[i], new Vector2(0f, 190f - i * 108f), dim);
    }

    void BuildChargeCard(RectTransform parent, GachaService.GemPackage p, Vector2 pos, Image overlay)
    {
        Image row = MakeImage(parent, "Pkg_" + p.sku, pos, new Vector2(640f, 96f), new Color(0.16f, 0.17f, 0.24f));
        string label = "<b>" + p.label + "</b>" + (string.IsNullOrEmpty(p.bonus) ? "" : "  <color=#7FE08A>" + p.bonus + "</color>");
        MakeText(row.rectTransform, "L", new Vector2(-110f, 16f), new Vector2(380f, 34f), 24, TextAnchor.MiddleLeft, label, Color.white);
        MakeText(row.rectTransform, "P", new Vector2(-110f, -22f), new Vector2(380f, 26f), 16, TextAnchor.MiddleLeft, "<color=#9AA>$" + p.priceUsd + "</color>", Color.white);
        GachaService.GemPackage cap = p;
        MakeButton(row.rectTransform, "Buy", new Vector2(230f, 0f), new Vector2(150f, 60f), "구매", new Color(0.2f, 0.5f, 0.42f), () => OnBuyGem(cap, overlay));
    }

    void OnBuyGem(GachaService.GemPackage p, Image overlay)
    {
        // 계정 게이트(하이브리드): 게스트(미연결)면 결제 불가 → 계정 만들기 유도.
        if (!AccountService.IsLinked)
        {
            ShowFeedback("구매하려면 계정이 필요합니다 — 계정 화면으로 이동합니다");
            if (overlay != null) Destroy(overlay.gameObject);
            SceneLoader.Load("AccountScene");
            return;
        }
        // TODO(단계 3b): PayPal createOrder → 승인 → captureOrder → 서버 GEM 지급. sandbox 키 연결 후 구현.
        ShowFeedback("PayPal 결제 연결 예정입니다 (" + p.label + ")", true);
    }

    // ── 갱신/헬퍼 ─────────────────────────────────────────
    void RefreshCurrency()
    {
        long gem = PlayerProfileService.Gem;
        if (gemText != null) gemText.text = "GEM <b>" + gem + "</b>";
        if (shardText != null) shardText.text = "조각 <b>" + PlayerProfileService.Shards + "</b>";
        int cost = GachaConfig.Instance.pullCostGem;
        if (pullButton != null)
        {
            Text lbl = pullButton.GetComponentInChildren<Text>();
            if (lbl != null) lbl.text = "<b>1회 뽑기</b>\n<size=15>" + cost + " GEM</size>";
            pullButton.interactable = !pulling && gem >= cost;
        }
        if (pullButton10 != null)
        {
            Text lbl = pullButton10.GetComponentInChildren<Text>();
            if (lbl != null) lbl.text = "<b>10회 뽑기</b>\n<size=15>" + (cost * 10) + " GEM</size>";
            pullButton10.interactable = !pulling && gem >= cost * 10;
        }
    }

    void SetPullButtons(bool on)
    {
        if (pullButton != null) pullButton.interactable = on;
        if (pullButton10 != null) pullButton10.interactable = on;
    }

    void ShowFeedback(string msg, bool ok = false)
    {
        if (feedbackText == null) return;
        feedbackText.text = msg;
        feedbackText.color = ok ? new Color(0.55f, 0.9f, 0.6f) : new Color(1f, 0.55f, 0.55f);
        feedbackText.enabled = true;
        feedbackText.transform.SetAsLastSibling(); // 오버레이 위로
        feedbackUntil = Time.unscaledTime + 2.5f;
    }

    Sprite ArtOf(SpellMarble m)
    {
        if (m.icon != null) return m.icon;
        if (m.ability != null && m.ability.icon != null) return m.ability.icon;
        return skinTable.Get(m.suit, m.grade);
    }

    static string GradeLabel(Grade g)
    {
        switch (g) { case Grade.Normal: return "일반"; case Grade.Gold: return "골드"; case Grade.Diamond: return "다이아"; case Grade.Legend: return "레전드"; default: return g.ToString(); }
    }

    // ── UI 프리미티브 ─────────────────────────────────────
    static Font UiFont()
    {
        if (uiFont == null) uiFont = Resources.Load<Font>("Fonts/malgun"); // 한글 폰트 — WebGL은 OS 폰트 폴백이 없어 내장 필수
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return uiFont;
    }

    static Sprite WhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        return whiteSprite;
    }

    // 부드러운 방사형 글로우(중앙 불투명 → 가장자리 투명). 등급 오라용.
    static Sprite glowSprite;
    static Sprite GlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        const int S = 64;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 c = new Vector2((S - 1) * 0.5f, (S - 1) * 0.5f);
        float maxd = S * 0.5f;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / maxd;
                float a = Mathf.Clamp01(1f - d);
                a = a * a; // 부드러운 falloff
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
        return glowSprite;
    }

    static RectTransform MakeRect(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        return rt;
    }

    static Image MakeImage(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform rt = MakeRect(parent, name, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = WhiteSprite();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Text MakeText(RectTransform parent, string name, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, string text, Color color)
    {
        RectTransform rt = MakeRect(parent, name, pos, size);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = UiFont(); t.fontSize = fontSize; t.alignment = align; t.supportRichText = true;
        t.text = text; t.color = color; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    RectTransform MakePanel(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        LoadPixelUI();
        Image img = MakeImage(parent, name, pos, size, PANEL);
        if (pixelPanelSprite != null) { img.sprite = pixelPanelSprite; img.color = Color.white; img.type = Image.Type.Sliced; } // 실험실 네온 도시어 패널
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
            img.type = Image.Type.Sliced;
        }
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors;
        cb.highlightedColor = new Color(0.82f, 0.96f, 1f);
        cb.pressedColor = new Color(0.66f, 0.86f, 0.96f);
        cb.fadeDuration = 0.1f;
        b.colors = cb;
        if (onClick != null) b.onClick.AddListener(onClick);
        MakeText(img.rectTransform, "Label", Vector2.zero, size, 20, TextAnchor.MiddleCenter, label, Color.white);
        return b;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
