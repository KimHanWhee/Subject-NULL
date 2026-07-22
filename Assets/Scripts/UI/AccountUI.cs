using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Services.Authentication;

// 계정 화면 — 코드 생성 UI. 게스트(익명) ↔ Google 계정 구조.
// - 게스트: [Google 계정 연결] 로 진행상황 유지한 채 영구화
// - Google 연결됨: 상태 표시 + [로그아웃]
// 씬엔 이 컴포넌트 + 카메라만 있으면 됨.
public class AccountUI : MonoBehaviour
{
    static readonly Color BG = new Color(0.07f, 0.075f, 0.11f, 1f);
    static readonly Color PANEL = new Color(0.12f, 0.12f, 0.18f, 0.95f);

    RectTransform canvasRoot;
    Text statusText, feedbackText, playerIdText, nickText;
    Button linkBtn, logoutBtn, switchBtn, copyIdBtn, nickBtn;
    InputField nickInput;
    bool editingNick;
    float feedbackUntil;
    bool busy;
    bool switchConfirm; // [다른 계정으로 로그인] 2단계 확인(게스트 진행상황 포기 경고)
    static Font uiFont;
    static Sprite whiteSprite;

    async void Start()
    {
        EnsureEventSystem();
        BuildUI();
        await ServicesBootstrap.WaitSignedInAsync(); // WebGL 안전(Task.Delay 금지)
        await AccountService.RefreshAsync();
        try { await AuthenticationService.Instance.GetPlayerNameAsync(); } catch { } // 닉네임 확보(없으면 자동 생성)
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

        MakeText(canvasRoot, "Title", new Vector2(0f, 430f), new Vector2(900f, 60f), 40, TextAnchor.MiddleCenter, "<b>" + Loc.T("acc.title") + "</b>", new Color(1f, 0.86f, 0.4f));
        statusText = MakeText(canvasRoot, "Status", new Vector2(0f, 340f), new Vector2(1300f, 44f), 24, TextAnchor.MiddleCenter, "<color=#E0A06A>" + Loc.T("acc.connecting") + "</color>", new Color(0.8f, 0.85f, 0.95f));

        // 중앙 패널
        RectTransform panel = MakePanel(canvasRoot, "Panel", new Vector2(0f, -40f), new Vector2(720f, 460f));

        MakeText(panel, "Info", new Vector2(0f, 150f), new Vector2(640f, 100f), 18, TextAnchor.UpperCenter,
            "<color=#9AA>" + Loc.T("acc.guestNotice") + "</color>", Color.white);

        // 닉네임(리더보드 표시명) + 변경 — 인라인 입력 전환
        nickText = MakeText(panel, "Nick", new Vector2(-40f, -122f), new Vector2(460f, 30f), 18, TextAnchor.MiddleCenter, "", new Color(0.75f, 0.85f, 0.95f));
        nickBtn = MakeButton(panel, "NickEdit", new Vector2(230f, -122f), new Vector2(90f, 44f), Loc.T("acc.change"), new Color(0.3f, 0.32f, 0.44f), OnNickButton);
        nickBtn.gameObject.SetActive(false);

        // 플레이어 ID(운영/문의 대응용) + 복사 — 패널 하단에 작게
        playerIdText = MakeText(panel, "PlayerId", new Vector2(-40f, -170f), new Vector2(460f, 30f), 16, TextAnchor.MiddleCenter, "", new Color(0.55f, 0.6f, 0.72f));
        copyIdBtn = MakeButton(panel, "CopyId", new Vector2(230f, -170f), new Vector2(90f, 44f), Loc.T("acc.copy"), new Color(0.3f, 0.32f, 0.44f), OnCopyId);
        copyIdBtn.gameObject.SetActive(false);

        linkBtn = MakeButton(panel, "LinkGoogle", new Vector2(0f, 30f), new Vector2(460f, 76f), Loc.T("acc.linkGoogle"), new Color(0.28f, 0.4f, 0.34f), OnLinkGoogle);
        // 게스트 전용: 이미 다른 플레이어에 연결된 Google 계정으로 갈아타는 출구(로그인 선택 화면으로)
        switchBtn = MakeButton(panel, "SwitchAccount", new Vector2(0f, -70f), new Vector2(460f, 64f), Loc.T("acc.switch"), new Color(0.3f, 0.32f, 0.44f), OnSwitchAccount);
        logoutBtn = MakeButton(panel, "Logout", new Vector2(0f, -80f), new Vector2(460f, 64f), Loc.T("acc.logout"), new Color(0.42f, 0.28f, 0.28f), OnLogout);

        feedbackText = MakeText(canvasRoot, "Feedback", new Vector2(0f, -360f), new Vector2(1200f, 40f), 22, TextAnchor.MiddleCenter, "", new Color(1f, 0.6f, 0.55f));
        feedbackText.enabled = false;

        MakeButton(canvasRoot, "Back", new Vector2(-810f, 476f), new Vector2(220f, 68f), Loc.T("common.back"), new Color(0.25f, 0.25f, 0.32f), () => SceneLoader.Load("MainMenuScene"));

        linkBtn.gameObject.SetActive(false);
        switchBtn.gameObject.SetActive(false);
        logoutBtn.gameObject.SetActive(false);
    }

