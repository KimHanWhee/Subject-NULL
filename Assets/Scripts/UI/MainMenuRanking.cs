using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// 메인 메뉴 랭킹 — Account 버튼 아래 [랭킹] 버튼을 런타임 생성(씬 편집 불필요).
// 클릭 시 오버레이로 글로벌 TOP 10 + 내 순위 표시(게임오버 화면과 같은 데이터 소스).
public class MainMenuRanking : MonoBehaviour
{
    const string Scene = "MainMenuScene";

    private TMP_FontAsset font;
    private GameObject overlay;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded += (s, _) => { if (s.name == Scene) Create(); };
        if (SceneManager.GetActiveScene().name == Scene) Create();
    }

    static void Create()
    {
        if (FindFirstObjectByType<MainMenuRanking>() != null) return;
        new GameObject("MainMenuRanking").AddComponent<MainMenuRanking>();
    }

    void Start()
    {
        font = Resources.Load<TMP_FontAsset>("Fonts/KoreanSDF");
        StartCoroutine(AttachWhenReady());
    }

    // AccountButton이 늦게 준비되는 경우까지 커버 — 몇 초간 재시도(비활성 포함 탐색)
    IEnumerator AttachWhenReady()
    {
        float deadline = Time.unscaledTime + 6f;
        GameObject account = FindAccountButton();
        while (account == null && Time.unscaledTime < deadline)
        {
            yield return null;
            account = FindAccountButton();
        }
        if (account == null)
        {
            Debug.LogWarning("[MainMenuRanking] AccountButton을 찾지 못해 랭킹 버튼 생성 실패");
            yield break;
        }
        BuildButton(account);
    }

    static GameObject FindAccountButton()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform t = FindDeep(root.transform, "AccountButton");
            if (t != null) return t.gameObject;
        }
        return null;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            Transform r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }

    void BuildButton(GameObject account)
    {
        Transform parent = account.transform.parent;

        GameObject go = new GameObject("RankingButton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(220f, 64f);
        rt.anchoredPosition = new Vector2(-30f, -104f); // Account(-30,-30, h64) 바로 아래

        Image img = go.GetComponent<Image>();
        Sprite chrome = Resources.Load<Sprite>("UI/SubjectNullButton");
        if (chrome != null) { img.sprite = chrome; img.type = Image.Type.Sliced; img.color = Color.white; }
        else img.color = new Color(0.2f, 0.24f, 0.34f, 0.95f);

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(ToggleOverlay);

        // 다른 메뉴 버튼과 동일한 호버 글로우/사운드.
        // MainMenuIntro는 이 버튼이 생기기 전에 Start()가 끝나므로 여기서 직접 붙인다.
        go.AddComponent<MenuButtonFx>().Init(Resources.Load<AudioClip>("Sounds/UIHover"));

        // 라벨은 생성 시점에 직접 번역해 넣는다.
        // (외부 로컬라이저가 나중에 훑는 방식은 이 버튼이 늦게 생성되면 놓친다)
        TMPro.TextMeshProUGUI lbl = MakeText(rt, Loc.T("menu.ranking"), 26, Vector2.zero,
            new Color(0.88f, 0.95f, 1f), FontStyles.Bold);
        lbl.rectTransform.sizeDelta = rt.sizeDelta;

        System.Action refresh = () => { if (lbl != null) lbl.text = Loc.T("menu.ranking"); };
        Loc.OnChanged += refresh;
        LocBinder.Attach(go, refresh);
    }

    void ToggleOverlay()
    {
        if (overlay != null) { Destroy(overlay); overlay = null; return; }
        BuildOverlay();
    }

    void BuildOverlay()
    {
        // 자체 캔버스(최상단) — 메뉴 연출/버튼 위에 뜨도록
        overlay = new GameObject("RankingOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4500;
        CanvasScaler sc = overlay.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);

        // 어두운 배경막(클릭 시 닫힘)
        GameObject dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform drt = (RectTransform)dimGo.transform;
        drt.SetParent(overlay.transform, false);
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
        Image dim = dimGo.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.75f);
        Button dimBtn = dimGo.GetComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(ToggleOverlay);

        // 도시어 패널
        GameObject panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        RectTransform prt = (RectTransform)panelGo.transform;
        prt.SetParent(overlay.transform, false);
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(560f, 700f);
        Image pImg = panelGo.GetComponent<Image>();
        Sprite dossier = Resources.Load<Sprite>("UI/LabDossierPanel");
        if (dossier != null) { pImg.sprite = dossier; pImg.type = Image.Type.Sliced; pImg.color = Color.white; }
        else pImg.color = new Color(0.1f, 0.11f, 0.16f, 0.97f);
        pImg.raycastTarget = true; // 패널 클릭이 뒤 배경막(닫기)으로 새지 않게

        MakeText(prt, Loc.T("rank.title"), 36, new Vector2(0f, 305f), new Color(1f, 0.86f, 0.4f), FontStyles.Bold);

        // 닫기
        GameObject closeGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform crt = (RectTransform)closeGo.transform;
        crt.SetParent(prt, false);
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(56f, 50f);
        crt.anchoredPosition = new Vector2(238f, 308f);
        Image cImg = closeGo.GetComponent<Image>();
        Sprite chrome = Resources.Load<Sprite>("UI/SubjectNullButton");
        if (chrome != null) { cImg.sprite = chrome; cImg.type = Image.Type.Sliced; }
        else cImg.color = new Color(0.3f, 0.3f, 0.38f);
        Button cBtn = closeGo.GetComponent<Button>();
        cBtn.targetGraphic = cImg;
        cBtn.onClick.AddListener(ToggleOverlay);
        MakeText(crt, "✕", 24, Vector2.zero, new Color(0.88f, 0.95f, 1f), FontStyles.Bold);

        LoadRanking(prt);
    }

    async void LoadRanking(Transform panel)
    {
        var loading = MakeText(panel, Loc.T("rank.loading"), 22, Vector2.zero,
                               new Color(0.6f, 0.65f, 0.75f), FontStyles.Normal);
        await ServicesBootstrap.WaitSignedInAsync();
        if (this == null || panel == null) return; // 오버레이 닫힘/씬 이탈 안전망

        if (!ServicesBootstrap.IsSignedIn)
        {
            // 에디터에서 MainMenuScene 직접 플레이 등 — 로그인 경유가 없으면 데이터 접근 불가
            Destroy(loading.gameObject);
            MakeText(panel, Loc.T("rank.needLogin"), 22, Vector2.zero,
                     new Color(0.75f, 0.65f, 0.55f), FontStyles.Normal);
            return;
        }

        var top = await RankingService.GetTopAsync(10);
        var me = await RankingService.GetMyEntryAsync();
        if (this == null || panel == null) return;
        Destroy(loading.gameObject);

        if (top == null)
        {
            MakeText(panel, Loc.T("rank.failed"), 22, Vector2.zero,
                     new Color(0.75f, 0.55f, 0.55f), FontStyles.Normal);
            return;
        }
        if (top.Count == 0)
        {
            MakeText(panel, Loc.T("rank.empty"), 22, Vector2.zero,
                     new Color(0.6f, 0.65f, 0.75f), FontStyles.Normal);
            return;
        }

        float y = 245f;
        const float step = 48f;
        for (int i = 0; i < top.Count; i++)
        {
            var e = top[i];
            Color rankCol = e.rank == 1 ? new Color(1f, 0.84f, 0.3f)
                          : e.rank == 2 ? new Color(0.8f, 0.85f, 0.9f)
                          : e.rank == 3 ? new Color(0.85f, 0.6f, 0.4f)
                          : new Color(0.6f, 0.65f, 0.75f);
            Color nameCol = e.isMe ? new Color(0.6f, 1f, 0.7f) : Color.white;
            var style = e.isMe ? FontStyles.Bold : FontStyles.Normal;

            var rank = MakeText(panel, e.rank.ToString(), 26, new Vector2(-205f, y), rankCol, FontStyles.Bold);
            rank.alignment = TextAlignmentOptions.Center;
            rank.rectTransform.sizeDelta = new Vector2(64f, 38f);
            var name = MakeText(panel, e.name + (e.isMe ? " ◀" : ""), 24, new Vector2(-25f, y), nameCol, style);
            name.alignment = TextAlignmentOptions.Left;
            name.rectTransform.sizeDelta = new Vector2(270f, 38f);
            var score = MakeText(panel, e.score.ToString("N0"), 24, new Vector2(175f, y), nameCol, style);
            score.alignment = TextAlignmentOptions.Right;
            score.rectTransform.sizeDelta = new Vector2(130f, 38f);
            y -= step;
        }

        // 내 순위(TOP 10 밖일 때 하단 별도 표시)
        if (me != null)
        {
            bool inTop = false;
            foreach (var e in top) if (e.isMe) { inTop = true; break; }
            if (!inTop)
                MakeText(panel, Loc.T("rank.myRank", me.rank, me.score.ToString("N0")), 26,
                         new Vector2(0f, -300f), new Color(0.6f, 1f, 0.7f), FontStyles.Bold);
        }
    }

    TextMeshProUGUI MakeText(Transform parent, string text, float size, Vector2 pos, Color color, FontStyles style)
    {
        GameObject go = new GameObject("Txt", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520f, size * 1.5f);
        rt.anchoredPosition = pos;
        return t;
    }
}
