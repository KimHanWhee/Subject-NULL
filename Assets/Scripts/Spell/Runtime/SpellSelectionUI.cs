using UnityEngine;
using UnityEngine.InputSystem;

// Design Ref: §5.1 — Ctrl 홀드 선택 모드: 손패 상승 + 게임 슬로우(선택 UI는 unscaled).
// Plan SC: FR-07(선택 모드) / FR-08(슬로우)
public class SpellSelectionUI : MonoBehaviour
{
    public RectTransform handRoot;                       // 상승/확대 대상(HUD 컨테이너)
    public CanvasGroup ctrlHint;                         // "▲ Ctrl" 안내 탭 — 평소 표시, 선택 중 페이드아웃
    public Vector2 raisedOffset = new Vector2(0f, 120f); // 위로 올라오는 양(px)
    public float raisedScale = 5f;                     // 선택 중 확대 배율(1=확대 없음)
    public float raiseSpeed = 10f;                       // unscaled 보간 속도(위치·스케일 공용)
    public float slowScale = 0.3f;                       // Plan SC: FR-08

    private Vector2 basePos;
    private Vector3 baseScale = Vector3.one;
    private bool selecting;
    private int slowHandle = -1;

    public bool IsSelecting => selecting;

    void Awake()
    {
        if (handRoot != null)
        {
            basePos = handRoot.anchoredPosition;
            baseScale = handRoot.localScale;
        }
    }

    void Update()
    {
        bool ctrl = Keyboard.current != null &&
                    (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);

        if (ctrl && !selecting) Enter();
        else if (!ctrl && selecting) Exit();

        if (handRoot != null)
        {
            float t = raiseSpeed * Time.unscaledDeltaTime; // 슬로우와 무관하게 부드럽게

            Vector2 targetPos = selecting ? basePos + raisedOffset : basePos;
            handRoot.anchoredPosition = Vector2.Lerp(handRoot.anchoredPosition, targetPos, t);

            // 부모 SpellHand를 확대 → 벨트·구슬 5개 모두 함께 확대
            Vector3 targetScale = selecting ? baseScale * raisedScale : baseScale;
            handRoot.localScale = Vector3.Lerp(handRoot.localScale, targetScale, t);
        }

        // "▲ Ctrl" 안내 탭: 선택 중엔 사라지고 평소엔 표시
        if (ctrlHint != null)
            ctrlHint.alpha = Mathf.MoveTowards(ctrlHint.alpha, selecting ? 0f : 1f, 6f * Time.unscaledDeltaTime);
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