    void RefreshStatus()
    {
        bool googleLinked = AccountService.IsLinked;
        if (statusText != null)
        {
            if (!ServicesBootstrap.IsSignedIn)
                statusText.text = "<color=#E0A06A>" + Loc.T("acc.failed") + "</color>";
            else if (googleLinked)
                statusText.text = Loc.T("acc.linked");
            else
                statusText.text = Loc.T("acc.guest");
        }
        bool signedIn = ServicesBootstrap.IsSignedIn;
        if (linkBtn != null)
        {
            linkBtn.gameObject.SetActive(signedIn && !googleLinked);
            linkBtn.interactable = !busy && GoogleAuth.IsSupported;
            Text label = linkBtn.GetComponentInChildren<Text>();
            if (label != null && !GoogleAuth.IsSupported)
                label.text = Loc.T("acc.linkGoogleWeb");
        }
        if (logoutBtn != null)
        {
            // 게스트 상태에서 로그아웃하면 진행상황을 잃으므로 Google 연결 시에만 노출
            logoutBtn.gameObject.SetActive(signedIn && googleLinked);
            logoutBtn.interactable = !busy;
        }
        if (switchBtn != null)
        {
            // 게스트 전용 — Google 연결 계정으로 갈아탈 때 사용(연결됨 상태에선 [로그아웃]이 같은 역할)
            switchBtn.gameObject.SetActive(signedIn && !googleLinked);
            switchBtn.interactable = !busy;
        }
        if (playerIdText != null)
        {
            string pid = CurrentPlayerId();
            playerIdText.text = string.IsNullOrEmpty(pid) ? "" : Loc.T("acc.playerId") + pid;
            if (copyIdBtn != null) copyIdBtn.gameObject.SetActive(!string.IsNullOrEmpty(pid));
        }
        if (nickText != null && !editingNick)
        {
            string nick = RankingService.CurrentName;
            nickText.text = string.IsNullOrEmpty(nick) ? "" : Loc.T("acc.nickname") + "<b>" + nick + "</b>";
            if (nickBtn != null)
            {
                nickBtn.gameObject.SetActive(signedIn && !string.IsNullOrEmpty(nick));
                nickBtn.interactable = !busy;
            }
        }
    }

    // ── 닉네임 인라인 편집 ──────────────────────────────────
    void OnNickButton()
    {
        if (busy) return;
        if (!editingNick) BeginNickEdit();
        else _ = SaveNickAsync();
    }

    void BeginNickEdit()
    {
        editingNick = true;
        nickText.gameObject.SetActive(false);
        Text label = nickBtn.GetComponentInChildren<Text>();
        if (label != null) label.text = Loc.T("acc.save");

        var go = new GameObject("NickInput", typeof(RectTransform), typeof(Image), typeof(InputField));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(nickText.rectTransform.parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(320f, 46f);
        rt.anchoredPosition = new Vector2(-40f, -122f);
        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.09f, 0.1f, 0.15f, 0.95f);

        var tgo = new GameObject("Text", typeof(RectTransform));
        tgo.transform.SetParent(rt, false);
        Text t = tgo.AddComponent<Text>();
        t.font = UiFont(); t.fontSize = 20; t.alignment = TextAnchor.MiddleLeft;
        t.color = new Color(0.9f, 0.95f, 1f); t.supportRichText = false;
        RectTransform trt = t.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12f, 4f); trt.offsetMax = new Vector2(-12f, -4f);

