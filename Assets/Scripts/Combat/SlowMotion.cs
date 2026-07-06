using UnityEngine;

// Design Ref: §2.2 — 전역 슬로우모션 제어. unscaled 타이머로 복구 보장, 쿨다운.
// + 카메라 줌인 펀치 연출(슬로우와 동기화, unscaled 보간).
public class SlowMotion : MonoBehaviour
{
    [Header("Slow Motion")] // Plan SC: FR-06 — Inspector 튜닝
    public float slowScale = 0.3f;  // Plan SC: FR-06 — 슬로우 강도
    public float duration = 0.35f;  // 실시간 지속(초)
    public float cooldown = 1f;     // 재발동 쿨다운(초)

    [Header("Camera Zoom")] // 니어미스 줌인 펀치 연출
    public Camera targetCamera;      // 비우면 Camera.main 자동
    public float zoomFactor = 0.9f;  // orthographicSize 배율 (작을수록 확대, 0.9 = 10% 줌인)
    public float zoomLerpSpeed = 8f; // 줌 보간 속도 (unscaled 기준)

    private float defaultFixedDelta;
    private float endUnscaled;
    private float nextAllowedUnscaled;
    private bool active;

    private float defaultOrthoSize;  // 원래 카메라 사이즈
    private float targetOrthoSize;   // 매 프레임 보간 목표

    void Awake()
    {
        defaultFixedDelta = Time.fixedDeltaTime;

        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null)
        {
            defaultOrthoSize = targetCamera.orthographicSize;
            targetOrthoSize = defaultOrthoSize;
        }
    }

    // Plan SC: FR-03 — 진행 중이거나 쿨다운 중이면 무시 (중첩/연속 발동 방지)
    public void Trigger()
    {
        if (active || Time.unscaledTime < nextAllowedUnscaled) return;

        // spell-marble Design §4.1 — 시간 제어는 TimeController 단일 진실원 경유(선택 슬로우와 공존).
        // TimeController 미배치 씬에서는 레거시 직접 제어로 폴백(니어미스 회귀 방지).
        if (TimeController.Instance != null)
        {
            TimeController.Instance.Pulse(slowScale, duration);
        }
        else
        {
            Time.timeScale = slowScale;
            Time.fixedDeltaTime = defaultFixedDelta * slowScale; // 물리도 비례 (끊김 방지)
        }
        endUnscaled = Time.unscaledTime + duration;          // Plan SC: FR-02 — 실시간 기준(카메라 줌 타이밍)
        active = true;

        targetOrthoSize = defaultOrthoSize * zoomFactor;     // 줌인 시작
    }

    void Update()
    {
        // 카메라 줌 보간은 항상 수행 (발동 시 줌인 / 복구 시 줌아웃) — unscaled로 슬로우 중에도 진행
        if (targetCamera != null && targetCamera.orthographic)
        {
            targetCamera.orthographicSize = Mathf.Lerp(
                targetCamera.orthographicSize, targetOrthoSize,
                zoomLerpSpeed * Time.unscaledDeltaTime);
        }

        if (!active) return;
        if (Time.unscaledTime >= endUnscaled) // Plan SC: FR-07 — unscaled로 확실히 복구
        {
            Restore();
            nextAllowedUnscaled = Time.unscaledTime + cooldown;
        }
    }

    void Restore()
    {
        // TimeController 경유 시 시간 복구는 펄스 만료로 자동 처리됨(여기서 timeScale을 만지면 선택 홀드를 덮어씀).
        // 폴백(TimeController 미배치)일 때만 직접 복구.
        if (TimeController.Instance == null)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultFixedDelta;
        }
        active = false;

        targetOrthoSize = defaultOrthoSize; // 줌아웃 (Update가 보간으로 복귀)
    }

    // Plan SC: FR-07 — 안전망: 씬 종료/비활성 시 카메라 정상화 (잔존 방지)
    // 시간 복구는 TimeController가 소유(있을 때). 폴백일 때만 직접 정상화.
    void OnDisable()
    {
        if (TimeController.Instance == null)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultFixedDelta;
        }
        if (targetCamera != null) targetCamera.orthographicSize = defaultOrthoSize;
    }
}
