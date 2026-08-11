using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 모바일 화면 컨트롤 — 왼쪽 이동 스틱, 오른쪽 조준/발사 스틱, 대시 버튼.
//
// 플로팅 방식(누른 자리에 스틱이 생김)을 쓴다. 고정 위치는 손 크기와 잡는 자세에 따라
// 손가락이 안 닿거나 화면을 가린다.
//
// 씬 배선 없이 스스로 붙는다(PassiveHUD·SpellOrbHologram과 같은 관례).
public class TouchControls : MonoBehaviour
{
    [Header("스틱")]
    public float stickRadius = 110f;       // 최대로 민 거리(px)
    public float deadZone = 12f;           // 이 안은 입력 없음(손가락 떨림)
    public float knobSize = 76f;
    public float baseSize = 168f;

    [Header("대시 버튼")]
    public float dashButtonSize = 124f;
    public Vector2 dashButtonOffset = new Vector2(-150f, 210f); // 우하단 기준

    [Header("모바일 레이아웃")]
    [Tooltip("터치 모드에서 스태미너 바를 위로 올릴 거리(px) — 벨트와 겹침 방지")]
    public float mobileStaminaLift = 120f;

    [Header("벨트 닫기 버튼")]
    public float closeButtonSize = 92f;
    public Vector2 closeButtonOffset = new Vector2(-84f, 96f); // 우하단 기준 — 벨트 오른쪽

    [Header("스펠 스와이프")]
    public float swipeStartHeight = 0.18f; // 화면 아래 이 비율 안에서 시작해야 인정
    public float swipeUpDistance = 90f;    // 이만큼 위로 밀면 벨트 열기

    // ── 내부 ────────────────────────────────────────────────
    class Stick
    {
        public int fingerId = -1;
        public Vector2 origin;    // 처음 누른 화면 좌표
        public Vector2 value;     // -1~1
        public RectTransform baseRt, knobRt;
        public CanvasGroup group;
    }

    private Stick left = new Stick(), right = new Stick();
    private RectTransform dashBtn;
    private RectTransform closeBtn;   // 벨트 닫기(모바일 전용)
    private CanvasGroup closeGroup;
    private bool layoutApplied;
    private float staminaBaseY = float.NaN;
    private float hintFontBase = float.NaN;
    private CanvasGroup dashGroup;
    private int dashFinger = -1;
    private bool dashQueued;

    private RectTransform root;
    private Canvas canvas;
    private bool built;

    // 스펠 스와이프 추적
    private int swipeFinger = -1;
    private Vector2 swipeStart;

    // 벨트에서 오브를 끌 때 쓰는 손가락(스펠 드래그로 넘긴다)
    private int spellFinger = -1;

