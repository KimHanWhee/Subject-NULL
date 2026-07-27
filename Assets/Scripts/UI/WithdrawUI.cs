using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// 결제 내역 · 청약철회 화면(오버레이).
//
// 철회 가능 여부는 전적으로 서버(GetPurchaseHistory)가 판단한 값을 그대로 쓴다.
// 클라이언트가 다시 계산하면 규칙이 어긋나 "버튼은 켜졌는데 누르면 실패"가 나고,
// 조작 여지도 생긴다. 여기서는 표시와 확인만 담당한다.
public static class WithdrawUI
{
    static Font uiFont;
    static Sprite whiteSprite;

    public static async void Show(RectTransform canvasRoot)
    {
        if (canvasRoot == null) return;

        Image dim = MakeImage(canvasRoot, "WithdrawDim", Vector2.zero, new Vector2(1920f, 1080f), new Color(0f, 0f, 0f, 0.88f));
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        RectTransform panel = MakeImage(dim.rectTransform, "WithdrawPanel", Vector2.zero, new Vector2(980f, 720f),
                                        new Color(0.10f, 0.12f, 0.15f, 0.98f)).rectTransform;

        MakeText(panel, "Title", new Vector2(0f, 310f), new Vector2(900f, 54f), 32, TextAnchor.MiddleCenter,
                 "<b>" + Loc.T("wd.title") + "</b>", new Color(1f, 0.86f, 0.42f));
        MakeText(panel, "Note", new Vector2(0f, 264f), new Vector2(900f, 40f), 17, TextAnchor.MiddleCenter,
                 "<color=#8A93A6>" + Loc.T("wd.note") + "</color>", Color.white);

        Text status = MakeText(panel, "Status", new Vector2(0f, 0f), new Vector2(900f, 40f), 22, TextAnchor.MiddleCenter,
                               Loc.T("wd.loading"), new Color(0.7f, 0.75f, 0.85f));

        // 목록 영역(스크롤) — 결제가 쌓이면 넘칠 수 있다
        RectTransform viewport = MakeRect(panel, "Viewport", new Vector2(0f, -20f), new Vector2(900f, 480f));
        Image vpImg = viewport.gameObject.AddComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0.03f);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        RectTransform content = MakeRect(viewport, "Content", Vector2.zero, new Vector2(880f, 10f));
        content.anchorMin = new Vector2(0.5f, 1f); content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        scroll.content = content;
        scroll.viewport = viewport;

        // 자동 철회가 안 되는 건(사용함·기간 만료·오류)의 출구 — 없으면 사용자가 연락할 방법이 없다.
        MakeText(panel, "Support", new Vector2(0f, -284f), new Vector2(900f, 56f), 16, TextAnchor.MiddleCenter,
                 "<color=#7C8496>" + Loc.T("wd.support") + "</color>", Color.white);

        MakeButton(panel, "Close", new Vector2(0f, -334f), new Vector2(260f, 56f), Loc.T("common.close"),
                   new Color(0.30f, 0.32f, 0.36f), delegate { UnityEngine.Object.Destroy(dim.gameObject); });

