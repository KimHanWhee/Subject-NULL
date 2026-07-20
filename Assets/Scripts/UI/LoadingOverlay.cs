using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// 게임 전역 로딩 오버레이(반투명). 현재 씬 위를 어둡게 덮고 "로딩 중" + 스피너를 표시한다.
// 씬 전환/가챠 등 비동기 작업에 공용으로 재사용. DontDestroyOnLoad 싱글턴, 최초 사용 시 지연 생성.
public class LoadingOverlay : MonoBehaviour
{
    static LoadingOverlay instance;

    Canvas canvas;
    CanvasGroup group;
    RectTransform spinner;
    Text titleText;
    bool visible;
    float dotTimer;
    int dotCount;
    Coroutine fadeCo;

    static readonly Color DIM = new Color(0f, 0f, 0f, 0.62f); // 뒤 화면이 살짝 비치는 반투명
    static readonly Color ACCENT = new Color(1f, 0.86f, 0.4f);
    static Font uiFont;
    static Sprite whiteSprite;
    static Sprite spinnerSprite;

    public static LoadingOverlay Get()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("LoadingOverlay");
            instance = go.AddComponent<LoadingOverlay>();
            DontDestroyOnLoad(go);
            instance.Build();
        }
        return instance;
    }

    // 씬 전환: 오버레이를 띄운 채 대상 씬을 비동기 로드 → 새 씬 위에서 오버레이 페이드아웃.
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        StartCoroutine(LoadRoutine(sceneName));
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        Show("로딩 중");
        Time.timeScale = 1f; // 이전 씬이 슬로우/일시정지였어도 로딩은 정상 속도
        float t0 = Time.unscaledTime;
        yield return null; // 오버레이 한 프레임 렌더 후 로드 시작

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        while (Time.unscaledTime - t0 < 0.35f) yield return null; // 최소 표시(깜빡임 방지)
        yield return null; // 새 씬 첫 프레임까지 덮은 뒤
        Hide();
    }

    public void Show(string message)
    {
        if (titleText != null) titleText.text = message;
        visible = true;
        if (canvas != null) canvas.enabled = true;
        if (group != null) group.blocksRaycasts = true; // 로딩 중 뒤 클릭 차단
        StartFade(1f);
    }

    public void Hide()
    {
        visible = false;
        if (group != null) group.blocksRaycasts = false;
        StartFade(0f);
    }

    void StartFade(float target)
    {
        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(Fade(target));
    }

    IEnumerator Fade(float target)
    {
        float start = group.alpha;
        float t = 0f, dur = 0.12f;
        while (t < dur) { t += Time.unscaledDeltaTime; group.alpha = Mathf.Lerp(start, target, t / dur); yield return null; }
        group.alpha = target;
        if (target <= 0f && canvas != null) canvas.enabled = false;
        fadeCo = null;
    }

    void Update()
    {
        if (!visible) return;
        if (spinner != null) spinner.Rotate(0f, 0f, -240f * Time.unscaledDeltaTime);
        dotTimer += Time.unscaledDeltaTime;
        if (dotTimer >= 0.35f)
        {
            dotTimer = 0f;
            dotCount = (dotCount + 1) % 4;
            if (titleText != null) titleText.text = "로딩 중" + new string('.', dotCount);
        }
    }

    void Build()
    {
        GameObject canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000; // 항상 최상단
        CanvasScaler sc = canvasGo.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        group = canvasGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        RectTransform root = canvas.GetComponent<RectTransform>();

        Image dim = MakeImage(root, "Dim", Vector2.zero, new Vector2(1920f, 1080f), DIM);
        dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = Vector2.zero; dim.rectTransform.offsetMax = Vector2.zero;
        dim.raycastTarget = true;

        Image sp = MakeImage(root, "Spinner", new Vector2(0f, 46f), new Vector2(78f, 78f), ACCENT);
        sp.sprite = SpinnerSprite();
        spinner = sp.rectTransform;

        titleText = MakeText(root, "Title", new Vector2(0f, -34f), new Vector2(600f, 44f), 30, TextAnchor.MiddleCenter, "로딩 중", new Color(0.92f, 0.93f, 0.98f));

        canvas.enabled = false;
    }

    // 회전 스피너용 호(annulus with gap) 스프라이트 — 반투명 배경 위에서도 깔끔하게 보임.
    static Sprite SpinnerSprite()
    {
        if (spinnerSprite != null) return spinnerSprite;
        const int S = 64;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 c = new Vector2((S - 1) * 0.5f, (S - 1) * 0.5f);
        float half = S * 0.5f;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                Vector2 d = new Vector2(x, y) - c;
                float r = d.magnitude / half;
                float a = 0f;
                if (r >= 0.6f && r <= 0.95f)
                {
                    float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                    float a360 = ang < 0f ? ang + 360f : ang;
                    if (a360 >= 50f && a360 <= 310f) { float p = (a360 - 50f) / 260f; a = p * p; } // 꼬리 투명→머리 진함(코멧)
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        spinnerSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
        return spinnerSprite;
    }

    // ── UI 프리미티브 ──
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

    static Image MakeImage(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        Image img = go.AddComponent<Image>();
        img.sprite = WhiteSprite();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Text MakeText(RectTransform parent, string name, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, string text, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        Text t = go.AddComponent<Text>();
        t.font = UiFont(); t.fontSize = fontSize; t.alignment = align; t.supportRichText = true;
        t.text = text; t.color = color; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }
}
