using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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
    bool skipFlips;

    RectTransform collectionGrid;
    static Font uiFont;
    static Sprite whiteSprite;

    void Start()
    {
        if (registry == null || skinTable == null) { Debug.LogError("[Gacha] registry/skinTable 미연결"); return; }
        EnsureEventSystem();
        BuildUI();
        RefreshCurrency();
        RebuildCollection();
    }

    void Update()
    {
        if (feedbackText != null && feedbackText.enabled && Time.unscaledTime > feedbackUntil)
            feedbackText.enabled = false;
    }

    void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
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

        MakeText(root, "Title", new Vector2(0f, 495f), new Vector2(900f, 60f), 40, TextAnchor.MiddleCenter, "<b>가챠 상점</b>", new Color(1f, 0.86f, 0.4f));

        // 재화(상단 우측, 세로로 쌓기 — 화면 안에)
        gemText = MakeText(root, "Gem", new Vector2(770f, 505f), new Vector2(320f, 36f), 24, TextAnchor.MiddleRight, "", GEM_COL);
        shardText = MakeText(root, "Shard", new Vector2(770f, 468f), new Vector2(320f, 34f), 22, TextAnchor.MiddleRight, "", SHARD_COL);

        BuildPullPanel(root);
        BuildCollectionPanel(root);

        feedbackText = MakeText(root, "Feedback", new Vector2(0f, -500f), new Vector2(1000f, 34f), 20, TextAnchor.MiddleCenter, "", new Color(1f, 0.6f, 0.55f));
        feedbackText.enabled = false;

        MakeButton(root, "Back", new Vector2(-820f, 495f), new Vector2(180f, 54f), "← 메인 메뉴", new Color(0.25f, 0.25f, 0.32f), () => SceneManager.LoadScene("MainMenuScene"));
    }

    void BuildPullPanel(RectTransform root)
    {
        RectTransform panel = MakePanel(root, "PullPanel", new Vector2(-540f, -30f), new Vector2(720f, 860f));
        MakeText(panel, "PullHeader", new Vector2(0f, 380f), new Vector2(660f, 34f), 24, TextAnchor.MiddleCenter, "<b>뽑기</b>  <size=15><color=#9AA>Gold 이상 마블 · 중복은 조각으로 환급</color></size>", Color.white);

        // 결과 카드 슬롯(뽑을 때마다 플립 카드 생성)
        revealSlot = MakeRect(panel, "RevealSlot", new Vector2(0f, 70f), new Vector2(320f, 440f));
        revealPlaceholder = MakeText(revealSlot, "Placeholder", Vector2.zero, new Vector2(300f, 60f), 20, TextAnchor.MiddleCenter, "여기에 결과가 표시됩니다", new Color(0.6f, 0.6f, 0.72f));

        pullButton = MakeButton(panel, "Pull1Btn", new Vector2(-175f, -330f), new Vector2(330f, 84f), "", new Color(0.32f, 0.28f, 0.5f), () => OnPull(1));
        pullButton10 = MakeButton(panel, "Pull10Btn", new Vector2(175f, -330f), new Vector2(330f, 84f), "", new Color(0.42f, 0.3f, 0.55f), () => OnPull(10));
    }

    void BuildCollectionPanel(RectTransform root)
    {
        RectTransform panel = MakePanel(root, "CollectionPanel", new Vector2(560f, -30f), new Vector2(760f, 860f));
        MakeText(panel, "ColHeader", new Vector2(0f, 385f), new Vector2(700f, 34f), 22, TextAnchor.MiddleLeft, "<b>도감 · 조각 교환</b>  <size=14><color=#9AA>미보유는 조각으로 확정 교환</color></size>", Color.white);
        MakeText(panel, "ColNote", new Vector2(0f, 352f), new Vector2(700f, 26f), 14, TextAnchor.MiddleLeft, "<color=#8A8A98>Normal 14종은 기본 보유</color>", Color.white);
        collectionGrid = MakeRect(panel, "Grid", new Vector2(0f, -30f), new Vector2(720f, 720f));
    }

    // ── 뽑기 ──────────────────────────────────────────────
    void OnPull(int count)
    {
        int cost = GachaConfig.Instance.pullCostGem * count;
        if (!GachaMachine.CanPull(count)) { ShowFeedback("GEM이 부족합니다 (필요 " + cost + ")"); return; }

        if (count <= 1)
        {
            GachaMachine.PullResult r = GachaMachine.Pull(registry);
            if (r == null) { ShowFeedback("뽑기에 실패했습니다"); return; }
            StopAllCoroutines();
            StartCoroutine(SingleReveal(r));
        }
        else
        {
            List<GachaMachine.PullResult> results = GachaMachine.PullMulti(registry, count);
            if (results == null || results.Count == 0) { ShowFeedback("뽑기에 실패했습니다"); return; }
            ShowMultiResults(results);
        }
        RefreshCurrency();
        RebuildCollection(); // 새 획득 반영
    }

    // 1회 뽑기: 슬롯에 플립 카드 생성 → 등급색 예고 + 뒤집기.
    IEnumerator SingleReveal(GachaMachine.PullResult r)
    {
        if (revealPlaceholder != null) revealPlaceholder.enabled = false;
        for (int i = revealSlot.childCount - 1; i >= 0; i--)
            if (revealSlot.GetChild(i).name == "Card") Destroy(revealSlot.GetChild(i).gameObject);
        CardView cv = CreateCard(revealSlot, Vector2.zero, 300f, 420f, r);
        skipFlips = false;
        yield return FlipCard(cv, true);
    }

    // 10연 등 다연차 결과 — 전체화면 오버레이 + 순차 플립.
    void ShowMultiResults(List<GachaMachine.PullResult> results)
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
            cards.Add(CreateCard(dim.rectTransform, new Vector2(x, y), cw, ch, results[k]));
        }

        Text sum = MakeText(dim.rectTransform, "Sum", new Vector2(0f, 388f), new Vector2(1000f, 34f), 20, TextAnchor.MiddleCenter, "", Color.white);
        Button skip = MakeButton(dim.rectTransform, "Skip", new Vector2(0f, -430f), new Vector2(200f, 56f), "SKIP", new Color(0.28f, 0.28f, 0.36f), () => skipFlips = true);
        skipFlips = false;
        StartCoroutine(FlipSequence(cards, sum, skip, dim, newCnt, shardSum));
    }

    IEnumerator FlipSequence(List<CardView> cards, Text sum, Button skip, Image overlay, int newCnt, int shardSum)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            bool strong = cards[i].grade >= Grade.Diamond; // 다이아/레전드는 강한 예고
            yield return FlipCard(cards[i], strong && !skipFlips);
            if (!skipFlips) yield return new WaitForSecondsRealtime(0.06f);
        }
        // 완료 → 요약 + 확인
        sum.text = "신규 <color=#7FE08A>" + newCnt + "종</color>  ·  <color=#C8A0FF>+조각 " + shardSum + "</color>";
        if (skip != null)
        {
            Text sl = skip.GetComponentInChildren<Text>(); if (sl != null) sl.text = "확인";
            skip.onClick.RemoveAllListeners();
            skip.onClick.AddListener(() => Destroy(overlay.gameObject));
        }
    }

    // 뒷면 카드 생성(앞면 내용은 채워두고 숨김). CardView 반환.
    CardView CreateCard(RectTransform parent, Vector2 pos, float w, float h, GachaMachine.PullResult r)
    {
        Grade grade = r.marble.grade;
        Color gc = GradePalette.ColorOf(grade);
        RectTransform root = MakeRect(parent, "Card", pos, new Vector2(w, h));
        CardView cv = new CardView { root = root, grade = grade };

        cv.glow = MakeImage(root, "Glow", Vector2.zero, new Vector2(w * 1.45f, h * 1.3f), new Color(gc.r, gc.g, gc.b, 0f));
        cv.glow.sprite = GlowSprite(); // 부드러운 방사형(겹쳐도 자연스럽게 번짐)
        RectTransform pivot = MakeRect(root, "Pivot", Vector2.zero, new Vector2(w, h));
        cv.pivot = pivot;

        // 뒷면
        cv.back = MakeImage(pivot, "Back", Vector2.zero, new Vector2(w, h), new Color(0.17f, 0.15f, 0.26f));
        MakeText(cv.back.rectTransform, "Q", Vector2.zero, new Vector2(w, h), Mathf.RoundToInt(h * 0.42f), TextAnchor.MiddleCenter, "<b>?</b>", new Color(0.48f, 0.48f, 0.62f));

        // 앞면(처음 숨김)
        Image edge = MakeImage(pivot, "Front", Vector2.zero, new Vector2(w, h), gc);
        Image body = MakeImage(edge.rectTransform, "Body", Vector2.zero, new Vector2(w - 6f, h - 6f), new Color(0.13f, 0.13f, 0.2f));
        float artS = Mathf.Min(w - 56f, h * 0.44f);
        Image art = MakeImage(body.rectTransform, "Art", new Vector2(0f, h * 0.16f), new Vector2(artS, artS), Color.white);
        art.sprite = ArtOf(r.marble); art.preserveAspect = true;
        string name = r.marble.ability != null ? r.marble.ability.abilityName : r.marble.marbleName;
        int nameSize = Mathf.RoundToInt(Mathf.Clamp(w * 0.085f, 13f, 22f));
        MakeText(body.rectTransform, "Name", new Vector2(0f, -h * 0.15f), new Vector2(w - 12f, 44f), nameSize, TextAnchor.UpperCenter, "<b>" + name + "</b>", Color.white);
        MakeText(body.rectTransform, "Grade", new Vector2(0f, -h * 0.30f), new Vector2(w - 12f, 26f), Mathf.RoundToInt(nameSize * 0.75f), TextAnchor.UpperCenter, "<color=#FFD24A>[" + GradeLabel(grade) + "]</color>", Color.white);
        string tag = r.isNew ? "<color=#7FE08A><b>NEW</b></color>" : "<color=#CFC080>중복 +조각 " + r.shardsGained + "</color>";
        MakeText(body.rectTransform, "Tag", new Vector2(0f, -h * 0.5f + 16f), new Vector2(w - 12f, 24f), Mathf.RoundToInt(nameSize * 0.72f), TextAnchor.MiddleCenter, tag, Color.white);
        edge.gameObject.SetActive(false);
        cv.front = edge.rectTransform;
        return cv;
    }

    // 등급색 예고(빛 모임) → 카드 뒤집기(scale.x 1→0→1, 중간에 앞면 교체).
    IEnumerator FlipCard(CardView cv, bool anticipate)
    {
        Color gc = GradePalette.ColorOf(cv.grade);
        if (anticipate && !skipFlips)
        {
            float dur = cv.grade == Grade.Legend ? 0.9f : (cv.grade == Grade.Diamond ? 0.6f : 0.35f);
            float pulses = cv.grade >= Grade.Diamond ? 3f : 2f;
            float t = 0f;
            while (t < dur && !skipFlips)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(0.1f, 0.75f, t / dur) * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * pulses)));
                cv.glow.color = new Color(gc.r, gc.g, gc.b, a);
                yield return null;
            }
        }
        if (skipFlips)
        {
            cv.back.gameObject.SetActive(false);
            cv.front.gameObject.SetActive(true);
            cv.pivot.localScale = Vector3.one;
            cv.glow.color = new Color(gc.r, gc.g, gc.b, 0.35f);
            yield break;
        }
        const float half = 0.12f;
        float f = 0f;
        while (f < half) { f += Time.unscaledDeltaTime; cv.pivot.localScale = new Vector3(Mathf.Lerp(1f, 0f, f / half), 1f, 1f); yield return null; }
        cv.back.gameObject.SetActive(false);
        cv.front.gameObject.SetActive(true);
        f = 0f;
        while (f < half) { f += Time.unscaledDeltaTime; cv.pivot.localScale = new Vector3(Mathf.Lerp(0f, 1f, f / half), 1f, 1f); yield return null; }
        cv.pivot.localScale = Vector3.one;
        cv.glow.color = new Color(gc.r, gc.g, gc.b, 0.35f);
    }

    class CardView
    {
        public RectTransform root, pivot, front;
        public Image glow, back;
        public Grade grade;
    }

    // ── 도감 · 교환 ───────────────────────────────────────
    void RebuildCollection()
    {
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

        Image edge = MakeImage(collectionGrid, "Item_" + m.marbleName, pos, new Vector2(w, h), gc);
        Image body = MakeImage(edge.rectTransform, "Body", Vector2.zero, new Vector2(w - 6f, h - 6f), new Color(0.15f, 0.15f, 0.22f));

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
            int cost = GachaMachine.ExchangeCost(m);
            Button ex = MakeButton(body.rectTransform, "Exchange", new Vector2(0f, -44f), new Vector2(w - 30f, 34f), "조각 " + cost + " 교환", new Color(0.32f, 0.28f, 0.5f), null);
            SpellMarble captured = m;
            ex.onClick.AddListener(() => OnExchange(captured));
            // 어둡게(미보유)
            body.color = new Color(0.1f, 0.1f, 0.14f);
            art.color = new Color(1f, 1f, 1f, 0.5f);
        }
    }

    void OnExchange(SpellMarble m)
    {
        if (OwnedMarblesService.IsOwned(m)) return;
        int cost = GachaMachine.ExchangeCost(m);
        if (WalletService.Shards < cost) { ShowFeedback("조각이 부족합니다 (필요 " + cost + ")"); return; }
        if (GachaMachine.Exchange(m))
        {
            string name = m.ability != null ? m.ability.abilityName : m.marbleName;
            ShowFeedback("'" + name + "' 교환 완료!", true);
            RefreshCurrency();
            RebuildCollection();
        }
    }

    // ── 갱신/헬퍼 ─────────────────────────────────────────
    void RefreshCurrency()
    {
        if (gemText != null) gemText.text = "GEM <b>" + WalletService.Gem + "</b>";
        if (shardText != null) shardText.text = "조각 <b>" + WalletService.Shards + "</b>";
        int cost = GachaConfig.Instance.pullCostGem;
        if (pullButton != null)
        {
            Text lbl = pullButton.GetComponentInChildren<Text>();
            if (lbl != null) lbl.text = "<b>1회 뽑기</b>\n<size=15>" + cost + " GEM</size>";
            pullButton.interactable = GachaMachine.CanPull(1);
        }
        if (pullButton10 != null)
        {
            Text lbl = pullButton10.GetComponentInChildren<Text>();
            if (lbl != null) lbl.text = "<b>10회 뽑기</b>\n<size=15>" + (cost * 10) + " GEM</size>";
            pullButton10.interactable = GachaMachine.CanPull(10);
        }
    }

    void ShowFeedback(string msg, bool ok = false)
    {
        if (feedbackText == null) return;
        feedbackText.text = msg;
        feedbackText.color = ok ? new Color(0.55f, 0.9f, 0.6f) : new Color(1f, 0.55f, 0.55f);
        feedbackText.enabled = true;
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
        return MakeImage(parent, name, pos, size, PANEL).rectTransform;
    }

    Button MakeButton(RectTransform parent, string name, Vector2 pos, Vector2 size, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        Image img = MakeImage(parent, name, pos, size, color);
        img.raycastTarget = true;
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
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
