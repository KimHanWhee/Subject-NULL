using UnityEngine;
using UnityEngine.InputSystem;

// Design Ref: §5.1 — Shift 홀드 선택 모드: 손패 상승 + 게임 슬로우(선택 UI는 unscaled).
// (원래 Ctrl이었으나 WebGL 배포 시 브라우저 단축키(Ctrl+W=탭 닫기) 충돌로 Shift로 변경)
// Plan SC: FR-07(선택 모드) / FR-08(슬로우)
public class SpellSelectionUI : MonoBehaviour
{
    public RectTransform handRoot;                       // 상승/확대 대상(HUD 컨테이너)
    public CanvasGroup ctrlHint;                         // "▲ Shift" 안내 탭 — 평소 표시, 선택 중 페이드아웃(필드명은 씬 직렬화 유지)
    public Vector2 raisedOffset = new Vector2(0f, 120f); // 위로 올라오는 양(px) — 스태미너 창 높이까지만
    public float raisedScale = 2.4f;                     // 선택 중 확대 배율(1=확대 없음)
    public float raiseSpeed = 10f;                       // unscaled 보간 속도(위치·스케일 공용)
    public float slowScale = 0.01f;                      // Plan SC: FR-08 — 씬 인스펙터 값과 동일 기준(0.01)

    [Header("Ctrl 시 카메라 시야 확대")]
    public float zoomedOrthoSize = 9f;                   // 선택 중 카메라 orthographicSize(0 이하=미사용) — 시야 넓게
    public float zoomSpeed = 8f;                         // unscaled 보간 속도

    private Vector2 basePos;
    private Vector3 baseScale = Vector3.one;
    private bool selecting;
    private int slowHandle = -1;
    private SpellCaster caster;          // 과부하(조커) 상태 조회용
    private float nextOverloadNotice;    // 알림 스팸 방지(1초 스로틀)
    private Camera zoomCam;              // 시야 확대 대상(메인 카메라)
    private float baseOrtho;             // 원래 orthographicSize
    private bool hasBaseOrtho;

    public bool IsSelecting => selecting;

    void Awake()
    {
        if (handRoot != null)
        {
            basePos = handRoot.anchoredPosition;


            baseScale = handRoot.localScale;
        }
        caster = FindFirstObjectByType<SpellCaster>();
    }

    void Update()
    {
        // 키보드는 Shift 홀드, 터치는 하단 스와이프 토글 — GameInput이 합쳐서 준다.
        // (예전엔 여기와 PlayerController가 각자 Shift를 읽어서, 판정이 어긋날 여지가 있었다)
        bool ctrl = GameInput.SpellSelecting;

        // 과부하(조커 경고~발동 전) 중엔 벨트 잠금 — Shift 선택 불가 + 알림
        bool overloaded = caster != null && caster.IsOverloaded;
        if (overloaded && selecting)
        {
            Exit(); // 과부하 진입 순간 선택 중이었으면 강제 해제
            GameInput.SetSpellSelectToggle(false); // 터치 토글도 내린다(안 내리면 곧바로 다시 열린다)
        }

        if (ctrl && !selecting)
        {
            if (overloaded)
            {
                if (Time.unscaledTime >= nextOverloadNotice)
                {
                    nextOverloadNotice = Time.unscaledTime + 1f;
                    Vector2 pos = Camera.main != null ? (Vector2)Camera.main.transform.position : Vector2.zero;
                    FloatingText.Show(pos + Vector2.down * 1.5f, "스펠 오브 과부하 상태입니다!", new Color(1f, 0.4f, 0.35f), 4.5f, 1.2f);
                }
            }
            else Enter();
        }
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

        // 카메라 시야: 선택 중엔 넓게(zoom out), 평소엔 원래 크기로 복귀
        EnsureCam();
        if (zoomCam != null && zoomCam.orthographic && hasBaseOrtho && zoomedOrthoSize > 0f)
        {
            float target = selecting ? zoomedOrthoSize : baseOrtho;
            zoomCam.orthographicSize = Mathf.Lerp(zoomCam.orthographicSize, target, zoomSpeed * Time.unscaledDeltaTime);
        }
    }

    void EnsureCam()
    {
        if (zoomCam != null) return;
        zoomCam = Camera.main;
        if (zoomCam != null && zoomCam.orthographic && !hasBaseOrtho)
        {
            baseOrtho = zoomCam.orthographicSize;
            hasBaseOrtho = true;
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

    // 안전망: 비활성 시 슬로우 해제 + 카메라 시야 원복(잔존 방지)
    void OnDisable()
    {
        ReleaseSlow();
        selecting = false;
        if (zoomCam != null && zoomCam.orthographic && hasBaseOrtho)
            zoomCam.orthographicSize = baseOrtho;
    }
}
