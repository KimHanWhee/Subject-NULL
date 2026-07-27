using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// 자산을 소모하는 행동(뽑기·조각 교환·GEM 구매) 직전에 한 번 더 확인받는 공용 팝업.
//
// 왜 공용으로 두는가:
//   확인 절차를 각 화면에서 따로 만들면 어떤 곳은 빠지거나 문구·동작이 제각각이 된다.
//   "자산이 나가는 지점은 전부 같은 창을 거친다"는 규칙을 코드 한 곳에서 보장한다.
//
// 사용:
//   bool ok = await ConfirmDialog.ShowAsync(canvasRoot, "제목", "본문", "확인", "취소");
//   if (!ok) return;
//
// 주의: 결제(현금) 확인은 표시 내용에 법적 고지가 들어가야 하므로 notice 인자를 함께 쓴다.
public static class ConfirmDialog
{
    static Font uiFont;
    static Sprite whiteSprite;

    // 반환: 확인 true / 취소·배경클릭 false
    public static Task<bool> ShowAsync(RectTransform canvasRoot, string title, string body,
                                       string confirmLabel, string cancelLabel, string notice = null)
    {
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        if (canvasRoot == null) { tcs.SetResult(false); return tcs.Task; }

        bool hasNotice = !string.IsNullOrEmpty(notice);
        // 고지가 붙는 결제 창은 5줄 + 영어 줄바꿈까지 들어가므로 넉넉히 잡는다.
        float panelH = hasNotice ? 560f : 340f;

        // 배경 — 뒤쪽 UI 클릭을 막는다(확인 전에 다른 조작이 끼어들면 안 된다)
        Image dim = MakeImage(canvasRoot, "ConfirmDim", Vector2.zero, new Vector2(1920f, 1080f), new Color(0f, 0f, 0f, 0.82f));
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        RectTransform panel = MakeImage(dim.rectTransform, "ConfirmPanel", Vector2.zero, new Vector2(760f, panelH),
                                        new Color(0.10f, 0.12f, 0.15f, 0.98f)).rectTransform;

        MakeText(panel, "Title", new Vector2(0f, panelH * 0.5f - 58f), new Vector2(700f, 54f),
                 30, TextAnchor.MiddleCenter, title, new Color(1f, 0.86f, 0.42f));

        MakeText(panel, "Body", new Vector2(0f, hasNotice ? 125f : 18f), new Vector2(680f, hasNotice ? 90f : 150f),
                 22, TextAnchor.UpperCenter, body, Color.white);

        if (hasNotice)
        {
            // 고지 문구 — 결제 전 안내처럼 "읽었다"는 근거가 필요한 내용을 담는다
            Image box = MakeImage(panel, "NoticeBox", new Vector2(0f, -70f), new Vector2(700f, 230f),
                                  new Color(1f, 1f, 1f, 0.06f));
            MakeText(box.rectTransform, "Notice", Vector2.zero, new Vector2(670f, 218f),
                     17, TextAnchor.UpperLeft, notice, new Color(0.78f, 0.82f, 0.88f));
        }

        float by = -panelH * 0.5f + 56f;
        MakeButton(panel, "Cancel", new Vector2(-170f, by), new Vector2(280f, 62f), cancelLabel,
                   new Color(0.30f, 0.32f, 0.36f), delegate { Close(dim, tcs, false); });
        MakeButton(panel, "Confirm", new Vector2(170f, by), new Vector2(280f, 62f), confirmLabel,
                   new Color(0.20f, 0.50f, 0.42f), delegate { Close(dim, tcs, true); });

        return tcs.Task;
    }

    static void Close(Image dim, TaskCompletionSource<bool> tcs, bool result)
    {
        if (dim != null) Object.Destroy(dim.gameObject);
        if (!tcs.Task.IsCompleted) tcs.SetResult(result);
    }

    // ── UI 빌더 (GachaShopUI의 절차적 생성 방식과 동일 규약) ──

    static Font UiFont()
    {
        if (uiFont == null)
        {
            // 한글 폰트 — WebGL은 OS 폰트 폴백이 없어 내장 필수(GachaShopUI와 동일 경로)
            uiFont = Resources.Load<Font>("Fonts/Pretendard-Regular");
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        return uiFont;
    }

    static Sprite WhiteSprite()
    {
        if (whiteSprite == null)
        {
            Texture2D t = Texture2D.whiteTexture;
            whiteSprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
        }
        return whiteSprite;
    }

    static RectTransform MakeRect(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
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

    static Text MakeText(RectTransform parent, string name, Vector2 pos, Vector2 size, int fontSize,
                         TextAnchor align, string text, Color color)
    {
        RectTransform rt = MakeRect(parent, name, pos, size);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = UiFont(); t.fontSize = fontSize; t.alignment = align; t.supportRichText = true;
        t.text = text; t.color = color; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static Button MakeButton(RectTransform parent, string name, Vector2 pos, Vector2 size, string label,
                             Color color, UnityEngine.Events.UnityAction onClick)
    {
        Image img = MakeImage(parent, name, pos, size, color);
        img.raycastTarget = true;
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors;
        cb.highlightedColor = new Color(0.82f, 0.96f, 1f);
        cb.pressedColor = new Color(0.66f, 0.86f, 0.96f);
        cb.fadeDuration = 0.1f;
        b.colors = cb;
        if (onClick != null) b.onClick.AddListener(onClick);
        MakeText(img.rectTransform, "Label", Vector2.zero, size, 20, TextAnchor.MiddleCenter, label, Color.white);
        return b;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
