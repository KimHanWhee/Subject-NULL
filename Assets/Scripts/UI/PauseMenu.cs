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

        BuildPauseButton(canvasGo.transform); // 우측 상단 상시 노출(패널 밖 — 정지 중에도 위치 유지)

        // 어두운 배경막
        panel = new GameObject("Dim", typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        var dim = panel.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.72f);
        dim.raycastTarget = true; // 뒤 HUD 클릭 차단
        var drt = dim.rectTransform;
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;

        // 중앙 도시어 패널(연구실 크롬) — 없으면 기존 무패널 레이아웃으로 폴백
        Transform content = panel.transform;
        var panelSprite = Resources.Load<Sprite>("UI/LabDossierPanel");
        if (panelSprite != null)
        {
            var box = new GameObject("LabPanel", typeof(Image));
            box.transform.SetParent(panel.transform, false);
            var bi = box.GetComponent<Image>();
            bi.sprite = panelSprite;
            bi.type = Image.Type.Sliced;
            bi.raycastTarget = false;
            var brt = bi.rectTransform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(560f, 620f);
            brt.anchoredPosition = Vector2.zero;
            content = box.transform;
        }

        // 제목 + 실험실 플레이버
        var title = MakeText(content, "일시정지", 60, new Vector2(0f, 200f));
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(1f, 0.86f, 0.4f);
        var sub = MakeText(content, "── 실험 일시 중단 ──", 22, new Vector2(0f, 142f));
        sub.color = new Color(0.55f, 0.75f, 0.8f);

        MakeButton(content, "재개", new Vector2(0f, 40f), new Color(0.30f, 0.62f, 0.35f), Resume);
        MakeButton(content, "다시시작", new Vector2(0f, -75f), new Color(0.32f, 0.44f, 0.66f), Restart);
        MakeButton(content, "메인메뉴", new Vector2(0f, -190f), new Color(0.55f, 0.32f, 0.34f), MainMenu);
    }

    // 우측 상단 일시정지 버튼(⏸) — 클릭 = Esc와 동일 토글. 점수(우상단 텍스트) 바로 위 코너.
    void BuildPauseButton(Transform canvasRoot)
    {
        var go = new GameObject("PauseBtn", typeof(Image), typeof(Button));
        go.transform.SetParent(canvasRoot, false);
        var img = go.GetComponent<Image>();
        var btnSprite = Resources.Load<Sprite>("UI/SubjectNullButton");
        if (btnSprite != null)
        {
            img.sprite = btnSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.85f, 0.95f, 1f, 0.9f);
        }
        else img.color = new Color(0.16f, 0.2f, 0.3f, 0.85f);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f); // 화면 우상단 고정(레터박스 대응)
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(64f, 64f);
        rt.anchoredPosition = new Vector2(-48f, -42f);

        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(Toggle);
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.75f, 0.95f, 1f);
        colors.pressedColor = new Color(0.55f, 0.8f, 0.95f);
        colors.fadeDuration = 0.08f;
        btn.colors = colors;

        var t = MakeText(go.transform, "II", 26, Vector2.zero);
        t.fontStyle = FontStyles.Bold;
        t.color = new Color(0.85f, 0.95f, 1f);
        t.rectTransform.sizeDelta = rt.sizeDelta;
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
        var pixel = Resources.Load<Sprite>("UI/SubjectNullButton"); // 연구실 네온 버튼 크롬(덱/가챠와 통일)
        if (pixel != null)
        {
            img.sprite = pixel;
            img.type = Image.Type.Sliced;
            img.color = Color.Lerp(color, Color.white, 0.4f);
        }
        else img.color = color;
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
