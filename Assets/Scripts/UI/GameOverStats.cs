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
        MakeText(canvasGo.transform, font, "점수  " + GameStats.lastScore, 84, new Vector2(0f, 300f), Color.white, FontStyles.Bold);
        MakeText(canvasGo.transform, font, "최고기록  " + GameStats.bestScore, 52, new Vector2(0f, 210f),
                 new Color(1f, 0.85f, 0.35f), FontStyles.Normal);
        if (nb)
            MakeText(canvasGo.transform, font, "★ 신기록! ★", 46, new Vector2(0f, 150f),
                     new Color(1f, 0.5f, 0.5f), FontStyles.Bold);
    }

    void MakeText(Transform parent, TMP_FontAsset font, string text, float size, Vector2 pos, Color color, FontStyles style)
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
    }
}
