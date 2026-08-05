using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 패시브 상태 표시 — 스태미너 게이지 왼쪽 위에 아이콘을 늘어놓는다.
//
// 표시 규칙(모드별로 알아야 할 게 다르다):
//   Always      — 컬러 고정. 별도 정보 없음
//   Periodic    — 쿨타임이면 흑백 + 남은 초, 발동 중이면 컬러 + 남은 초
//   OnKillCount — 미발동이면 흑백 + 남은 처치 수, 발동 중이면 컬러 + 남은 초
//   Once        — 남아 있으면 컬러, 소모되면 흑백
//
// 씬 배선 없이 런타임 생성(프로젝트 UI 관례). 스태미너 바를 찾아 그 기준으로 붙는다.
public class PassiveHUD : MonoBehaviour
{
    [Tooltip("아이콘 한 변(px)")]
    public float iconSize = 46f;
    public float iconSpacing = 54f;

    [Tooltip("스태미너 게이지 왼쪽 끝에서 얼마나 왼쪽·위로 띄울지")]
    public Vector2 offsetFromStamina = new Vector2(-8f, 46f);

    class Entry
    {
        public PassiveRunner.Slot slot;
        public Image icon;
        public Text label;
        public Outline labelOutline;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private RectTransform root;
    private bool built;

    // 씬 배선 없이 스스로 붙는다(SpellOrbHologram과 같은 방식).
    // 스태미너 바가 있는 씬 = 게임 화면이라는 뜻이므로 그걸 기준으로 삼는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => TryAttach();
        TryAttach();
    }

    static void TryAttach()
    {
        if (FindFirstObjectByType<PassiveHUD>() != null) return;
        StaminaBar bar = FindFirstObjectByType<StaminaBar>();
        if (bar == null) return;
        bar.gameObject.AddComponent<PassiveHUD>();
    }

    static readonly Color ActiveTint = Color.white;
    static readonly Color IdleTint = new Color(0.45f, 0.45f, 0.5f, 0.85f); // 흑백(회색 틴트)

    void Update()
    {
        if (!built)
        {
            // 구동기가 준비된 뒤에 만든다(SpellCaster.Start에서 세팅되므로 한 프레임 이상 늦다)
            if (PassiveRunner.Instance == null || PassiveRunner.Instance.Slots.Count == 0) return;
            Build();
            built = true;
        }
        Refresh();
        PlaceTooltip();
    }

    void Build()
    {
        StaminaBar bar = Object.FindFirstObjectByType<StaminaBar>();
        if (bar == null || bar.fillImage == null) return;

        RectTransform fill = bar.fillImage.rectTransform;
        if (fill.parent == null) return;

        GameObject go = new GameObject("PassiveHUD", typeof(RectTransform));
        root = go.GetComponent<RectTransform>();
        root.SetParent(fill.parent, false);
        root.anchorMin = fill.anchorMin;
        root.anchorMax = fill.anchorMax;
        root.pivot = fill.pivot;

        // 게이지 왼쪽 끝을 기준점으로 잡는다(게이지 길이가 바뀌어도 따라감)
        float leftX = fill.anchoredPosition.x - fill.sizeDelta.x * 0.5f;
        root.anchoredPosition = new Vector2(leftX + offsetFromStamina.x,
                                            fill.anchoredPosition.y + offsetFromStamina.y);
        root.sizeDelta = Vector2.zero;

        // 스펠 벨트보다 뒤에 그려지게 한다.
        // UGUI는 뒤 형제가 위에 그려지므로, 벨트 앞 형제로 넣어야 Shift로 벨트가 올라올 때
        // 패시브 아이콘이 벨트를 덮지 않는다.
        SpellHandHUD belt = FindFirstObjectByType<SpellHandHUD>();
        if (belt != null && belt.transform.parent == root.parent)
            root.SetSiblingIndex(belt.transform.GetSiblingIndex());

        IReadOnlyList<PassiveRunner.Slot> slots = PassiveRunner.Instance.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            Entry e = new Entry();
            e.slot = slots[i];

            GameObject ig = new GameObject("Passive" + i, typeof(RectTransform));
            RectTransform irt = ig.GetComponent<RectTransform>();
            irt.SetParent(root, false);
            irt.anchoredPosition = new Vector2(i * iconSpacing, 0f);
            irt.sizeDelta = new Vector2(iconSize, iconSize);

            e.icon = ig.AddComponent<Image>();
            e.icon.sprite = IconOf(e.slot.marble);
            e.icon.preserveAspect = true;
            e.icon.raycastTarget = true; // 호버 판정을 받아야 툴팁이 뜬다

            // 마우스 올리면 설명 툴팁
            int captured = i;
            DeckItemEvents ev = ig.AddComponent<DeckItemEvents>();
            ev.onEnter = () => ShowTooltip(captured);
            ev.onExit = HideTooltip;

            // 남은 초/처치 수 — 아이콘 아래쪽에 겹쳐 표시
            GameObject tg = new GameObject("Label", typeof(RectTransform));
            RectTransform trt = tg.GetComponent<RectTransform>();
            trt.SetParent(irt, false);
            trt.anchoredPosition = new Vector2(0f, -iconSize * 0.42f);
            trt.sizeDelta = new Vector2(iconSize * 1.6f, 20f);

            e.label = tg.AddComponent<Text>();
            e.label.font = LoadFont();
            e.label.fontSize = 16;
            e.label.fontStyle = FontStyle.Bold;
            e.label.alignment = TextAnchor.MiddleCenter;
            e.label.horizontalOverflow = HorizontalWrapMode.Overflow;
            e.label.raycastTarget = false;

            // 배경이 밝을 때도 읽히게 검은 외곽선
            e.labelOutline = tg.AddComponent<Outline>();
            e.labelOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            e.labelOutline.effectDistance = new Vector2(1.5f, -1.5f);

            entries.Add(e);
        }

