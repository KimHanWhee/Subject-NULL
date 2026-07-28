using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Design Ref: §5.2 — 선택 모드 중 슬롯을 드래그→드롭해 능력 발동.
// Targeted는 드롭한 월드 위치, SelfBuff는 SpellCaster가 플레이어로 보정.
// Plan SC: FR-09(드래그 발동) / FR-07(호버 툴팁)
public class SpellDragHandler : MonoBehaviour
{
    public SpellSelectionUI selection;
    public SpellHandHUD hud;
    public SpellCaster caster;
    public Camera worldCamera;      // 비우면 Camera.main
    public Canvas canvas;           // 슬롯 rect 스크린 판정용(Overlay면 카메라 null 사용)

    [Header("Drag Ghost (선택)")]
    public RectTransform dragGhost; // 드래그 중 커서 따라다니는 아이콘
    public Image dragGhostImage;

    [Header("Tooltip (선택)")]
    public GameObject tooltipRoot;
    public Text tooltipText;        // 능력명/등급/설명
    public Image tooltipIcon;       // 능력 고유 아이콘(SpellAbility.icon)

    [Header("Hover (선택)")]
    public float hoverScale = 1.2f;     // 마우스 올린 슬롯 확대 배율(1=없음)
    public float hoverLerpSpeed = 12f;  // 확대/복귀 보간 속도(unscaled)

    [Header("Sound (선택)")]
    public AudioSource audioSource;     // 비우면 자동 생성
    public AudioClip hoverSound;        // 마우스 올릴 때(슬롯 진입 1회)
    public AudioClip clickSound;        // 드래그 시작(집을 때)
    public AudioClip useSound;          // 발동 성공 시(공통 베이스음). 능력별 SpellAbility.activationSound 아래에 깔림.
    [Range(0f, 1f)] public float useVolume = 0.5f; // 공통 베이스음 음량(메인=능력별 음 대비 낮게)

    [Header("Pick-up Pop (선택)")]
    public float dragPopScale = 1.3f;   // 집어들 때 시작 확대 배율
    public float dragPopDuration = 0.7f; // 원래 크기로 줄어드는 시간(초, unscaled)

    [Header("Cancel Feedback (선택)")]
    public Image beltImage;             // (선택) 붉게 물들일 기존 벨트 이미지. 비우면 자동 오버레이 생성
    public Color cancelTint = new Color(1f, 0.35f, 0.35f, 1f);         // beltImage 지정 시 틴트 색
    public Color cancelOverlayColor = new Color(1f, 0f, 0f, 0.35f);    // 자동 오버레이 색(이미지 미지정 시)
    public GameObject cancelLabel;      // "사용 취소" 표시(선택) — 커서 근처로 이동
    private Color beltColor0 = Color.white; // 벨트 원래 색(복구용)
    private GameObject cancelOverlay;   // 자동 생성된 취소 영역 표시(붉은 반투명)

    private int dragSlot = -1;
    private int lastHovered = -1;       // 호버 사운드 1회 재생용

    // 현재 마우스가 올라간 슬롯(없으면 -1). 홀로그램(SpellOrbHologram)이 이 값을 읽어
    // 해당 슬롯의 아이콘 표시를 끄고 툴팁으로 자리를 넘긴다.
    // 판정 로직을 두 곳에서 중복 구현하지 않도록 여기서만 계산해 공개한다.
    public int HoveredSlot { get; private set; } = -1;

    private float dragStartTime;        // 집는 팝 애니 기준 시각(unscaled)
    private Vector3[] slotBaseScale;    // 각 슬롯 기본 스케일(최초 1회 캡처)

    void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        if (beltImage != null) beltColor0 = beltImage.color;
        if (cancelLabel != null) cancelLabel.SetActive(false);
        if (dragGhost != null) dragGhost.gameObject.SetActive(false);
        if (tooltipRoot != null) tooltipRoot.SetActive(false);

        // 씬의 툴팁 텍스트가 내장 Arial이면 한글 교체 — WebGL은 OS 폰트 폴백이 없어
        // 한글 글리프가 사라지고, 레이아웃이 텍스트 크기를 따라가 박스까지 쭈그라든다.
        if (tooltipText != null)
        {
            Font kr = Resources.Load<Font>("Fonts/Pretendard-Regular");
            if (kr != null) tooltipText.font = kr;
        }

