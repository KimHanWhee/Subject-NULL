using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// 설정 오버레이 — 어디서든 SettingsUI.Open()으로 띄운다(메인메뉴·일시정지 공용).
// 시간 제어는 하지 않는다: 일시정지에서 열면 이미 멈춰 있고, 메인메뉴에서는 멈출 게 없다.
public class SettingsUI : MonoBehaviour
{
    static readonly Color Ink = new Color(0.92f, 0.94f, 1f);
    static readonly Color Accent = new Color(0.55f, 0.9f, 1f);
    static readonly Color Muted = new Color(0.55f, 0.6f, 0.7f);

    public static bool IsOpen { get; private set; }

    TMP_FontAsset font;
    RectTransform panel;
    Slider volumeSlider;
    TextMeshProUGUI volumeValue;
    Image koBtn, enBtn;

    // 언어를 바꾸면 글자를 다시 써야 하는 대상들
    TextMeshProUGUI titleTxt, volumeLabel, langLabel, hintTxt, closeLabel;

    public static void Open()
    {
        if (IsOpen) return;
        new GameObject("SettingsUI").AddComponent<SettingsUI>().Build();
    }

    void Build()
    {
        IsOpen = true;
        font = Resources.Load<TMP_FontAsset>("Fonts/KoreanSDF");
        EnsureEventSystem();

        GameObject canvasGo = new GameObject("SettingsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas c = canvasGo.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 4300; // 일시정지(4000)·고난선택(4200) 위, 로딩(5000) 아래
        CanvasScaler sc = canvasGo.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);

        Image dim = MakeImage(canvasGo.transform, "Dim", Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.85f));
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = Vector2.zero;
        dim.rectTransform.offsetMax = Vector2.zero;
        dim.raycastTarget = true;

        Image box = MakeImage(dim.transform, "Panel", Vector2.zero, new Vector2(760f, 520f), Color.white);
        Sprite frame = Resources.Load<Sprite>("UI/LabDossierPanel");
        if (frame != null) { box.sprite = frame; box.type = Image.Type.Sliced; }
        else box.color = new Color(0.1f, 0.11f, 0.16f, 0.97f);
        panel = box.rectTransform;

        titleTxt = MakeText(panel, "Title", "", 44, new Vector2(0f, 190f), new Vector2(700f, 60f), Accent, FontStyles.Bold);

        // ── 음량 ──
        volumeLabel = MakeText(panel, "VolLabel", "", 26, new Vector2(-230f, 78f), new Vector2(260f, 36f), Ink, FontStyles.Bold);
        volumeLabel.alignment = TextAlignmentOptions.Left;
        volumeValue = MakeText(panel, "VolValue", "", 26, new Vector2(268f, 78f), new Vector2(120f, 36f), Accent, FontStyles.Bold);
        volumeValue.alignment = TextAlignmentOptions.Right;
        BuildVolumeSlider(new Vector2(0f, 34f), new Vector2(600f, 18f));

        // ── 언어 ──
        langLabel = MakeText(panel, "LangLabel", "", 26, new Vector2(-230f, -40f), new Vector2(260f, 36f), Ink, FontStyles.Bold);
        langLabel.alignment = TextAlignmentOptions.Left;
        koBtn = BuildLangButton("한국어", new Vector2(60f, -42f), Language.Ko);
        enBtn = BuildLangButton("English", new Vector2(240f, -42f), Language.En);

        hintTxt = MakeText(panel, "Hint", "", 18, new Vector2(0f, -125f), new Vector2(660f, 30f), Muted, FontStyles.Normal);

        // ── 닫기 ──
        Image closeImg = MakeImage(panel, "Close", new Vector2(0f, -195f), new Vector2(240f, 62f), Color.white);
        Sprite btnSprite = Resources.Load<Sprite>("UI/SubjectNullButton");
        if (btnSprite != null) { closeImg.sprite = btnSprite; closeImg.type = Image.Type.Sliced; }
        else closeImg.color = new Color(0.3f, 0.33f, 0.4f);
        Button close = closeImg.gameObject.AddComponent<Button>();
        close.targetGraphic = closeImg;
        close.onClick.AddListener(Close);
        closeLabel = MakeText(closeImg.rectTransform, "Label", "", 26, Vector2.zero, new Vector2(240f, 62f), Ink, FontStyles.Bold);