        BuildTooltip();
    }

    // ── 툴팁 ────────────────────────────────────────────────
    // 최상위 캔버스에 붙인다 — 아이콘 옆에 두면 다른 HUD에 가려진다.
    private RectTransform tipRoot;
    private Text tipText;
    private Image tipIcon;

    void BuildTooltip()
    {
        // ⚠️ "루트 캔버스를 아무거나 찾기"로 하면 안 된다.
        //    씬에 PauseCanvas 같은 평소 꺼져 있는 캔버스가 있어서, 거기 붙으면
        //    툴팁이 부모째로 비활성이라 영영 안 보인다(실제로 그렇게 안 떴다).
        //    아이콘이 올라가 있는 바로 그 캔버스에 붙인다.
        if (root == null) return;
        Canvas host = root.GetComponentInParent<Canvas>();
        if (host == null) return;
        Canvas top = host.rootCanvas != null ? host.rootCanvas : host;

        GameObject go = new GameObject("PassiveTooltip", typeof(RectTransform));
        tipRoot = go.GetComponent<RectTransform>();
        tipRoot.SetParent(top.transform, false);
        tipRoot.pivot = new Vector2(0f, 0f); // 커서 우상단으로 펼침
        tipRoot.sizeDelta = new Vector2(330f, 132f);

        Image bg = new GameObject("Bg", typeof(RectTransform)).AddComponent<Image>();
        bg.rectTransform.SetParent(tipRoot, false);
        bg.rectTransform.anchorMin = Vector2.zero;
        bg.rectTransform.anchorMax = Vector2.one;
        bg.rectTransform.offsetMin = Vector2.zero;
        bg.rectTransform.offsetMax = Vector2.zero;
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);
        bg.raycastTarget = false;

        tipIcon = new GameObject("Icon", typeof(RectTransform)).AddComponent<Image>();
        tipIcon.rectTransform.SetParent(tipRoot, false);
        tipIcon.rectTransform.anchorMin = new Vector2(0f, 1f);
        tipIcon.rectTransform.anchorMax = new Vector2(0f, 1f);
        tipIcon.rectTransform.anchoredPosition = new Vector2(32f, -32f);
        tipIcon.rectTransform.sizeDelta = new Vector2(46f, 46f);
        tipIcon.preserveAspect = true;
        tipIcon.raycastTarget = false;

        tipText = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
        tipText.rectTransform.SetParent(tipRoot, false);
        tipText.rectTransform.anchorMin = new Vector2(0f, 1f);
        tipText.rectTransform.anchorMax = new Vector2(0f, 1f);
        tipText.rectTransform.pivot = new Vector2(0f, 1f);
        tipText.rectTransform.anchoredPosition = new Vector2(64f, -10f);
        tipText.rectTransform.sizeDelta = new Vector2(256f, 118f);
        tipText.font = LoadFont();
        tipText.fontSize = 15;
        tipText.alignment = TextAnchor.UpperLeft;
        tipText.color = Color.white;
        tipText.raycastTarget = false;

        // 다른 HUD 위로 — 활성 상태에서 설정해야 먹는다(비활성이면 조용히 무시된다)
        Canvas c = go.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 32000;
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        tipRoot.SetAsLastSibling();
        go.SetActive(false);
    }

    void ShowTooltip(int index)
    {
        if (tipRoot == null || index < 0 || index >= entries.Count) return;
        SpellMarble m = entries[index].slot.marble;
        if (m == null || m.ability == null) return;

        tipRoot.gameObject.SetActive(true);

        // ⚠️ overrideSorting은 비활성 오브젝트에 걸면 조용히 꺼진다(부모 캔버스를 못 찾아서).
        //    활성화 "직후"에 다시 확정해야 다른 HUD 위로 확실히 올라온다.
        Canvas c = tipRoot.GetComponent<Canvas>();
        if (c != null) { c.overrideSorting = true; c.sortingOrder = 32000; }
        tipRoot.SetAsLastSibling();

        tipText.text = "<b>" + SpellText.Name(m.ability) + "</b>\n"
                     + SuitInfo.RichLabel(m.suit) + "  <color=#FFD24A>[" + m.grade + "]</color>\n"
                     + SpellText.Desc(m.ability);
        tipIcon.sprite = IconOf(m);
        tipIcon.enabled = tipIcon.sprite != null;
        PlaceTooltip();
    }

    void PlaceTooltip()
    {
        if (tipRoot == null || !tipRoot.gameObject.activeSelf) return;
        if (UnityEngine.InputSystem.Mouse.current == null) return;
        Vector2 mp = UnityEngine.InputSystem.Mouse.current.position.ReadValue();

        // 화면 오른쪽·위로 넘치면 반대편으로 접는다
        float w = tipRoot.sizeDelta.x, h = tipRoot.sizeDelta.y;
        float x = mp.x + 18f, y = mp.y + 18f;
        if (x + w > Screen.width) x = mp.x - 18f - w;
        if (y + h > Screen.height) y = mp.y - 18f - h;
        tipRoot.position = new Vector2(x, y);
    }

    void HideTooltip()
    {
        if (tipRoot != null) tipRoot.gameObject.SetActive(false);
    }

    // 한글 폰트 — WebGL은 OS 폰트 폴백이 없어 내장 폰트를 반드시 써야 한다.
    // ⚠️ 경로가 한 글자만 틀려도 조용히 null이 되고, 기본 폰트로 폴백돼 한글만 깨진다
    //    (숫자·영문은 멀쩡해서 에디터에서는 알아채기 어렵다). 파일명은 Pretendard-Regular.
    static Font LoadFont()
    {
        Font f = Resources.Load<Font>("Fonts/Pretendard-Regular");
        if (f == null) Debug.LogWarning("[PassiveHUD] 한글 폰트 로드 실패 — 툴팁 한글이 깨진다");
        return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    static Sprite IconOf(SpellMarble m)
    {
        if (m == null) return null;
        if (m.icon != null) return m.icon;
        return m.ability != null ? m.ability.icon : null;
    }

    void Refresh()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            PassiveRunner.Slot s = e.slot;
            bool active = s.IsActive;
            string text = "";

            switch (s.marble.passiveMode)
            {
                case PassiveMode.Always:
                    active = true; // 상시는 항상 켜진 것으로 본다
                    break;

                case PassiveMode.Periodic:
                    // 발동 중이면 남은 지속, 아니면 다음 발동까지
                    text = Mathf.CeilToInt(active ? s.RemainActive : s.RemainCooldown) + "s";
                    break;

                case PassiveMode.OnKillCount:
                    text = active ? Mathf.CeilToInt(s.RemainActive) + "s" : s.killsLeft.ToString();
                    break;

                case PassiveMode.Once:
                    active = HasResurrectionLeft(); // 소모되면 흑백
                    break;
            }

            e.icon.color = active ? ActiveTint : IdleTint;
            e.label.text = text;
            e.label.color = active ? new Color(1f, 0.95f, 0.6f) : new Color(0.8f, 0.82f, 0.9f);
        }
    }

    // Once(부활)는 상태 컴포넌트가 남아 있는지로 판단한다 —
    // 소모되면 ResurrectionStatus가 스스로 제거되므로 그게 곧 "남았나"의 답이다.
    bool HasResurrectionLeft()
    {
        GameObject p = GameObject.FindWithTag("Player");
        return p != null && p.GetComponent<ResurrectionStatus>() != null;
    }
}