        // 툴팁 정렬용 Canvas는 여기서 "부착만" 한다.
        // ⚠️ overrideSorting은 여기서 켜도 꺼진다 — 이 시점의 tooltipRoot는 비활성이라
        //    Unity가 부모 캔버스를 찾지 못하고 이 캔버스를 루트로 판단해(isRootCanvas=true)
        //    루트에는 무의미한 overrideSorting을 강제로 false로 되돌린다.
        //    그래서 실제 설정은 활성화 직후(EnsureTooltipOnTop)에 한다.
        if (tooltipRoot != null && tooltipRoot.GetComponent<Canvas>() == null)
            tooltipRoot.AddComponent<Canvas>();
    }

    void Update()
    {
        if (selection == null || hud == null || caster == null) return;

        // 선택 모드가 아니면 모든 상태 정리
        if (!selection.IsSelecting) { EndAll(); return; }
        if (Mouse.current == null) return;

        Vector2 mouse = Mouse.current.position.ReadValue();
        int hovered = SlotUnderPoint(mouse);
        // 드래그 중에는 툴팁을 띄우지 않으므로 홀로그램도 아이콘 상태를 유지해야 한다.
        HoveredSlot = dragSlot < 0 ? hovered : -1;

        // 호버 사운드: 드래그 중이 아닐 때, 마블이 있는 슬롯에 처음 올라오면 1회(빈 슬롯 제외)
        if (dragSlot < 0 && hovered != lastHovered)
        {
            if (hovered >= 0 && HasMarble(hovered)) PlaySound(hoverSound);
            lastHovered = hovered;
        }

        // 툴팁: 드래그 중이 아닐 때만 호버 슬롯 표시
        UpdateTooltip(dragSlot < 0 ? hovered : -1, mouse);

        // 호버(또는 드래그 중인) 슬롯만 살짝 확대
        ApplyHoverScale(dragSlot >= 0 ? dragSlot : hovered);

        // 드래그 시작
        if (dragSlot < 0 && hovered >= 0 && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SpellMarble m = hovered < caster.Slots.Count ? caster.Slots[hovered] : null;
            if (m != null) BeginDrag(hovered, m, mouse);
        }

        // 드래그 진행/드롭
        if (dragSlot >= 0)
        {
            bool overBelt = IsOverBelt(mouse);
            UpdateCancelFeedback(overBelt, mouse); // 벨트 위면 붉게 + "사용 취소"

            if (dragGhost != null)
            {
                dragGhost.position = mouse;
                // 집는 팝: 확대 → 원래 크기로 easeOut
                float pp = Mathf.Clamp01((Time.unscaledTime - dragStartTime) / Mathf.Max(0.0001f, dragPopDuration));
                float ease = 1f - (1f - pp) * (1f - pp);
                dragGhost.localScale = Vector3.one * Mathf.Lerp(dragPopScale, 1f, ease);
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                if (overBelt)
                {
                    EndDrag(); // 벨트에 다시 드롭 → 사용 취소
                }
                else
                {
                    Camera cam = worldCamera != null ? worldCamera : Camera.main; // 비었으면 메인 카메라 보정
                    Vector3 world = cam != null
                        ? cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, 0f))
                        : Vector3.zero;
                    world.z = 0f;
                    bool ok = caster.Activate(dragSlot, new Vector2(world.x, world.y)); // Plan SC: FR-09
                    if (ok) PlaySound(useSound, useVolume); // 공통 베이스음(능력별 음은 SpellCaster가 재생)
                    EndDrag();
                }
            }
        }
    }

    void BeginDrag(int slot, SpellMarble m, Vector2 mouse)
    {
        dragSlot = slot;
        dragStartTime = Time.unscaledTime;
        PlaySound(clickSound);
        EnsureDragGhost();               // 미할당 시 런타임 생성
        if (dragGhost != null)
        {
            dragGhost.gameObject.SetActive(true);
            dragGhost.position = mouse;
            dragGhost.localScale = Vector3.one * dragPopScale; // 확대 상태로 시작 → Update에서 줄어듦
            Sprite s = hud.ResolveIcon(m);   // 벨트와 동일한 슈트×등급 스프라이트
            if (dragGhostImage != null) { dragGhostImage.sprite = s; dragGhostImage.enabled = s != null; }
        }
        hud.SetDragSuppressed(slot);     // 집어든 구슬은 벨트에서 잠시 숨김(이동 느낌)
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
    }

    void EndDrag()
    {
        dragSlot = -1;
        lastHovered = -1;
        UpdateCancelFeedback(false, Vector2.zero); // 벨트 색/라벨 원복
        if (dragGhost != null) dragGhost.gameObject.SetActive(false);
        if (hud != null) hud.SetDragSuppressed(-1); // 숨겼던 슬롯 아이콘 복구
    }

    // 드래그 중 취소 영역(벨트) 위 여부를 붉은 표시 + "사용 취소" 라벨로 알림
    void UpdateCancelFeedback(bool cancel, Vector2 mouse)
    {
        if (beltImage != null)
        {
            beltImage.color = cancel ? cancelTint : beltColor0;   // 기존 이미지를 틴트
        }
        else
        {
            EnsureCancelOverlay();                                 // 이미지 없으면 자동 오버레이
            if (cancelOverlay != null && cancelOverlay.activeSelf != cancel)
                cancelOverlay.SetActive(cancel);
        }

        if (cancelLabel != null)
        {
            if (cancelLabel.activeSelf != cancel) cancelLabel.SetActive(cancel);
            if (cancel) ((RectTransform)cancelLabel.transform).position = mouse; // 커서 근처
        }
    }

    // beltImage 미지정 시 손패(handRoot)를 덮는 붉은 반투명 사각형을 런타임 생성
    void EnsureCancelOverlay()
    {
        if (cancelOverlay != null) return;
        RectTransform hand = selection != null ? selection.handRoot : null;
        if (hand == null) return;

        GameObject go = new GameObject("SpellCancelOverlay", typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(hand, false);            // 손패 자식 → 위치·스케일 자동 추종
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;          // handRoot 영역을 꽉 채움
        Image img = go.AddComponent<Image>();
        img.color = cancelOverlayColor;
        img.raycastTarget = false;
        rt.SetAsLastSibling();                // 벨트 위에 덧그림
        cancelOverlay = go;
        go.SetActive(false);
    }

    // 툴팁을 홀로그램보다 앞에 그리게 만든다.
    // ⚠️ 반드시 tooltipRoot가 활성인 상태에서 호출할 것.
    //    비활성이면 Unity가 부모 캔버스를 찾지 못해 overrideSorting이 false로 되돌아간다.
    public void EnsureTooltipOnTop()
    {
        if (tooltipRoot == null || !tooltipRoot.activeInHierarchy) return;
        Canvas tc = tooltipRoot.GetComponent<Canvas>();
        if (tc == null) tc = tooltipRoot.AddComponent<Canvas>();
        tc.overrideSorting = true;
        tc.sortingOrder = TooltipSortingOrder;
        tooltipRoot.transform.SetAsLastSibling(); // 정렬 순서와 계층 순서 둘 다 맞춘다
    }

    void EndAll()
    {
        EndDrag();
        lastHovered = -1;
        HoveredSlot = -1;
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
        ResetHoverScales(); // 선택 모드 벗어나면 슬롯 크기 원복
    }

    void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip, volume);
    }

    // 해당 슬롯에 마블이 실제로 있는지(빈 슬롯 호버 사운드 방지용)
    bool HasMarble(int slot)
    {
        return caster != null && slot >= 0 && slot < caster.Slots.Count && caster.Slots[slot] != null;
    }

    // dragGhost 미할당 시 캔버스 아래 커서 추종 아이콘을 런타임 생성(배선 없이도 동작)
    void EnsureDragGhost()
    {
        if (dragGhost != null || canvas == null) return;
        GameObject go = new GameObject("SpellDragGhost", typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;                         // 드롭 판정 방해 금지
        RectTransform slot0 = hud != null ? hud.GetSlotRect(0) : null;
        rt.sizeDelta = (slot0 != null) ? slot0.rect.size : new Vector2(64f, 64f);
        rt.SetAsLastSibling();                             // 항상 최상단
        dragGhost = rt;
        dragGhostImage = img;
        go.SetActive(false);
    }

    // 슬롯 기본 스케일 최초 캡처(확대 적용 전 값이라야 정확)
    void EnsureBaseScales()
    {
        int n = hud.SlotCount;
        if (slotBaseScale != null && slotBaseScale.Length == n) return;
        slotBaseScale = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            RectTransform rt = hud.GetSlotRect(i);
            slotBaseScale[i] = rt != null ? rt.localScale : Vector3.one;
        }
    }

    // 지정 슬롯만 hoverScale로, 나머지는 기본 크기로 부드럽게 보간
    void ApplyHoverScale(int highlight)
    {
        EnsureBaseScales();
        float t = hoverLerpSpeed * Time.unscaledDeltaTime;
        for (int i = 0; i < slotBaseScale.Length; i++)
        {
            RectTransform rt = hud.GetSlotRect(i);
            if (rt == null) continue;
            Vector3 target = (i == highlight) ? slotBaseScale[i] * hoverScale : slotBaseScale[i];
            rt.localScale = Vector3.Lerp(rt.localScale, target, t);
        }
    }

    // 모든 슬롯을 기본 크기로 복귀
    void ResetHoverScales()
    {
        if (slotBaseScale == null) return;
        float t = hoverLerpSpeed * Time.unscaledDeltaTime;
        for (int i = 0; i < slotBaseScale.Length; i++)
        {
            RectTransform rt = hud.GetSlotRect(i);
            if (rt != null) rt.localScale = Vector3.Lerp(rt.localScale, slotBaseScale[i], t);
        }
    }

    // 벨트(슬롯 또는 손패 영역) 위인지 — 드롭 시 여기면 사용 취소
    bool IsOverBelt(Vector2 screenPoint)
    {
        if (SlotUnderPoint(screenPoint) >= 0) return true;
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
        RectTransform hand = selection != null ? selection.handRoot : null;
        return hand != null && RectTransformUtility.RectangleContainsScreenPoint(hand, screenPoint, cam);
    }

    int SlotUnderPoint(Vector2 screenPoint)
    {
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
        for (int i = 0; i < hud.SlotCount; i++)
        {
            RectTransform rt = hud.GetSlotRect(i);
            if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPoint, cam))
                return i;
        }
        return -1;
    }

    void UpdateTooltip(int slot, Vector2 mouse)
    {
        if (tooltipRoot == null) return;

        SpellMarble m = (slot >= 0 && slot < caster.Slots.Count) ? caster.Slots[slot] : null;
        if (m == null || m.ability == null) { tooltipRoot.SetActive(false); return; }

        bool wasHidden = !tooltipRoot.activeSelf;
        tooltipRoot.SetActive(true);
        if (wasHidden) EnsureTooltipOnTop();
        if (tooltipText != null)
            tooltipText.text =
                "<b>" + SpellText.Name(m.ability) + "</b>\n" +
                SuitInfo.RichLabel(m.suit) + "  <color=#FFD24A>[" + m.grade + "]</color>\n" +
                SpellText.Desc(m.ability);
        if (tooltipIcon != null)
        {
            Sprite ic = m.icon != null ? m.icon : m.ability.icon; // 스킬 고유 아이콘(SpellMarble.icon) 우선, 없으면 능력 아이콘 폴백
            tooltipIcon.sprite = ic;
            tooltipIcon.enabled = ic != null;
        }
        RectTransform rrt = (RectTransform)tooltipRoot.transform;
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rrt); // 설명 길이에 맞춰 박스 높이 즉시 반영

        // 커서를 따라다니지 않고 해당 구슬 바로 위에 고정한다.
        // 그 자리에 아이콘 홀로그램이 떠 있다가 툴팁으로 바뀌는 연출이라,
        // 위치가 어긋나면 "확장"이 아니라 "다른 창이 뜬 것"으로 보인다.
        RectTransform slotRect = hud != null ? hud.GetSlotRect(slot) : null;
        if (slotRect != null)
        {
            Vector3[] c = new Vector3[4];
            slotRect.GetWorldCorners(c);
            float topY = Mathf.Max(c[1].y, c[2].y);
            float midX = (c[0].x + c[3].x) * 0.5f;
            // 박스 아래변이 구슬 위에 오도록 — 피벗과 무관하게 맞춘다.
            Vector3[] t = new Vector3[4];
            rrt.GetWorldCorners(t);
            float halfH = (Mathf.Max(t[1].y, t[2].y) - Mathf.Min(t[0].y, t[3].y)) * 0.5f;
            rrt.position = new Vector3(midX, topY + halfH + HologramGap * rrt.lossyScale.y, rrt.position.z);
        }
        else rrt.position = mouse; // 슬롯을 못 찾으면 기존 동작 유지
    }

    // 구슬과 홀로그램/툴팁 사이 간격(px, 캔버스 기준). SpellOrbHologram과 같은 값을 써야
    // 아이콘이 툴팁으로 바뀔 때 위치가 튀지 않는다.
    public const float HologramGap = 14f;

    // 툴팁 정렬 순서. 홀로그램(HologramSortingOrder)보다 높아야 가려지지 않는다.
    public const int TooltipSortingOrder = 310;
}