        await Reload(canvasRoot, dim, content, status);
    }

    static async Task Reload(RectTransform canvasRoot, Image dim, RectTransform content, Text status)
    {
        // 기존 행 제거(철회 후 갱신용)
        for (int i = content.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(content.GetChild(i).gameObject);

        status.enabled = true;
        status.text = Loc.T("wd.loading");

        GachaService.PurchaseHistory hist = null;
        try { hist = await GachaService.GetPurchaseHistoryAsync(); }
        catch (Exception e) { Debug.LogError("[Withdraw] 내역 조회 실패: " + e); }

        if (dim == null || content == null) return; // 화면을 닫았으면 중단

        if (hist == null || hist.items == null || hist.items.Count == 0)
        {
            status.text = Loc.T("wd.empty");
            return;
        }
        status.enabled = false;

        const float rowH = 92f;
        content.sizeDelta = new Vector2(880f, hist.items.Count * (rowH + 8f));

        for (int i = 0; i < hist.items.Count; i++)
        {
            GachaService.PurchaseItem it = hist.items[i];
            float y = -(i * (rowH + 8f)) - rowH * 0.5f;

            Image row = MakeImage(content, "Row" + i, new Vector2(0f, y), new Vector2(860f, rowH), new Color(1f, 1f, 1f, 0.05f));

            string date = it.capturedAt;
            if (!string.IsNullOrEmpty(date) && date.Length >= 10) date = date.Substring(0, 10);

            MakeText(row.rectTransform, "Info", new Vector2(-90f, 16f), new Vector2(640f, 30f), 21, TextAnchor.MiddleLeft,
                     "<b>GEM " + it.gem + "</b>   " + it.currency + " " + it.amount, Color.white);
            MakeText(row.rectTransform, "Date", new Vector2(-90f, -16f), new Vector2(640f, 26f), 16, TextAnchor.MiddleLeft,
                     "<color=#7C8496>" + date + "</color>", Color.white);

            if (it.canWithdraw)
            {
                GachaService.PurchaseItem captured = it;
                MakeButton(row.rectTransform, "Withdraw", new Vector2(330f, 0f), new Vector2(180f, 56f), Loc.T("wd.button"),
                           new Color(0.46f, 0.28f, 0.30f),
                           delegate { OnWithdraw(canvasRoot, dim, content, status, captured); });
            }
            else
            {
                MakeText(row.rectTransform, "Reason", new Vector2(330f, 0f), new Vector2(200f, 40f), 17, TextAnchor.MiddleCenter,
                         "<color=#6E7686>" + ReasonText(it.reason) + "</color>", Color.white);
            }
        }
    }

    // 왜 못 하는지 보여줘야 문의가 줄어든다.
    static string ReasonText(string reason)
    {
        if (reason == "used") return Loc.T("wd.r.used");
        if (reason == "expired") return Loc.T("wd.r.expired");
        if (reason == "refunded") return Loc.T("wd.r.refunded");
        if (reason == "processing") return Loc.T("wd.r.processing");
        return Loc.T("wd.r.nocapture"); // no-capture / no-baseline — 기능 도입 전 결제
    }

    static async void OnWithdraw(RectTransform canvasRoot, Image dim, RectTransform content, Text status,
                                 GachaService.PurchaseItem item)
    {
        bool ok = await ConfirmDialog.ShowAsync(canvasRoot,
            Loc.T("wd.confirmTitle"),
            Loc.T("wd.confirmBody", item.currency + " " + item.amount, item.gem),
            Loc.T("confirm.yes"), Loc.T("confirm.no"));
        if (!ok || dim == null) return;

        status.enabled = true;
        status.text = Loc.T("wd.loading");

        try
        {
            GachaService.WithdrawResult r = await GachaService.WithdrawPurchaseAsync(item.orderId);
            if (dim == null) return;
            if (r != null && r.ok)
            {
                await PlayerProfileService.RefreshAsync(); // 젬이 회수됐으므로 미러 갱신
                FloatingToast(canvasRoot, Loc.T("wd.done", item.currency + " " + item.amount), true);
            }
            else FloatingToast(canvasRoot, Loc.T("wd.failed", "unknown"), false);
        }
        catch (Exception e)
        {
            // 서버 사유(gem-already-used 등)를 그대로 노출해야 원인 추적이 된다.
            Debug.LogError("[Withdraw] 실패: " + e);
            if (dim != null) FloatingToast(canvasRoot, Loc.T("wd.failed", ShortReason(e.Message)), false);
        }

        if (dim != null && content != null) await Reload(canvasRoot, dim, content, status);
    }

    static string ShortReason(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return "unknown";
        int i = msg.IndexOf("Error:", StringComparison.Ordinal);
        if (i >= 0) msg = msg.Substring(i + 6);
        msg = msg.Trim();
        return msg.Length > 40 ? msg.Substring(0, 40) : msg;
    }

    static void FloatingToast(RectTransform canvasRoot, string text, bool good)
    {
        Text t = MakeText(canvasRoot, "WdToast", new Vector2(0f, -430f), new Vector2(1400f, 44f), 24,
                          TextAnchor.MiddleCenter, text, good ? new Color(0.6f, 1f, 0.7f) : new Color(1f, 0.6f, 0.55f));
        UnityEngine.Object.Destroy(t.gameObject, 4f);
    }

    // ── UI 빌더 (ConfirmDialog와 동일 규약) ──

    static Font UiFont()
    {
        if (uiFont == null) uiFont = Resources.Load<Font>("Fonts/Pretendard-Regular");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        MakeText(img.rectTransform, "Label", Vector2.zero, size, 19, TextAnchor.MiddleCenter, label, Color.white);
        return b;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
