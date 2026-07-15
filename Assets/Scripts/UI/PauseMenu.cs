using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;

// 게임 중 일시정지 메뉴 — GameScene 로드 시 스스로 생성(씬 편집 불필요).
// Esc로 토글, 시간 정지는 TimeController(PushHold 0)로 슬로우/스펠 홀드와 충돌 없이 처리.
// 버튼: 재개 / 다시시작 / 메인메뉴.
public class PauseMenu : MonoBehaviour
{
    const string ScenePlayable = "GameScene";

    private bool paused;
    private int holdHandle = -1;
    private GameObject panel;
    private TMP_FontAsset font; // 한글 지원 폰트(씬의 기존 TMP에서 확보)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded += (scene, _) => TryCreate(scene);
        TryCreate(SceneManager.GetActiveScene());
    }

    static void TryCreate(Scene scene)
    {
        if (scene.name != ScenePlayable) return;
        if (FindFirstObjectByType<PauseMenu>() != null) return;
        new GameObject("PauseMenu").AddComponent<PauseMenu>();
    }

    void Start()
    {
        EnsureEventSystem();
        BuildUI();
        panel.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Toggle();
    }

    void Toggle()
    {
        if (paused) Resume();
        else Pause();
    }

    void Pause()
    {
        paused = true;
        panel.SetActive(true);
        if (TimeController.Instance != null) holdHandle = TimeController.Instance.PushHold(0f);
        else Time.timeScale = 0f;
    }

    void Resume()
    {
        paused = false;
        panel.SetActive(false);
        ReleaseTime();
    }

    void ReleaseTime()
    {
        if (TimeController.Instance != null)
        {
            if (holdHandle != -1) { TimeController.Instance.PopHold(holdHandle); holdHandle = -1; }
        }
        else Time.timeScale = 1f;
    }

    void Restart()
    {
        ReleaseTime();
        Time.timeScale = 1f; // 로딩 전 확실히 정상화
        SceneLoader.Load("GameScene");
    }

    void MainMenu()
    {
        ReleaseTime();
        Time.timeScale = 1f;
        SceneLoader.Load("MainMenuScene");
    }

    void OnDisable()
    {
        // 안전망: 정지 상태로 씬을 떠나도 시간 복구
        if (paused) ReleaseTime();
    }

    // ---- UI 생성 (코드 기반, 프로젝트 관례) ----

    void BuildUI()
    {
        font = ResolveKoreanFont();
        var canvasGo = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4000; // 로딩 오버레이(5000)보다 아래, 게임 HUD보다 위
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // 어두운 배경막
        panel = new GameObject("Dim", typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        var dim = panel.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.72f);
        var drt = dim.rectTransform;
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;

        // 제목
        var title = MakeText(panel.transform, "일시정지", 88, new Vector2(0f, 220f));
        title.fontStyle = FontStyles.Bold;

        MakeButton(panel.transform, "재개", new Vector2(0f, 60f), new Color(0.30f, 0.62f, 0.35f), Resume);
        MakeButton(panel.transform, "다시시작", new Vector2(0f, -50f), new Color(0.32f, 0.44f, 0.66f), Restart);
        MakeButton(panel.transform, "메인메뉴", new Vector2(0f, -160f), new Color(0.55f, 0.32f, 0.34f), MainMenu);
    }

    TextMeshProUGUI MakeText(Transform parent, string text, float size, Vector2 anchoredPos)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font; // 한글 폰트(미지정 시 두부 현상)
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520f, size * 1.4f);
        rt.anchoredPosition = anchoredPos;
        return t;
    }

    void MakeButton(Transform parent, string label, Vector2 anchoredPos, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label, typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(360f, 92f);
        rt.anchoredPosition = anchoredPos;

        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);
        var colors = btn.colors;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        colors.fadeDuration = 0.08f;
        btn.colors = colors;

        var t = MakeText(go.transform, label, 40, Vector2.zero);
        t.rectTransform.sizeDelta = rt.sizeDelta;
    }

    // 한글 폰트 확보 — Resources의 KoreanSDF 우선(HUD 점수 폰트는 라틴 전용이라 한글이 두부됨).
    TMP_FontAsset ResolveKoreanFont()
    {
        var kr = Resources.Load<TMP_FontAsset>("Fonts/KoreanSDF");
        if (kr != null) return kr;
        foreach (var t in FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            if (t.font != null) return t.font;
        return TMP_Settings.defaultFontAsset;
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
        es.AddComponent<InputSystemUIInputModule>(); // 신 Input System용 모듈(구 StandaloneInputModule 불가)
    }
}
