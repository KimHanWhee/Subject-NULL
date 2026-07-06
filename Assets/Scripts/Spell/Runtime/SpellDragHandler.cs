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

    private int dragSlot = -1;

    void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        if (dragGhost != null) dragGhost.gameObject.SetActive(false);
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
    }

    void Update()
    {
        if (selection == null || hud == null || caster == null) return;

        // 선택 모드가 아니면 모든 상태 정리
        if (!selection.IsSelecting) { EndAll(); return; }
        if (Mouse.current == null) return;

        Vector2 mouse = Mouse.current.position.ReadValue();
        int hovered = SlotUnderPoint(mouse);

        // 툴팁: 드래그 중이 아닐 때만 호버 슬롯 표시
        UpdateTooltip(dragSlot < 0 ? hovered : -1, mouse);

        // 드래그 시작
        if (dragSlot < 0 && hovered >= 0 && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SpellMarble m = hovered < caster.Slots.Count ? caster.Slots[hovered] : null;
            if (m != null) BeginDrag(hovered, m, mouse);
        }

        // 드래그 진행/드롭
        if (dragSlot >= 0)
        {
            if (dragGhost != null) dragGhost.position = mouse;

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                Vector3 world = worldCamera != null
                    ? worldCamera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, 0f))
                    : Vector3.zero;
                caster.Activate(dragSlot, new Vector2(world.x, world.y)); // Plan SC: FR-09
                EndDrag();
            }
        }
    }

    void BeginDrag(int slot, SpellMarble m, Vector2 mouse)
    {
        dragSlot = slot;
        if (dragGhost != null)
        {
            dragGhost.gameObject.SetActive(true);
            dragGhost.position = mouse;
            if (dragGhostImage != null) { dragGhostImage.sprite = m.icon; dragGhostImage.enabled = m.icon != null; }
        }
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
    }

    void EndDrag()
    {
        dragSlot = -1;
        if (dragGhost != null) dragGhost.gameObject.SetActive(false);
    }

    void EndAll()
    {
        EndDrag();
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
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

        tooltipRoot.SetActive(true);
        if (tooltipText != null)
            tooltipText.text = m.ability.abilityName + " [" + m.grade + "]\n" + m.ability.description;
        ((RectTransform)tooltipRoot.transform).position = mouse;
    }
}
