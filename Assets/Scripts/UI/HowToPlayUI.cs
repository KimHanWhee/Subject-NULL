using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

// 게임 방법(도움말) 씬 — 탭 전환식 UI를 코드로 구성.
// 프로젝트의 런타임 UI 생성 관례(JokerSpell/FloatingText)와 동일한 방식.
// 씬에는 이 컴포넌트를 가진 GameObject 하나 + 카메라만 있으면 된다(Canvas·EventSystem 자동 생성).
public class HowToPlayUI : MonoBehaviour
{
    // 팔레트 — SUBJECT:NULL 실험실 테마(다크 네이비 + 청록 네온)
    static readonly Color BG = new Color(0.09f, 0.11f, 0.16f, 1f);
    static readonly Color PANEL = Color.white;                            // 패널 스프라이트가 색을 담당
    static readonly Color TAB_ON = Color.white;                           // 선택된 탭(밝게 — 네온 프레임 부각)
    static readonly Color TAB_OFF = new Color(0.5f, 0.56f, 0.62f, 1f);    // 미선택 탭(어둡게)
    static readonly Color INK = new Color(0.92f, 0.94f, 1f, 1f);
    static readonly Color TITLE = new Color(0.55f, 0.9f, 1f, 1f);         // 청록
    static readonly Color BACK_COL = Color.white;
    static readonly Color OUTLINE = new Color(0.02f, 0.05f, 0.08f, 1f);

    // 실험실 UI 스프라이트(Resources/UI, 9-슬라이스)
    Sprite panelSprite, buttonSprite;

    const int TabCount = 3;

    // 탭 제목/본문은 Loc에서 현재 언어로 가져온다.
    // (static readonly 배열로 두면 클래스 초기화 시점 언어가 굳어버려 전환에 대응하지 못한다)
    static string TabName(int i) { return Loc.T("howto.tab" + i); }
    static string TabBody(int i) { return Loc.T("howto.body" + i); }

    // 각 탭의 인게임 캡처(Resources/Help). 텍스트 오른쪽에 표시.
    static readonly string[] TabShots = { "Help/tab_controls", "Help/tab_marbles", "Help/tab_joker" };

    TMP_FontAsset font;
    GameObject[] panels;
    Image[] tabImages;

    void Start()
    {
        font = Resources.Load<TMP_FontAsset>("Fonts/KoreanSDF");
        panelSprite = Resources.Load<Sprite>("UI/LabDossierPanel");
        buttonSprite = Resources.Load<Sprite>("UI/SubjectNullButton");
        EnsureEventSystem();
        BuildUI();
        ShowTab(0);
    }

