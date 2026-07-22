using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// 고난 3중 1택 화면 — 덱을 한 바퀴 소진했을 때 뜬다.
// 시간은 TimeController.PushHold(0)로 완전 정지(카드 3장을 읽어야 하므로 슬로우로는 부족).
// 선택이 끝나면 onDone 콜백 → 조커 경고 시퀀스로 이어진다.
public class HardshipSelectUI : MonoBehaviour
{
    const int ChoiceCount = 3;

    static readonly Color Ink = new Color(0.92f, 0.94f, 1f);
    static readonly Color Dim = new Color(0f, 0f, 0f, 0.82f);

    TMP_FontAsset font;
    int timeHandle = -1;
    Action onDone;
    bool resolved;

    public static bool IsOpen { get; private set; }

    // 이미 떠 있으면 중복 생성하지 않는다(덱 소진이 연달아 보고돼도 안전).
    // ⚠️ 이때 onDone을 버리면 안 된다 — 호출자(SpellCaster)는 이 콜백으로 조커를 발동하고
    //    덱을 재셔플하므로, 유실되면 손패가 빈 채 영원히 멈추는 소프트락이 된다.
    public static void Show(Action onDone)
    {
        if (IsOpen)
        {
            if (onDone != null) onDone();
            return;
        }
        // 모든 고난이 최대 중첩이면 고를 게 없다 — 화면을 띄우면 선택 불가로 진행이 막히므로 건너뛴다.
        if (HardshipSystem.AvailableCount <= 0)
        {
            if (onDone != null) onDone();
            return;
        }
        GameObject go = new GameObject("HardshipSelectUI");
        HardshipSelectUI ui = go.AddComponent<HardshipSelectUI>();
        ui.onDone = onDone;
        ui.Build();
    }

    void Build()
    {
        IsOpen = true;
        font = Resources.Load<TMP_FontAsset>("Fonts/KoreanSDF");

        // 시간 정지 — timeScale 단일 관리자 경유(니어미스/선택 슬로우와 충돌 없음)
        if (TimeController.Instance != null) timeHandle = TimeController.Instance.PushHold(0f);

        EnsureEventSystem();

        GameObject canvasGo = new GameObject("HardshipCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4200; // 일시정지(4000) 위, 로딩(5000) 아래
        CanvasScaler sc = canvasGo.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);

        Image dim = MakeImage(canvasGo.transform, "Dim", Vector2.zero, Vector2.zero, Dim);
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = Vector2.zero;
        dim.rectTransform.offsetMax = Vector2.zero;
        dim.raycastTarget = true; // 뒤쪽 클릭 차단

        MakeText(dim.transform, "Title", Loc.T("hardship.title"), 54, new Vector2(0f, 330f), new Vector2(1200f, 70f),
            new Color(1f, 0.55f, 0.45f), FontStyles.Bold);
        MakeText(dim.transform, "Sub", Loc.T("hardship.sub"), 22,
            new Vector2(0f, 272f), new Vector2(1200f, 34f), new Color(0.6f, 0.72f, 0.82f), FontStyles.Normal);

        HardshipId[] picks = HardshipSystem.PickChoices(ChoiceCount);
        // 카드 수만큼 중앙 정렬(1~3장) — 남은 고난이 적으면 그만큼만 뜬다
        const float cardW = 380f, cardH = 380f, gap = 40f;
        float total = picks.Length * cardW + (picks.Length - 1) * gap;
        float x0 = -total * 0.5f + cardW * 0.5f;
        for (int i = 0; i < picks.Length; i++)
            BuildCard(dim.transform, picks[i], new Vector2(x0 + i * (cardW + gap), 0f), new Vector2(cardW, cardH));

        MakeText(dim.transform, "Foot", Loc.T("hardship.foot"), 18,
            new Vector2(0f, -290f), new Vector2(1200f, 30f), new Color(0.5f, 0.55f, 0.65f), FontStyles.Normal);
    }

    void BuildCard(Transform parent, HardshipId id, Vector2 pos, Vector2 size)
    {
        HardshipSystem.Def def = HardshipSystem.GetDef(id);
        int owned = HardshipSystem.Stack(id);

        Image card = MakeImage(parent, "Card_" + id, pos, size, new Color(0.1f, 0.11f, 0.16f, 0.97f));
        Sprite frame = Resources.Load<Sprite>("UI/LabDossierPanel");
        if (frame != null) { card.sprite = frame; card.type = Image.Type.Sliced; card.color = Color.white; }
        card.raycastTarget = true;

        // 상단 색 띠 — 고난별 아이덴티티(아이콘 없이도 구분되게)
        MakeImage(card.transform, "Accent", new Vector2(0f, size.y * 0.5f - 34f), new Vector2(size.x - 60f, 5f), def.color);

        MakeText(card.transform, "Name", def.Name, 34, new Vector2(0f, size.y * 0.5f - 82f),
            new Vector2(size.x - 50f, 46f), def.color, FontStyles.Bold);

        MakeText(card.transform, "Desc", def.Desc, 22, new Vector2(0f, 20f),
            new Vector2(size.x - 70f, 90f), Ink, FontStyles.Normal);

        // 현재 누적 — 같은 것을 또 고르면 얼마나 쌓이는지 보여준다
        string stackTxt = owned > 0
            ? "<color=#FFD24A>" + Loc.T("hardship.stacks", owned) + "</color>  →  " + Loc.T("hardship.stacks", owned + 1)
            : "<color=#7A8090>" + Loc.T("hardship.none") + "</color>";
        MakeText(card.transform, "Stack", stackTxt, 20, new Vector2(0f, -size.y * 0.5f + 96f),
            new Vector2(size.x - 50f, 30f), Ink, FontStyles.Normal);

        Button btn = card.gameObject.AddComponent<Button>();
        btn.targetGraphic = card;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1f, 1f, 1f, 1f);
        cb.normalColor = new Color(0.82f, 0.86f, 0.92f, 1f);
        cb.pressedColor = new Color(0.7f, 0.8f, 0.9f, 1f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        HardshipId captured = id;
        btn.onClick.AddListener(() => Choose(captured));

        MakeText(card.transform, "Pick", Loc.T("hardship.pick"), 24, new Vector2(0f, -size.y * 0.5f + 46f),
            new Vector2(size.x - 60f, 34f), new Color(0.55f, 0.9f, 1f), FontStyles.Bold);
    }

    void Choose(HardshipId id)
    {
        if (resolved) return; // 같은 프레임 중복 클릭 방지
        resolved = true;
        HardshipSystem.Add(id);
        Close();
    }

    void Close()
    {
        if (TimeController.Instance != null && timeHandle >= 0)
        {
            TimeController.Instance.PopHold(timeHandle);
            timeHandle = -1;
        }
        IsOpen = false;
        Action cb = onDone;
        onDone = null;
        Destroy(gameObject);
        if (cb != null) cb();
    }

    // 안전망 — 씬 전환 등으로 파괴돼도 시간이 멈춘 채 남지 않게
    void OnDestroy()
    {
        IsOpen = false;
        if (TimeController.Instance != null && timeHandle >= 0)
        {
            TimeController.Instance.PopHold(timeHandle);
            timeHandle = -1;
        }
    }

    static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    // ---- UI 헬퍼 ----

    static Image MakeImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return img;
    }

    TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Vector2 pos,
        Vector2 sizeDelta, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = style;
        t.richText = true;
        t.raycastTarget = false; // 카드 버튼 클릭을 가리지 않게
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = pos;
        return t;
    }
}