        Loc.OnChanged += RefreshTexts;
        RefreshTexts();
    }

    void BuildVolumeSlider(Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject("VolumeSlider", typeof(RectTransform));
        go.transform.SetParent(panel, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image bg = MakeImage(rt, "BG", Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.12f));
        Stretch(bg.rectTransform);

        GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(rt, false);
        RectTransform fa = (RectTransform)fillArea.transform;
        Stretch(fa);

        Image fill = MakeImage(fa, "Fill", Vector2.zero, Vector2.zero, Accent);
        Stretch(fill.rectTransform);

        GameObject handleArea = new GameObject("HandleArea", typeof(RectTransform));
        handleArea.transform.SetParent(rt, false);
        RectTransform ha = (RectTransform)handleArea.transform;
        Stretch(ha);

        Image handle = MakeImage(ha, "Handle", Vector2.zero, new Vector2(26f, 34f), Color.white);

        Slider s = go.AddComponent<Slider>();
        s.fillRect = fill.rectTransform;
        s.handleRect = handle.rectTransform;
        s.targetGraphic = handle;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.SetValueWithoutNotify(SettingsService.MasterVolume);
        s.onValueChanged.AddListener(OnVolumeChanged);
        volumeSlider = s;
    }

    Image BuildLangButton(string label, Vector2 pos, Language lang)
    {
        Image img = MakeImage(panel, "Lang_" + lang, pos, new Vector2(170f, 56f), Color.white);
        Sprite btnSprite = Resources.Load<Sprite>("UI/SubjectNullButton");
        if (btnSprite != null) { img.sprite = btnSprite; img.type = Image.Type.Sliced; }
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        Language captured = lang;
        b.onClick.AddListener(() => OnLanguage(captured));
        MakeText(img.rectTransform, "Label", label, 24, Vector2.zero, new Vector2(170f, 56f), Ink, FontStyles.Bold);
        return img;
    }

    void OnVolumeChanged(float v)
    {
        SettingsService.SetMasterVolume(v);
        if (volumeValue != null) volumeValue.text = Mathf.RoundToInt(v * 100f) + "%";
    }

    void OnLanguage(Language lang)
    {
        SettingsService.SetLanguage(lang); // Loc.OnChanged → RefreshTexts 가 이어서 호출됨
    }

    // 언어 전환 시 즉시 반영 — 창을 닫았다 열 필요가 없게
    void RefreshTexts()
    {
        if (titleTxt != null) titleTxt.text = Loc.T("settings.title");
        if (volumeLabel != null) volumeLabel.text = Loc.T("settings.volume");
        if (langLabel != null) langLabel.text = Loc.T("settings.language");
        if (hintTxt != null) hintTxt.text = Loc.T("settings.hint");
        if (closeLabel != null) closeLabel.text = Loc.T("common.close");
        if (volumeValue != null) volumeValue.text = Mathf.RoundToInt(SettingsService.MasterVolume * 100f) + "%";

        // 선택된 언어 버튼을 밝게
        if (koBtn != null) koBtn.color = Loc.Current == Language.Ko ? Color.white : new Color(0.45f, 0.5f, 0.58f);
        if (enBtn != null) enBtn.color = Loc.Current == Language.En ? Color.white : new Color(0.45f, 0.5f, 0.58f);
    }

    void Close() { Destroy(gameObject); }

    void OnDestroy()
    {
        Loc.OnChanged -= RefreshTexts;
        IsOpen = false;
    }

    // ---- 헬퍼 ----

    static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static Image MakeImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return img;
    }

    TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Vector2 pos,
        Vector2 sizeDelta, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = style;
        t.raycastTarget = false;
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = pos;
        return t;
    }
}
