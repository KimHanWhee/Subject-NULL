using System.Text;
using UnityEngine;
using TMPro;

// 누적된 고난을 화면 좌상단에 상시 표시.
// 이게 없으면 플레이어가 "왜 갑자기 어려워졌는지" 알 수 없어 학습이 불가능하다.
// GameScene에 배선할 필요 없이 자동 생성된다(프로젝트의 런타임 UI 관례).
public class HardshipHUD : MonoBehaviour
{
    TextMeshProUGUI label;
    readonly StringBuilder sb = new StringBuilder();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, _) =>
        {
            if (s.name != "GameScene") return;
            if (FindFirstObjectByType<HardshipHUD>() != null) return;
            new GameObject("HardshipHUD").AddComponent<HardshipHUD>();
        };
    }

    void Start()
    {
        GameObject canvasGo = new GameObject("HardshipHudCanvas",
            typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3000; // HUD 층(선택 UI 4200보다 아래)
        UnityEngine.UI.CanvasScaler sc = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
        sc.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);

        GameObject go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(canvasGo.transform, false);
        label = go.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset f = Resources.Load<TMP_FontAsset>("Fonts/KoreanSDF");
        if (f != null) label.font = f;
        label.fontSize = 20f;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.richText = true;
        label.raycastTarget = false;

        RectTransform rt = label.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(520f, 200f);
        rt.anchoredPosition = new Vector2(28f, -96f); // 좌상단 HP 하트 아래

        HardshipSystem.OnChanged += Refresh;
        Refresh();
    }

    void OnDestroy() { HardshipSystem.OnChanged -= Refresh; }

    void Refresh()
    {
        if (label == null) return;
        if (HardshipSystem.TotalStacks == 0) { label.text = ""; return; }

        sb.Length = 0;
        sb.Append("<color=#FF8A7A><b>고난</b></color>  ");
        for (int i = 0; i < HardshipSystem.Count; i++)
        {
            HardshipId id = (HardshipId)i;
            int n = HardshipSystem.Stack(id);
            if (n <= 0) continue;
            HardshipSystem.Def d = HardshipSystem.GetDef(id);
            sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(d.color)).Append('>')
              .Append(d.name).Append("</color> ×").Append(n).Append("   ");
        }
        label.text = sb.ToString();
    }
}
