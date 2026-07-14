using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// 계정 화면 — 코드 생성 UI(GachaShopUI 패턴). 익명 계정을 아이디/비번에 연결(영구화)하거나,
// 기존 계정으로 로그인. 씬엔 이 컴포넌트 + 카메라만 있으면 됨.
public class AccountUI : MonoBehaviour
{
    static readonly Color BG = new Color(0.07f, 0.075f, 0.11f, 1f);
    static readonly Color PANEL = new Color(0.12f, 0.12f, 0.18f, 0.95f);

    RectTransform canvasRoot;
    Text statusText, feedbackText;
    InputField linkUser, linkPass, loginUser, loginPass;
    Button linkBtn, loginBtn;
    float feedbackUntil;
    bool busy;
    static Font uiFont;
    static Sprite whiteSprite;

    async void Start()
    {
        EnsureEventSystem();
        BuildUI();
        for (int i = 0; i < 100 && !ServicesBootstrap.IsSignedIn; i++)
            await Task.Delay(100);
        RefreshStatus();
    }

    void Update()
    {
        if (feedbackText != null && feedbackText.enabled && Time.unscaledTime > feedbackUntil)
            feedbackText.enabled = false;
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
        GameObject canvasGo = new GameObject("AccountCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler sc = canvasGo.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasRoot = canvas.GetComponent<RectTransform>();

        Image bg = MakeImage(canvasRoot, "BG", Vector2.zero, new Vector2(1920f, 1080f), BG);
        Stretch(bg.rectTransform);

        MakeText(canvasRoot, "Title", new Vector2(0f, 430f), new Vector2(900f, 60f), 40, TextAnchor.MiddleCenter, "<b>계정</b>", new Color(1f, 0.86f, 0.4f));
        statusText = MakeText(canvasRoot, "Status", new Vector2(0f, 360f), new Vector2(1100f, 40f), 22, TextAnchor.MiddleCenter, "", new Color(0.8f, 0.85f, 0.95f));

        // 왼쪽: 계정 만들기(연결)
        RectTransform link = MakePanel(canvasRoot, "LinkPanel", new Vector2(-320f, -40f), new Vector2(560f, 560f));
        MakeText(link, "LH", new Vector2(0f, 220f), new Vector2(520f, 34f), 26, TextAnchor.MiddleCenter, "<b>계정 만들기</b>", Color.white);
        MakeText(link, "LHsub", new Vector2(0f, 176f), new Vector2(500f, 60f), 15, TextAnchor.UpperCenter, "<color=#9AA>지금 진행상황(GEM·마블) 그대로 유지한 채\n아이디/비번을 연결합니다. 다른 기기서 복구 가능.</color>", Color.white);
        linkUser = MakeInput(link, "LinkUser", new Vector2(0f, 90f), new Vector2(460f, 56f), "아이디 (3~20자)", false);
        linkPass = MakeInput(link, "LinkPass", new Vector2(0f, 20f), new Vector2(460f, 56f), "비밀번호 (8~30자, 대소문자·숫자·기호)", true);
        linkBtn = MakeButton(link, "LinkBtn", new Vector2(0f, -70f), new Vector2(460f, 68f), "계정 만들기", new Color(0.3f, 0.42f, 0.32f), OnLink);
        MakeText(link, "LinkHint", new Vector2(0f, -150f), new Vector2(500f, 40f), 14, TextAnchor.UpperCenter, "<color=#8A8A98>익명 상태에서 한 번만 연결됩니다.</color>", Color.white);

        // 오른쪽: 로그인(기존 계정)
        RectTransform login = MakePanel(canvasRoot, "LoginPanel", new Vector2(320f, -40f), new Vector2(560f, 560f));
        MakeText(login, "GH", new Vector2(0f, 220f), new Vector2(520f, 34f), 26, TextAnchor.MiddleCenter, "<b>로그인</b>", Color.white);
        MakeText(login, "GHsub", new Vector2(0f, 176f), new Vector2(500f, 60f), 15, TextAnchor.UpperCenter, "<color=#E0A06A>⚠ 다른 계정으로 로그인하면 지금 익명 진행상황엔\n접근할 수 없습니다(연결 안 했다면).</color>", Color.white);
        loginUser = MakeInput(login, "LoginUser", new Vector2(0f, 90f), new Vector2(460f, 56f), "아이디", false);
        loginPass = MakeInput(login, "LoginPass", new Vector2(0f, 20f), new Vector2(460f, 56f), "비밀번호", true);
        loginBtn = MakeButton(login, "LoginBtn", new Vector2(0f, -70f), new Vector2(460f, 68f), "로그인", new Color(0.3f, 0.32f, 0.5f), OnLogin);

        feedbackText = MakeText(canvasRoot, "Feedback", new Vector2(0f, -400f), new Vector2(1200f, 40f), 22, TextAnchor.MiddleCenter, "", new Color(1f, 0.6f, 0.55f));
        feedbackText.enabled = false;

        MakeButton(canvasRoot, "Back", new Vector2(-820f, 480f), new Vector2(180f, 54f), "← 메인 메뉴", new Color(0.25f, 0.25f, 0.32f), () => SceneLoader.Load("MainMenuScene"));
    }

    void RefreshStatus()
    {
        bool linked = AccountService.IsLinked;
        if (statusText != null)
        {
            if (!ServicesBootstrap.IsSignedIn) statusText.text = "<color=#E0A06A>서버 연결 중...</color>";
            else if (linked) statusText.text = "현재 계정: <b><color=#7FE08A>" + AccountService.Username + "</color></b> (영구 · 복구 가능)";
            else statusText.text = "현재 계정: <color=#E0A06A>익명</color> — 아직 저장 안 됨. 계정을 만들어 두세요.";
        }
        // 이미 연결됐으면 '계정 만들기'는 잠금
        if (linkBtn != null) linkBtn.interactable = ServicesBootstrap.IsSignedIn && !linked && !busy;
        if (loginBtn != null) loginBtn.interactable = ServicesBootstrap.IsSignedIn && !busy;
    }

    async void OnLink()
    {
        if (busy) return;
        busy = true; RefreshStatus();
        try
        {
            var res = await AccountService.LinkAsync(linkUser.text.Trim(), linkPass.text);
            if (res.ok) { ShowFeedback("계정 생성 완료! 이제 어느 기기서든 로그인해 복구할 수 있어요.", true); linkPass.text = ""; }
            else ShowFeedback(res.error);
        }
        catch (Exception e) { Debug.LogError("[Account] link: " + e); ShowFeedback("오류로 실패했습니다"); }
        finally { busy = false; RefreshStatus(); }
    }

    async void OnLogin()
    {
        if (busy) return;
        busy = true; RefreshStatus();
        try
        {
            var res = await AccountService.LoginAsync(loginUser.text.Trim(), loginPass.text);
            if (res.ok)
            {
                await PlayerProfileService.RefreshAsync(); // 로그인 계정의 재화/소유 반영
                ShowFeedback("로그인 성공!", true);
                loginPass.text = "";
            }
            else ShowFeedback(res.error);
        }
        catch (Exception e) { Debug.LogError("[Account] login: " + e); ShowFeedback("오류로 실패했습니다"); }
        finally { busy = false; RefreshStatus(); }
    }

    void ShowFeedback(string msg, bool ok = false)
    {
        if (feedbackText == null) return;
        feedbackText.text = msg;
        feedbackText.color = ok ? new Color(0.55f, 0.9f, 0.6f) : new Color(1f, 0.55f, 0.55f);
        feedbackText.enabled = true;
        feedbackUntil = Time.unscaledTime + 3.5f;
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

    InputField MakeInput(RectTransform parent, string name, Vector2 pos, Vector2 size, string placeholder, bool password)
    {
        Image box = MakeImage(parent, name, pos, size, new Color(0.08f, 0.08f, 0.13f));
        box.raycastTarget = true;
        InputField input = box.gameObject.AddComponent<InputField>();

        Text ph = MakeText(box.rectTransform, "Placeholder", Vector2.zero, new Vector2(size.x - 24f, size.y), 20, TextAnchor.MiddleLeft, placeholder, new Color(0.55f, 0.55f, 0.65f));
        ph.rectTransform.offsetMin = new Vector2(14f, 0f); ph.rectTransform.offsetMax = new Vector2(-14f, 0f);
        Text txt = MakeText(box.rectTransform, "Text", Vector2.zero, new Vector2(size.x - 24f, size.y), 20, TextAnchor.MiddleLeft, "", Color.white);
        txt.rectTransform.offsetMin = new Vector2(14f, 0f); txt.rectTransform.offsetMax = new Vector2(-14f, 0f);
        txt.supportRichText = false;

        input.textComponent = txt;
        input.placeholder = ph;
        input.targetGraphic = box;
        if (password) { input.contentType = InputField.ContentType.Password; input.inputType = InputField.InputType.Password; }
        input.lineType = InputField.LineType.SingleLine;
        input.characterLimit = 40;
        return input;
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
        MakeText(img.rectTransform, "Label", Vector2.zero, size, 22, TextAnchor.MiddleCenter, label, Color.white);
        return b;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
