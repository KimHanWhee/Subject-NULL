using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// GameOverScene 진입 시 이번 점수 + 계정 최고기록을 표시(코드 생성, 씬 편집 불필요).
public class GameOverStats : MonoBehaviour
{
    const string Scene = "GameOverScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded += (s, _) => { if (s.name == Scene) Create(); };
        if (SceneManager.GetActiveScene().name == Scene) Create();
    }

    static void Create()
    {
        if (FindFirstObjectByType<GameOverStats>() != null) return;
        new GameObject("GameOverStats").AddComponent<GameOverStats>();
    }

    void Start()
    {
        var font = Resources.Load<TMP_FontAsset>("Fonts/KoreanSDF");

        var canvasGo = new GameObject("GameOverStatsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3000;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        bool nb = GameStats.isNewBest;
        MakeText(canvasGo.transform, font, Loc.T("over.score", GameStats.lastScore), 84, new Vector2(0f, 300f), Color.white, FontStyles.Bold);
        MakeText(canvasGo.transform, font, Loc.T("over.best", GameStats.bestScore), 52, new Vector2(0f, 210f),
                 new Color(1f, 0.85f, 0.35f), FontStyles.Normal);
        if (nb)
            MakeText(canvasGo.transform, font, Loc.T("over.newBest"), 46, new Vector2(0f, 150f),
                     new Color(1f, 0.5f, 0.5f), FontStyles.Bold);

        BuildRankingPanel(canvasGo.transform, font);
    }

    // ── 글로벌 랭킹(우측 패널, UGS Leaderboards) ─────────────
    void BuildRankingPanel(Transform canvasRoot, TMP_FontAsset font)
    {
        var panelGo = new GameObject("RankingPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvasRoot, false);
        var img = panelGo.GetComponent<Image>();
        var sprite = Resources.Load<Sprite>("UI/LabDossierPanel"); // 연구실 도시어 크롬
        if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; img.color = Color.white; }
        else img.color = new Color(0.1f, 0.11f, 0.16f, 0.92f);
        img.raycastTarget = false;
        var rt = panelGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(470f, 640f);
        rt.anchoredPosition = new Vector2(620f, -70f);

        MakeText(panelGo.transform, font, Loc.T("rank.title"), 34, new Vector2(0f, 275f),
                 new Color(1f, 0.86f, 0.4f), FontStyles.Bold);
        LoadRanking(panelGo.transform, font);
    }

    async void LoadRanking(Transform panel, TMP_FontAsset font)
    {
        var loading = MakeText(panel, font, Loc.T("rank.loading"), 22, Vector2.zero,
                               new Color(0.6f, 0.65f, 0.75f), FontStyles.Normal);
        await ServicesBootstrap.WaitSignedInAsync();
        var top = await RankingService.GetTopAsync(10);
        var me = await RankingService.GetMyEntryAsync();
        if (this == null || panel == null) return; // 씬 이탈 안전망
        Destroy(loading.gameObject);

        if (top == null)
        {
            MakeText(panel, font, Loc.T("rank.failed"), 22, Vector2.zero,
                     new Color(0.75f, 0.55f, 0.55f), FontStyles.Normal);
            return;
        }
        if (top.Count == 0)
        {
            MakeText(panel, font, Loc.T("rank.empty"), 22, Vector2.zero,
                     new Color(0.6f, 0.65f, 0.75f), FontStyles.Normal);
            return;
        }

        float y = 220f;
        const float step = 44f;
        for (int i = 0; i < top.Count; i++)
        {
            var e = top[i];
            Color rankCol = e.rank == 1 ? new Color(1f, 0.84f, 0.3f)
                          : e.rank == 2 ? new Color(0.8f, 0.85f, 0.9f)
                          : e.rank == 3 ? new Color(0.85f, 0.6f, 0.4f)
                          : new Color(0.6f, 0.65f, 0.75f);
            Color nameCol = e.isMe ? new Color(0.6f, 1f, 0.7f) : Color.white;
            var style = e.isMe ? FontStyles.Bold : FontStyles.Normal;

            var rank = MakeText(panel, font, e.rank.ToString(), 24, new Vector2(-175f, y), rankCol, FontStyles.Bold);
            rank.alignment = TextAlignmentOptions.Center;
            rank.rectTransform.sizeDelta = new Vector2(60f, 36f);
            var name = MakeText(panel, font, e.name + (e.isMe ? " ◀" : ""), 22, new Vector2(-15f, y), nameCol, style);
            name.alignment = TextAlignmentOptions.Left;
            name.rectTransform.sizeDelta = new Vector2(230f, 36f);
            var score = MakeText(panel, font, e.score.ToString("N0"), 22, new Vector2(145f, y), nameCol, style);
            score.alignment = TextAlignmentOptions.Right;
            score.rectTransform.sizeDelta = new Vector2(120f, 36f);
            y -= step;
        }

        // 내 순위(TOP 10 밖일 때 하단 별도 표시)
        if (me != null)
        {
            bool inTop = false;
            foreach (var e in top) if (e.isMe) { inTop = true; break; }
            if (!inTop)
                MakeText(panel, font, Loc.T("rank.myRank", me.rank, me.score.ToString("N0")), 24,
                         new Vector2(0f, -272f), new Color(0.6f, 1f, 0.7f), FontStyles.Bold);
        }
    }

    TextMeshProUGUI MakeText(Transform parent, TMP_FontAsset font, string text, float size, Vector2 pos, Color color, FontStyles style)
    {
        var go = new GameObject("Txt", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(900f, size * 1.5f);
        rt.anchoredPosition = pos;
        return t;
    }
}