    // 새 인풋 시스템 프로젝트라 InputSystemUIInputModule 필요(StandaloneInputModule은 에러)
    void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    void BuildUI()
    {
        // --- Canvas ---
        GameObject canvasGo = new GameObject("HowToPlayCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        RectTransform root = canvas.GetComponent<RectTransform>();

        // --- 배경(전체) ---
        Image bg = MakeImage(root, "BG", BG);
        Stretch(bg.rectTransform);

        // --- 타이틀 ---
        var title = MakeText(root, "Title", Loc.T("howto.title"), 60f, TITLE, TextAlignmentOptions.Center,
            new Vector2(0f, 440f), new Vector2(1200f, 110f), FontStyles.Bold);
        AddOutline(title);

        // --- 탭 버튼 ---
        tabImages = new Image[TabCount];
        float tabW = 320f, tabH = 80f, gap = 24f;
        float totalW = TabCount * tabW + (TabCount - 1) * gap;
        float startX = -totalW * 0.5f + tabW * 0.5f;
        for (int i = 0; i < TabCount; i++)
        {
            int idx = i;
            float x = startX + i * (tabW + gap);
            Button b = MakeButton(root, "Tab" + i, TabName(i), 34f, TAB_OFF, INK,
                new Vector2(x, 330f), new Vector2(tabW, tabH));
            tabImages[i] = b.GetComponent<Image>();
            b.onClick.AddListener(() => ShowTab(idx));
        }

        // --- 내용 패널(탭별): 왼쪽 텍스트 + 오른쪽 인게임 캡처 ---
        panels = new GameObject[TabCount];
        for (int i = 0; i < TabCount; i++)
        {
            Image panel = MakeImage(root, "Panel" + i, PANEL);
            if (panelSprite != null) { panel.sprite = panelSprite; panel.type = Image.Type.Sliced; }
            SetRect(panel.rectTransform, new Vector2(0f, -95f), new Vector2(1680f, 760f));

            // 왼쪽: 설명 텍스트 (긴 탭 본문도 하단 테두리 안에 들어가게 넉넉히)
            TextMeshProUGUI body = MakeText(panel.rectTransform, "Body", TabBody(i), 30f, INK,
                TextAlignmentOptions.TopLeft, new Vector2(-395f, 0f), new Vector2(760f, 660f), FontStyles.Normal);
            body.lineSpacing = 6f;

            // 오른쪽: 인게임 스크린샷 — 네온 패널 프레임 안에 표시
            Sprite shot = Resources.Load<Sprite>(TabShots[i]);
            if (shot != null)
            {
                Image frame = MakeImage(panel.rectTransform, "ShotFrame", PANEL);
                if (panelSprite != null) { frame.sprite = panelSprite; frame.type = Image.Type.Sliced; }
                SetRect(frame.rectTransform, new Vector2(455f, 0f), new Vector2(770f, 480f));

                Image img = MakeImage(frame.rectTransform, "Shot", Color.white);
                img.sprite = shot;
                img.preserveAspect = true;
                SetRect(img.rectTransform, Vector2.zero, new Vector2(700f, 410f));
            }

            panels[i] = panel.gameObject;
        }

        // --- 뒤로 버튼 (좌상단 통일 규격 — 덱/가챠/플레이방법 동일) ---
        Button back = MakeButton(root, "BackButton", Loc.T("common.back"), 30f, BACK_COL, Color.white,
            new Vector2(-810f, 476f), new Vector2(220f, 68f));
        back.onClick.AddListener(OnBack);

        // --- 오픈소스 고지 (하단 중앙, 탭과 무관하게 상시 표시) ---
        // SIL OFL 1.1은 폰트 재배포 시 저작권 고지를 함께 제공할 것을 요구한다.
        MakeText(root, "Credits", "Pretendard © Kil Hyung-jin · SIL Open Font License 1.1", 18f,
            new Color(0.45f, 0.5f, 0.6f, 1f), TextAlignmentOptions.Center,
            new Vector2(0f, -505f), new Vector2(1200f, 28f), FontStyles.Normal);
    }

    void ShowTab(int index)
    {
        for (int i = 0; i < panels.Length; i++)
            if (panels[i] != null) panels[i].SetActive(i == index);
        for (int i = 0; i < tabImages.Length; i++)
            if (tabImages[i] != null) tabImages[i].color = (i == index) ? TAB_ON : TAB_OFF;
    }

    void OnBack()
    {
        SceneLoader.Load("MainMenuScene");
    }

    // ---- UI 헬퍼 ----

    Image MakeImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color,
        TextAlignmentOptions align, Vector2 anchoredPos, Vector2 sizeDelta, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.richText = true;
        SetRect(t.rectTransform, anchoredPos, sizeDelta);
        return t;
    }

    Button MakeButton(Transform parent, string name, string label, float size, Color bgColor, Color textColor,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        Image img = MakeImage(parent, name, bgColor);
        if (buttonSprite != null) { img.sprite = buttonSprite; img.type = Image.Type.Sliced; }
        SetRect(img.rectTransform, anchoredPos, sizeDelta);
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var c = b.colors;
        c.highlightedColor = new Color(0.75f, 0.95f, 1f);
        c.pressedColor = new Color(0.6f, 0.85f, 0.95f);
        c.fadeDuration = 0.1f;
        b.colors = c;
        var lbl = MakeText(img.rectTransform, "Label", label, size, textColor, TextAlignmentOptions.Center,
            Vector2.zero, sizeDelta, FontStyles.Bold);
        AddOutline(lbl);
        return b;
    }

    // 밝은 배경에서도 읽히도록 어두운 외곽선
    void AddOutline(TextMeshProUGUI t)
    {
        var m = t.fontMaterial;
        m.EnableKeyword("OUTLINE_ON");
        m.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, OUTLINE);
        m.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, 0.22f);
    }

    static void SetRect(RectTransform rt, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
