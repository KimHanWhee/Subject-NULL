using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// 부트/타이틀 씬 게이트.
// - 이전 세션이 자동 재개되면: "Press Any Key to Start" → 아무 입력이면 메인 메뉴.
// - 첫 방문(세션 없음)이면: [게스트로 시작] [Google로 시작] 선택 버튼 표시.
public class LoginGate : MonoBehaviour
{
    [SerializeField] private string nextScene = "MainMenuScene";
    [SerializeField] private Text promptText;                 // 상태/안내 문구
    [SerializeField] private float failTimeout = 10f;         // 이 시간 넘게 초기화 안 되면 실패 처리

    [Header("Messages")]
    [SerializeField] private string connectingMsg = "연결 중...";
    [SerializeField] private string readyMsg = "Press Any Key to Start";
    [SerializeField] private string failMsg = "연결 실패 — 아무 키나 눌러 재시도";

    enum Phase { Connecting, Choice, SigningIn, Ready, Failed }
    Phase phase = Phase.Connecting;
    float elapsed;
    GameObject choiceRoot;
    GameObject promptBox; // 프롬프트 배경 박스 — 선택 화면에선 비어 보이므로 숨김
    Text feedbackText;
    static Font uiFont;

    void Awake()
    {
        promptBox = GameObject.Find("PromptBG");
    }

    void Update()
    {
        switch (phase)
        {
            case Phase.Connecting:
                if (ServicesBootstrap.IsSignedIn) { EnterReady(); return; }
                // 초기화됐고 재개할 세션도 없으면 → 로그인 방식 선택.
                // 세션 재개가 진행 중이면(토큰 보유) 완료까지 대기해 버튼 깜빡임/오표시 방지.
                if (ServicesBootstrap.IsInitialized && !ServicesBootstrap.HasCachedSession) { EnterChoice(); return; }
                elapsed += Time.unscaledDeltaTime;
                if (elapsed >= failTimeout)
                {
                    // 세션 재개 실패(만료 등) — 초기화만 됐다면 선택 화면으로 복귀, 아니면 재시도 안내
                    if (ServicesBootstrap.IsInitialized) EnterChoice();
                    else { phase = Phase.Failed; SetPrompt(failMsg); }
                }
                else SetPrompt(connectingMsg);
                return;

            case Phase.Choice:
                if (ServicesBootstrap.IsSignedIn) EnterReady(); // 뒤늦은 세션 재개 안전망
                return;

            case Phase.SigningIn:
                SetPrompt(connectingMsg);
                return;

            case Phase.Ready:
                if (AnyInput()) SceneLoader.Load(nextScene);
                return;

            case Phase.Failed:
                if (AnyInput()) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // 부트 씬 재시작 = 재시도
                return;
        }
    }

    void EnterReady()
    {
        phase = Phase.Ready;
        if (choiceRoot != null) choiceRoot.SetActive(false);
        if (promptBox != null) promptBox.SetActive(true);
        SetPrompt(readyMsg);
    }

    void EnterChoice()
    {
        phase = Phase.Choice;
        SetPrompt("");
        if (promptBox != null) promptBox.SetActive(false);
        if (choiceRoot == null) BuildChoiceButtons();
        choiceRoot.SetActive(true);
    }

    async void OnGuest()
    {
        if (phase != Phase.Choice) return;
        BeginSignIn();
        string err = await ServicesBootstrap.SignInGuestAsync();
        EndSignIn(err);
    }

    async void OnGoogle()
    {
        if (phase != Phase.Choice) return;
        BeginSignIn();
        string err = await ServicesBootstrap.SignInGoogleAsync();
        EndSignIn(err);
    }

    void BeginSignIn()
    {
        phase = Phase.SigningIn;
        if (choiceRoot != null) choiceRoot.SetActive(false);
        ShowFeedback("");
    }

    void EndSignIn(string err)
    {
        if (ServicesBootstrap.IsSignedIn) { EnterReady(); return; }
        EnterChoice();
        ShowFeedback(string.IsNullOrEmpty(err) ? "" : err);
    }

    bool AnyInput()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        return (kb != null && kb.anyKey.wasPressedThisFrame) ||
               (mouse != null && mouse.leftButton.wasPressedThisFrame);
    }

    void SetPrompt(string s)
    {
        if (promptText != null && promptText.text != s) promptText.text = s;
    }

    void ShowFeedback(string s)
    {
        if (feedbackText != null) feedbackText.text = s;
    }

    // ── 선택 버튼(코드 생성 — 프롬프트와 같은 캔버스에 배치) ──

    void BuildChoiceButtons()
    {
        // 이 씬엔 EventSystem이 없음(기존엔 키 입력만 받았음) — 버튼 클릭용으로 생성
        if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        RectTransform parent = promptText != null
            ? promptText.canvas.GetComponent<RectTransform>()
            : null;
        choiceRoot = new GameObject("LoginChoice", typeof(RectTransform));
        RectTransform root = choiceRoot.GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        // 프롬프트 문구 자리 근처(중앙 하단부)
        root.anchoredPosition = promptText != null
            ? promptText.rectTransform.anchoredPosition
            : new Vector2(0f, -220f);
        root.sizeDelta = new Vector2(760f, 200f);

        MakeButton(root, "GuestBtn", new Vector2(-190f, 20f), new Vector2(340f, 76f), "게스트로 시작", OnGuest);
        MakeButton(root, "GoogleBtn", new Vector2(190f, 20f), new Vector2(340f, 76f), "Google로 시작", OnGoogle);

        GameObject fb = new GameObject("Feedback", typeof(RectTransform));
        RectTransform frt = fb.GetComponent<RectTransform>();
        frt.SetParent(root, false);
        frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.sizeDelta = new Vector2(760f, 36f);
        frt.anchoredPosition = new Vector2(0f, -50f);
        feedbackText = fb.AddComponent<Text>();
        feedbackText.font = UiFont();
        feedbackText.fontSize = 20;
        feedbackText.alignment = TextAnchor.MiddleCenter;
        feedbackText.color = new Color(1f, 0.6f, 0.55f);
        feedbackText.raycastTarget = false;
        feedbackText.text = "";
    }

    void MakeButton(RectTransform parent, string name, Vector2 pos, Vector2 size, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;

        Image img = go.AddComponent<Image>();
        Sprite pixel = Resources.Load<Sprite>("UI/SubjectNullButton"); // PixelLab 버튼 프레임(9-슬라이스)
        if (pixel != null) { img.sprite = pixel; img.type = Image.Type.Sliced; }
        else img.color = new Color(0.16f, 0.2f, 0.3f, 0.95f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        GameObject tg = new GameObject("Label", typeof(RectTransform));
        RectTransform trt = tg.GetComponent<RectTransform>();
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        Text t = tg.AddComponent<Text>();
        t.font = UiFont();
        t.fontSize = 27;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.88f, 0.95f, 1f);
        t.raycastTarget = false;
        t.text = label;
    }

    static Font UiFont()
    {
        if (uiFont == null) uiFont = Resources.Load<Font>("Fonts/Pretendard-Regular"); // 한글 폰트 — WebGL은 OS 폰트 폴백이 없어 내장 필수
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return uiFont;
    }
}
