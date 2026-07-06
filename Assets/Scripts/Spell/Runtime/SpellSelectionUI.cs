using UnityEngine;
using UnityEngine.InputSystem;

// Design Ref: §5.1 — Ctrl 홀드 선택 모드: 손패 상승 + 게임 슬로우(선택 UI는 unscaled).
// Plan SC: FR-07(선택 모드) / FR-08(슬로우)
public class SpellSelectionUI : MonoBehaviour
{
    public RectTransform handRoot;                       // 상승 대상(HUD 컨테이너)
    public Vector2 raisedOffset = new Vector2(0f, 120f); // 위로 올라오는 양(px)
    public float raiseSpeed = 10f;                       // unscaled 보간 속도
    public float slowScale = 0.3f;                       // Plan SC: FR-08

    private Vector2 basePos;
    private bool selecting;
    private int slowHandle = -1;

    public bool IsSelecting => selecting;

    void Awake()
    {
        if (handRoot != null) basePos = handRoot.anchoredPosition;
    }

    void Update()
    {
        bool ctrl = Keyboard.current != null &&
                    (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);

        if (ctrl && !selecting) Enter();
        else if (!ctrl && selecting) Exit();

        if (handRoot != null)
        {
            Vector2 target = selecting ? basePos + raisedOffset : basePos;
            handRoot.anchoredPosition = Vector2.Lerp(
                handRoot.anchoredPosition, target, raiseSpeed * Time.unscaledDeltaTime);
        }
    }

    void Enter()
    {
        selecting = true;
        if (TimeController.Instance != null) slowHandle = TimeController.Instance.PushHold(slowScale);
    }

    void Exit()
    {
        selecting = false;
        ReleaseSlow();
    }

    void ReleaseSlow()
    {
        if (TimeController.Instance != null && slowHandle != -1)
            TimeController.Instance.PopHold(slowHandle);
        slowHandle = -1;
    }

    // 안전망: 비활성 시 슬로우 해제(잔존 방지)
    void OnDisable()
    {
        ReleaseSlow();
        selecting = false;
    }
}