    private static Sprite circle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => TryAttach();
        TryAttach();
    }

    static void TryAttach()
    {
        if (FindFirstObjectByType<TouchControls>() != null) return;
        // 게임 화면에만 — 스태미너 바가 있는 씬이 곧 게임 화면이다
        StaminaBar bar = FindFirstObjectByType<StaminaBar>();
        if (bar == null) return;
        bar.gameObject.AddComponent<TouchControls>();
    }

    void Update()
    {
        if (!built) { Build(); built = true; }

        Touchscreen ts = Touchscreen.current;
        bool show = GameInput.TouchMode && ts != null;
        SetVisible(show);

        if (!show)
        {
            // 마우스로 돌아갔으면 눌린 상태를 남기지 않는다
            ReleaseStick(left); ReleaseStick(right);
            dashFinger = -1; swipeFinger = -1; spellFinger = -1;
            if (closeGroup != null) closeGroup.alpha = 0f; // root 밖이라 따로 숨겨야 한다
            GameInput.PushTouch(Vector2.zero, Vector2.zero, false, false);
            return;
        }

        ReadTouches(ts);
    }

    // 벨트가 움직인 뒤에 붙는다.
    // Update에서 하면 SpellSelectionUI와 실행 순서가 보장되지 않아, 벨트가 올라오는 동안
    // 버튼이 한 프레임씩 뒤처져 따라간다.
    void LateUpdate()
    {
        ApplyMobileLayout();

        if (closeBtn == null) return;
        bool open = GameInput.SpellSelecting && GameInput.TouchMode;
        if (closeGroup != null) closeGroup.alpha = open ? 1f : 0f;
        if (open) PlaceCloseButton();
    }

    // 모바일 화면비 보정.
    // 기준 해상도(16:9)에서는 스태미너 바와 스펠 벨트가 안 겹치는데, 폰은 세로가 눌린
    // 화면비(19.5:9 등)라 아래쪽 UI가 서로 밀려 올라와 겹친다.
    // 터치 모드일 때만 스태미너를 벨트 위로 올리고, 안내 탭 문구를 터치용으로 바꾼다.
    void ApplyMobileLayout()
    {
        bool touch = GameInput.TouchMode;
        if (touch == layoutApplied) return;   // 상태가 바뀔 때만 손댄다
        layoutApplied = touch;

        StaminaBar bar = FindFirstObjectByType<StaminaBar>();
        if (bar != null && bar.fillImage != null)
        {
            RectTransform fill = bar.fillImage.rectTransform;
            Transform holder = fill.parent;              // StaminaGauge 묶음
            RectTransform grp = holder as RectTransform;
            if (grp != null)
            {
                if (touch) { staminaBaseY = grp.anchoredPosition.y; grp.anchoredPosition += new Vector2(0f, mobileStaminaLift); }
                else if (!float.IsNaN(staminaBaseY)) grp.anchoredPosition = new Vector2(grp.anchoredPosition.x, staminaBaseY);
            }
        }

        // 안내 탭 문구 — 없애면 모바일 유저가 스펠을 여는 방법을 알 수 없다.
        // 키 이름 대신 조작 방식을 적어준다.
        SpellSelectionUI sel = FindFirstObjectByType<SpellSelectionUI>();
        if (sel != null && sel.ctrlHint != null)
        {
            Transform label = sel.ctrlHint.transform.Find("Label");
            TMPro.TextMeshProUGUI tm = label != null ? label.GetComponent<TMPro.TextMeshProUGUI>() : null;
            if (tm != null)
            {
                if (touch) { if (float.IsNaN(hintFontBase)) hintFontBase = tm.fontSize; }
                tm.text = touch ? Loc.T("hint.spellTouch") : Loc.T("hint.spellKey");
                // 터치 문구가 더 길다 — 탭 폭을 안 넘게 살짝 줄인다
                if (!float.IsNaN(hintFontBase)) tm.fontSize = touch ? hintFontBase * 0.72f : hintFontBase;
            }
        }
    }

    void ReadTouches(Touchscreen ts)
    {
        bool spellOpen = GameInput.SpellSelecting;
        Vector2 pointer = Vector2.zero;
        bool pDown = false, pPressed = false, pReleased = false;

        var touches = ts.touches;
        for (int i = 0; i < touches.Count; i++)
        {
            var t = touches[i];
            var phase = t.phase.ReadValue();
            if (phase == UnityEngine.InputSystem.TouchPhase.None) continue;

            int id = t.touchId.ReadValue();
            Vector2 pos = t.position.ReadValue();
            bool began = phase == UnityEngine.InputSystem.TouchPhase.Began;
            bool ended = phase == UnityEngine.InputSystem.TouchPhase.Ended
                      || phase == UnityEngine.InputSystem.TouchPhase.Canceled;

            // ── 이미 역할이 정해진 손가락 ──
            if (id == left.fingerId) { UpdateStick(left, pos, ended); continue; }
            if (id == right.fingerId) { UpdateStick(right, pos, ended); continue; }
            if (id == dashFinger) { if (ended) dashFinger = -1; continue; }
            if (id == spellFinger)
            {
                pointer = pos; pDown = !ended; pReleased = ended;
                if (ended) spellFinger = -1;
                continue;
            }
            if (id == swipeFinger)
            {
                if (pos.y - swipeStart.y > swipeUpDistance)
                {
                    GameInput.SetSpellSelectToggle(true); // 위로 밀기 → 벨트 열기
                    swipeFinger = -1;
                }
                else if (ended) swipeFinger = -1;
                continue;
            }

            if (!began) continue;

            // ── 새 손가락에 역할 배정 ──

            // 대시 버튼
            if (dashBtn != null && Inside(dashBtn, pos)) { dashFinger = id; dashQueued = true; continue; }

            // 닫기 버튼(벨트 오른쪽) — 슬롯 판정보다 먼저 본다
            if (spellOpen && closeBtn != null && Inside(closeBtn, pos))
            {
                GameInput.SetSpellSelectToggle(false);
                continue;
            }

            // 벨트가 열려 있으면 슬롯 위 터치는 드래그로 넘긴다(오브를 끌어 쓰는 동작)
            if (spellOpen && OverSlot(pos))
            {
                spellFinger = id;
                pointer = pos; pDown = true; pPressed = true;
                continue;
            }

            // 스펠 벨트 열기 — 화면 아래 가장자리에서 위로 밀기.
            // 닫기는 스와이프가 아니라 벨트 오른쪽 X 버튼으로 한다.
            // (벨트가 열리면 화면 아래를 덮어버려서, 아래로 미는 스와이프는
            //  시작 지점이 벨트에 먹혀 판정이 잘 안 잡혔다)
            if (!spellOpen && pos.y < Screen.height * swipeStartHeight)
            {
                swipeFinger = id; swipeStart = pos;
                continue;
            }

            // 좌/우 절반으로 이동/조준 스틱
            if (pos.x < Screen.width * 0.5f)
            {
                if (left.fingerId < 0) BeginStick(left, id, pos);
            }
            else
            {
                if (right.fingerId < 0) BeginStick(right, id, pos);
                // 벨트가 열려 있지 않은 평상시, 오른쪽 탭은 "필드 탭"으로도 기록해둔다
                // (♠ 비격진천뢰가 탭한 자리에 떨어지게)
                GameInput.PushFieldTap(GameInput.ScreenToWorld(pos));
            }
        }

        // 닫는 방법은 "아래로 스와이프" 하나로 통일한다.
        // 예전엔 "바깥을 탭하면 닫기"도 있었는데, 아래로 미는 손이 화면에 닿는 순간
        // 그 탭으로 먼저 닫혀버려서 스와이프가 성립하지 않았다.
        // 닫는 동작이 둘이면 하나가 다른 하나를 가로챈다.

        // 닫기 버튼 — 벨트가 열려 있을 때만 보이고, 벨트 바로 오른쪽에 붙는다.
        // 벨트는 열릴 때 위로 올라오며 2.4배로 커지므로 위치가 크게 변한다.
        // 화면 모서리에 고정해두면 벨트에서 멀찍이 떨어져 "벨트를 닫는 버튼"으로 안 읽힌다.
        if (closeGroup != null) closeGroup.alpha = spellOpen ? 1f : 0f;

        bool fire = right.fingerId >= 0 && right.value.sqrMagnitude > 0.0001f;
        GameInput.PushTouch(left.value, right.value, fire, dashQueued);
        GameInput.PushTouchPointer(pointer, pDown, pPressed, pReleased);
        dashQueued = false;
    }

    // 벨트의 가장 오른쪽 슬롯 옆에 붙인다(벨트가 커지거나 올라가도 따라감)
    void PlaceCloseButton()
    {
        SpellHandHUD hud = FindFirstObjectByType<SpellHandHUD>();
        if (hud == null || hud.SlotCount <= 0) return;

        RectTransform last = null;
        float maxX = float.MinValue;
        for (int i = 0; i < hud.SlotCount; i++)
        {
            RectTransform r = hud.GetSlotRect(i);
            if (r == null) continue;
            if (r.position.x > maxX) { maxX = r.position.x; last = r; }
        }
        if (last == null) return;

        Vector3[] c = new Vector3[4];
        last.GetWorldCorners(c);
        float right = Mathf.Max(c[2].x, c[3].x);
        float midY = (c[0].y + c[1].y) * 0.5f;

        // 슬롯 크기에 맞춰 버튼도 키운다 — 벨트가 확대되면 버튼만 작으면 눌리기 어렵다
        float slotH = Mathf.Abs(c[1].y - c[0].y);
        float size = Mathf.Clamp(slotH * 0.8f, closeButtonSize * 0.7f, closeButtonSize * 2f);
        closeBtn.sizeDelta = new Vector2(size, size);
        closeBtn.position = new Vector3(right + size * 0.85f, midY, 0f);
    }

    void BeginStick(Stick s, int id, Vector2 pos)
    {
        s.fingerId = id;
        s.origin = pos;
        s.value = Vector2.zero;
        if (s.baseRt != null) { s.baseRt.position = pos; s.knobRt.position = pos; }
        if (s.group != null) s.group.alpha = 1f;
    }

    void UpdateStick(Stick s, Vector2 pos, bool ended)
    {
        if (ended) { ReleaseStick(s); return; }

        Vector2 d = pos - s.origin;
        float len = d.magnitude;
        if (len < deadZone) s.value = Vector2.zero;
        else
        {
            // deadZone을 뺀 나머지를 0~1로 — 손가락을 조금만 움직여도 최대 속도가 되지 않게
            float t = Mathf.Clamp01((len - deadZone) / (stickRadius - deadZone));
            s.value = d.normalized * t;
        }

        if (s.knobRt != null)
            s.knobRt.position = s.origin + Vector2.ClampMagnitude(d, stickRadius);
    }

    void ReleaseStick(Stick s)
    {
        s.fingerId = -1;
        s.value = Vector2.zero;
        if (s.group != null) s.group.alpha = 0f;
    }

    // 슬롯(오브가 놓인 칸) 위인지 — 드래그로 넘길지 판단.
    // 벨트 배경은 제외한다(닫기 스와이프가 시작될 자리라서).
    bool OverSlot(Vector2 screen)
    {
        SpellHandHUD hud = FindFirstObjectByType<SpellHandHUD>();
        if (hud == null) return false;
        for (int i = 0; i < hud.SlotCount; i++)
        {
            RectTransform r = hud.GetSlotRect(i);
            if (r != null && RectTransformUtility.RectangleContainsScreenPoint(r, screen, null)) return true;
        }
        return false;
    }

    // 스펠 벨트/슬롯 위인지 — 드래그를 스펠 쪽에 넘길지 판단
    bool OverSpellUI(Vector2 screen)
    {
        SpellHandHUD hud = FindFirstObjectByType<SpellHandHUD>();
        if (hud == null) return false;
        for (int i = 0; i < hud.SlotCount; i++)
        {
            RectTransform r = hud.GetSlotRect(i);
            if (r != null && RectTransformUtility.RectangleContainsScreenPoint(r, screen, null)) return true;
        }
        RectTransform belt = hud.transform as RectTransform;
        return belt != null && RectTransformUtility.RectangleContainsScreenPoint(belt, screen, null);
    }

    static bool Inside(RectTransform rt, Vector2 screen)
    {
        return rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screen, null);
    }

    void SetVisible(bool on)
    {
        if (root != null && root.gameObject.activeSelf != on) root.gameObject.SetActive(on);
    }

    // ── UI 생성 ─────────────────────────────────────────────
    void Build()
    {
        StaminaBar bar = FindFirstObjectByType<StaminaBar>();
        Canvas host = bar != null && bar.fillImage != null
            ? bar.fillImage.canvas : FindFirstObjectByType<Canvas>();
        if (host == null) return;
        canvas = host.rootCanvas != null ? host.rootCanvas : host;

        GameObject rootGo = new GameObject("TouchControls", typeof(RectTransform));
        root = rootGo.GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero; root.offsetMax = Vector2.zero;
        root.SetAsFirstSibling(); // 다른 HUD보다 뒤에 그린다(벨트·버튼을 가리지 않게)

        BuildStick(left, "MoveStick", new Color(0.55f, 0.85f, 1f));
        BuildStick(right, "AimStick", new Color(1f, 0.72f, 0.45f));
        BuildDashButton();
        BuildCloseButton();
    }

    void BuildStick(Stick s, string name, Color tint)
    {
        GameObject bg = new GameObject(name + "Base", typeof(RectTransform));
        s.baseRt = bg.GetComponent<RectTransform>();
        s.baseRt.SetParent(root, false);
        s.baseRt.sizeDelta = new Vector2(baseSize, baseSize);
        Image bi = bg.AddComponent<Image>();
        bi.sprite = CircleSprite();
        bi.color = new Color(tint.r, tint.g, tint.b, 0.16f);
        bi.raycastTarget = false;

        GameObject kn = new GameObject(name + "Knob", typeof(RectTransform));
        s.knobRt = kn.GetComponent<RectTransform>();
        s.knobRt.SetParent(root, false);
        s.knobRt.sizeDelta = new Vector2(knobSize, knobSize);
        Image ki = kn.AddComponent<Image>();
        ki.sprite = CircleSprite();
        ki.color = new Color(tint.r, tint.g, tint.b, 0.5f);
        ki.raycastTarget = false;

        // 누르기 전에는 안 보인다 — 플로팅이라 안 눌린 스틱의 위치는 의미가 없다.
        // 배경과 손잡이가 따로 놀지 않게 알파를 묶는다.
        s.group = bg.AddComponent<CanvasGroup>();
        s.group.alpha = 0f;
        CanvasGroup kg = kn.AddComponent<CanvasGroup>();
        kg.alpha = 0f;
        bg.AddComponent<AlphaLink>().other = kg;
    }

    void BuildDashButton()
    {
        GameObject go = new GameObject("DashBtn", typeof(RectTransform));
        dashBtn = go.GetComponent<RectTransform>();
        dashBtn.SetParent(root, false);
        dashBtn.anchorMin = new Vector2(1f, 0f);
        dashBtn.anchorMax = new Vector2(1f, 0f);
        dashBtn.pivot = new Vector2(0.5f, 0.5f);
        dashBtn.anchoredPosition = dashButtonOffset;
        dashBtn.sizeDelta = new Vector2(dashButtonSize, dashButtonSize);

        Image im = go.AddComponent<Image>();
        im.sprite = CircleSprite();
        im.color = new Color(0.55f, 0.85f, 1f, 0.22f);
        im.raycastTarget = false; // 판정은 직접 한다(EventSystem을 안 거침)

        GameObject tg = new GameObject("Label", typeof(RectTransform));
        RectTransform trt = tg.GetComponent<RectTransform>();
        trt.SetParent(dashBtn, false);
        trt.sizeDelta = new Vector2(dashButtonSize, 34f);
        Text tx = tg.AddComponent<Text>();
        tx.font = Resources.Load<Font>("Fonts/Pretendard-Regular")
               ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tx.text = "DASH";
        tx.fontSize = 22;
        tx.fontStyle = FontStyle.Bold;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.color = new Color(0.8f, 0.93f, 1f, 0.8f);
        tx.raycastTarget = false;

        dashGroup = go.AddComponent<CanvasGroup>();
    }

    // 벨트 닫기 버튼 — 벨트 오른쪽 공간.
    // 아래로 스와이프해 닫는 방식은 벨트가 화면 아래를 덮어버려 시작 지점이 먹혔다.
    // 빨간 원만으로는 "닫기"로 안 읽혀서, 굵은 X + "닫기" 글자 + 흰 테두리로 못 박는다.
    void BuildCloseButton()
    {
        GameObject go = new GameObject("BeltCloseBtn", typeof(RectTransform));
        closeBtn = go.GetComponent<RectTransform>();
        // ⚠️ root(TouchControls)는 벨트보다 뒤에 그려진다. 닫기 버튼을 거기 두면 벨트에 가린다.
        //    캔버스 직속 마지막 자식으로 붙여 항상 위에 오게 한다.
        closeBtn.SetParent(canvas != null ? canvas.transform : root, false);
        closeBtn.SetAsLastSibling();
        closeBtn.anchorMin = new Vector2(1f, 0f);
        closeBtn.anchorMax = new Vector2(1f, 0f);
        closeBtn.anchoredPosition = closeButtonOffset;
        closeBtn.sizeDelta = new Vector2(closeButtonSize, closeButtonSize);

        // 바탕(짙은 적색 원) — 배경이 밝아도 눈에 띄게 불투명하게
        Image bg = go.AddComponent<Image>();
        bg.sprite = CircleSprite();
        bg.color = new Color(0.72f, 0.16f, 0.18f, 0.92f);
        bg.raycastTarget = false; // 판정은 직접 한다(EventSystem을 안 거침)

        // 흰 테두리 — 어두운 배경에서도 윤곽이 보이게
        GameObject ring = new GameObject("Ring", typeof(RectTransform));
        RectTransform rrt = ring.GetComponent<RectTransform>();
        rrt.SetParent(closeBtn, false);
        rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
        rrt.offsetMin = new Vector2(-4f, -4f); rrt.offsetMax = new Vector2(4f, 4f);
        rrt.SetAsFirstSibling();  // 바탕 뒤 = 테두리처럼 보인다
        Image ri = ring.AddComponent<Image>();
        ri.sprite = CircleSprite();
        ri.color = new Color(1f, 1f, 1f, 0.85f);
        ri.raycastTarget = false;

        // ✕ 기호 — 폰트에 없을 수 있으므로 선 두 개로 직접 그린다(글리프 의존 제거)
        MakeCross(closeBtn);

        // "닫기" 글자 — 기호만으로는 못 알아보는 경우를 없앤다
        GameObject tg = new GameObject("Label", typeof(RectTransform));
        RectTransform trt = tg.GetComponent<RectTransform>();
        trt.SetParent(closeBtn, false);
        trt.anchorMin = new Vector2(0.5f, 0f);
        trt.anchorMax = new Vector2(0.5f, 0f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -4f);
        trt.sizeDelta = new Vector2(140f, 28f);
        Text tx = tg.AddComponent<Text>();
        tx.font = Resources.Load<Font>("Fonts/Pretendard-Regular")
               ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tx.text = "닫기";
        tx.fontSize = 22;
        tx.fontStyle = FontStyle.Bold;
        tx.alignment = TextAnchor.UpperCenter;
        tx.color = Color.white;
        tx.raycastTarget = false;
        Outline ol = tg.AddComponent<Outline>();
        ol.effectColor = new Color(0f, 0f, 0f, 0.9f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        closeGroup = go.AddComponent<CanvasGroup>();
        closeGroup.alpha = 0f; // 벨트가 열렸을 때만 보인다
    }

    // ✕ 를 사각형 두 개를 교차시켜 그린다. 폰트 글리프에 기대지 않아 어디서든 똑같이 보인다.
    void MakeCross(RectTransform parent)
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject bar = new GameObject("Cross" + i, typeof(RectTransform));
            RectTransform rt = bar.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(parent.sizeDelta.x * 0.52f, 7f);
            rt.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 45f : -45f);
            Image im = bar.AddComponent<Image>();
            im.color = Color.white;
            im.raycastTarget = false;
        }
    }

    static Sprite CircleSprite()
    {
        if (circle != null) return circle;
        const int R = 64;
        Texture2D t = new Texture2D(R * 2, R * 2, TextureFormat.RGBA32, false);
        Color[] px = new Color[R * 2 * R * 2];
        for (int y = 0; y < R * 2; y++)
            for (int x = 0; x < R * 2; x++)
            {
                float d = Mathf.Sqrt((x - R + 0.5f) * (x - R + 0.5f) + (y - R + 0.5f) * (y - R + 0.5f));
                float a = Mathf.Clamp01(R - d);          // 가장자리 1px 안티에일리어싱
                px[y * R * 2 + x] = new Color(1f, 1f, 1f, a);
            }
        t.SetPixels(px); t.Apply();
        circle = Sprite.Create(t, new Rect(0, 0, R * 2, R * 2), new Vector2(0.5f, 0.5f), 100f);
        return circle;
    }
}

// 스틱 배경의 알파를 손잡이에도 그대로 적용 — 둘이 따로 놀지 않게
public class AlphaLink : MonoBehaviour
{
    public CanvasGroup other;
    private CanvasGroup self;
    void Awake() { self = GetComponent<CanvasGroup>(); }
    void LateUpdate() { if (self != null && other != null) other.alpha = self.alpha; }
}