        nickInput = go.GetComponent<InputField>();
        nickInput.textComponent = t;
        nickInput.characterLimit = 16;
        string cur = RankingService.CurrentName;
        int hash = cur.IndexOf('#');
        nickInput.text = hash > 0 ? cur.Substring(0, hash) : cur; // #태그는 서버가 다시 붙임
        nickInput.ActivateInputField();
    }

    async System.Threading.Tasks.Task SaveNickAsync()
    {
        if (nickInput == null) return;
        busy = true;
        try
        {
            string err = await RankingService.SetNameAsync(nickInput.text);
            if (err != null) { ShowFeedback(err); return; } // 편집 상태 유지, 재시도 가능
            ShowFeedback(Loc.T("acc.msgNickSaved"), true);
            EndNickEdit();
        }
        finally { busy = false; RefreshStatus(); }
    }

    void EndNickEdit()
    {
        editingNick = false;
        if (nickInput != null) { Destroy(nickInput.gameObject); nickInput = null; }
        nickText.gameObject.SetActive(true);
        Text label = nickBtn.GetComponentInChildren<Text>();
        if (label != null) label.text = Loc.T("acc.change");
    }

    static string CurrentPlayerId()
    {
        try { return ServicesBootstrap.IsSignedIn ? AuthenticationService.Instance.PlayerId : ""; }
        catch { return ""; }
    }

    void OnCopyId()
    {
        string pid = CurrentPlayerId();
        if (string.IsNullOrEmpty(pid)) return;
        ClipboardUtil.Copy(pid);
        ShowFeedback(Loc.T("acc.msgIdCopied"), true);
    }

    // 게스트 → 로그인 선택 화면. 지금 게스트 계정은 복구 불가라 2번 클릭으로 확인.
    void OnSwitchAccount()
    {
        if (busy) return;
        if (!switchConfirm)
        {
            switchConfirm = true;
            Text label = switchBtn.GetComponentInChildren<Text>();
            if (label != null) label.text = Loc.T("acc.warnSwitch");
            ShowFeedback(Loc.T("acc.warnSwitch2"));
            return;
        }
        OnLogout(); // 세션 토큰 제거 → 로그인 화면(게스트/Google 선택)
    }

    async void OnLinkGoogle()
    {
        if (busy) return;
        busy = true; RefreshStatus();
        try
        {
            string err = await ServicesBootstrap.LinkGoogleAsync();
            await AccountService.RefreshAsync();
            if (AccountService.IsLinked) ShowFeedback(Loc.T("acc.msgLinked"), true);
            else if (!string.IsNullOrEmpty(err)) ShowFeedback(err);
        }
        finally { busy = false; RefreshStatus(); }
    }

    void OnLogout()
    {
        if (busy) return;
        try
        {
            AuthenticationService.Instance.SignOut(true); // 세션 토큰까지 제거
        }
        catch (Exception e) { Debug.LogWarning("[Account] 로그아웃 예외: " + e.Message); }
        SceneLoader.Load("LoginScene");
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
        if (uiFont == null) uiFont = Resources.Load<Font>("Fonts/Pretendard-Regular"); // 한글 폰트 — WebGL은 OS 폰트 폴백이 없어 내장 필수
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

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    RectTransform MakeRect(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        return rt;
    }

    RectTransform MakePanel(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        Image img = MakeImage(parent, name, pos, size, PANEL);
        Sprite pixel = Resources.Load<Sprite>("UI/LabDossierPanel"); // PixelLab 패널 프레임
        if (pixel != null)
        {
            img.sprite = pixel;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        return img.rectTransform;
    }

    Image MakeImage(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform rt = MakeRect(parent, name, pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = WhiteSprite();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    Text MakeText(RectTransform parent, string name, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, string text, Color color)
    {
        RectTransform rt = MakeRect(parent, name, pos, size);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = UiFont(); t.fontSize = fontSize; t.alignment = align;
        t.supportRichText = true; t.text = text; t.color = color; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    Button MakeButton(RectTransform parent, string name, Vector2 pos, Vector2 size, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        Image img = MakeImage(parent, name, pos, size, color);
        img.raycastTarget = true;
        Sprite pixel = Resources.Load<Sprite>("UI/SubjectNullButton"); // PixelLab 버튼 프레임
        if (pixel != null)
        {
            img.sprite = pixel;
            img.type = Image.Type.Sliced;
            img.color = Color.Lerp(Color.white, color, 0.35f);
        }
        Button btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        MakeText(img.rectTransform, "Label", Vector2.zero, new Vector2(size.x - 20f, size.y), Mathf.RoundToInt(size.y * 0.36f), TextAnchor.MiddleCenter, label, new Color(0.9f, 0.95f, 1f));
        return btn;
    }
}
