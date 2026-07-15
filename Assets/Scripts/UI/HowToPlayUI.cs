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
    // 팔레트
    static readonly Color BG = new Color(0.07f, 0.08f, 0.12f, 1f);
    static readonly Color PANEL = new Color(0.13f, 0.15f, 0.21f, 0.96f);
    static readonly Color TAB_ON = new Color(0.96f, 0.78f, 0.25f, 1f);   // 선택된 탭(노랑)
    static readonly Color TAB_OFF = new Color(0.26f, 0.29f, 0.38f, 1f);  // 미선택 탭
    static readonly Color INK = new Color(0.92f, 0.94f, 1f, 1f);
    static readonly Color TITLE = new Color(1f, 0.86f, 0.4f, 1f);
    static readonly Color BACK_COL = new Color(0.7f, 0.28f, 0.32f, 1f);

    static readonly string[] TabNames = { "조작", "스펠 마블", "조커" };

    // 각 탭의 인게임 캡처(Resources/Help). 텍스트 오른쪽에 표시.
    static readonly string[] TabShots = { "Help/tab_controls", "Help/tab_marbles", "Help/tab_joker" };

    static readonly string[] TabBody =
    {
        // 조작
        "<b>이동</b>   WASD / 방향키\n\n" +
        "<b>공격</b>   마우스 좌클릭 (커서 방향)\n\n" +
        "<b>대시</b>   SPACE\n" +
        "     스태미너 소모\n" +
        "     원거리 공격 회피 · 쿨타임 1초\n\n" +
        "<b>Ctrl</b>   스펠 마블",

        // 스펠 마블
        "스펠 마블은 특수 능력을 발동시키는 마법 구슬입니다.\n" +
        "덱에서 직접 원하는 스펠 마블을 편성하여 사용할 수 있습니다.\n\n" +
        "<b>타입</b>\n" +
        "   <color=#7FB0FF>♠ 스페이드 (공격)</color>   <color=#FF7F8A>♥ 하트 (버프)</color>\n" +
        "   <color=#7FE08A>♣ 클로버 (유틸)</color>   <color=#C8A0FF>♦ 다이아몬드 (방어)</color>\n\n" +
        "<b>등급</b>   일반 → <color=#FFD24A>골드</color> → <color=#66D0FF>다이아</color> → <color=#FF8AF0>레전드</color>\n" +
        "   높은 등급일수록 강한 능력.\n" +
        "   골드 이상은 <b>마블 뽑기</b> 및 구슬 조각 교환으로 획득.\n\n" +
        "<b>사용</b>   Ctrl 홀드 시 스펠 마블 벨트 팝업\n" +
        "        Ctrl 홀드 + 마블 드래그 & 드롭 시 사용",

        // 조커
        "덱에 편성된 스펠 마블을 <b>모두 사용</b>하면\n" +
        "<color=#FFD24A><b>JOKER</b></color> 가 발동됩니다.\n\n" +
        "JOKER는 <b>플레이어와 적 모두에게</b>\n" +
        "강력한 피해를 줄 수 있으므로,\n" +
        "잘 이용하여 적을 효율적으로 물리쳐보세요!\n\n" +
        "   · <color=#FF7A7A>대숙청</color> — 안전지대 밖 전멸\n" +
        "   · <color=#FFE066>번개 폭풍</color> — 낙뢰 회피\n" +
        "   · <color=#C87AFF>집단 혼란</color> — 조작 반전"
    };

    TMP_FontAsset font;
    GameObject[] panels;
    Image[] tabImages;

    void Start()
    {
        font = Resources.Load<TMP_FontAsset>("Fonts/KoreanSDF");
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
        MakeText(root, "Title", "게임 방법", 60f, TITLE, TextAlignmentOptions.Center,
            new Vector2(0f, 440f), new Vector2(1200f, 110f), FontStyles.Bold);

        // --- 탭 버튼 ---
        tabImages = new Image[TabNames.Length];
        float tabW = 320f, tabH = 80f, gap = 24f;
        float totalW = TabNames.Length * tabW + (TabNames.Length - 1) * gap;
        float startX = -totalW * 0.5f + tabW * 0.5f;
        for (int i = 0; i < TabNames.Length; i++)
        {
            int idx = i;
            float x = startX + i * (tabW + gap);
            Button b = MakeButton(root, "Tab" + i, TabNames[i], 34f, TAB_OFF, INK,
                new Vector2(x, 330f), new Vector2(tabW, tabH));
            tabImages[i] = b.GetComponent<Image>();
            b.onClick.AddListener(() => ShowTab(idx));
        }

        // --- 내용 패널(탭별): 왼쪽 텍스트 + 오른쪽 인게임 캡처 ---
        panels = new GameObject[TabBody.Length];
        for (int i = 0; i < TabBody.Length; i++)
        {
            Image panel = MakeImage(root, "Panel" + i, PANEL);
            SetRect(panel.rectTransform, new Vector2(0f, -75f), new Vector2(1680f, 700f));

            // 왼쪽: 설명 텍스트
            TextMeshProUGUI body = MakeText(panel.rectTransform, "Body", TabBody[i], 30f, INK,
                TextAlignmentOptions.TopLeft, new Vector2(-420f, 0f), new Vector2(760f, 640f), FontStyles.Normal);
            body.lineSpacing = 6f;

            // 오른쪽: 인게임 스크린샷(비율 유지)
            Sprite shot = Resources.Load<Sprite>(TabShots[i]);
            if (shot != null)
            {
                Image img = MakeImage(panel.rectTransform, "Shot", Color.white);
                img.sprite = shot;
                img.preserveAspect = true;
                SetRect(img.rectTransform, new Vector2(450f, 0f), new Vector2(760f, 470f));
            }

            panels[i] = panel.gameObject;
        }

        // --- 뒤로 버튼 ---
        Button back = MakeButton(root, "BackButton", "← 뒤로", 34f, BACK_COL, Color.white,
            new Vector2(760f, -460f), new Vector2(300f, 88f));
        back.onClick.AddListener(OnBack);
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
        SetRect(img.rectTransform, anchoredPos, sizeDelta);
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        MakeText(img.rectTransform, "Label", label, size, textColor, TextAlignmentOptions.Center,
            Vector2.zero, sizeDelta, FontStyles.Bold);
        return b;
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
