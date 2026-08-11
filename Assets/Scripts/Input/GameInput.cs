using UnityEngine;
using UnityEngine.InputSystem;

// 게임플레이 입력의 단일 창구 — 키보드/마우스와 터치를 합쳐서 내보낸다.
//
// 왜 필요한가:
//  ① 터치 전용 기기에서는 Keyboard.current / Mouse.current 가 모두 null이다.
//     Input System은 터치를 마우스로 자동 변환해주지 않는다.
//     예전에는 PlayerController가 이 둘을 null 검사 없이 6곳에서 직접 읽어서,
//     휴대폰 브라우저에서는 첫 프레임부터 예외가 쏟아져 아무것도 못 움직였다.
//  ② 입력을 읽는 곳이 흩어져 있으면 터치 경로를 추가할 때마다 빠뜨리는 데가 생긴다.
//
// 터치 값은 TouchControls가 매 프레임 밀어 넣는다(Push* 메서드).
// 여기서는 "어느 쪽 입력을 쓸지"만 판단한다.
public static class GameInput
{
    // ── 최종 결과(게임 코드가 읽는 값) ─────────────────────────
    public static Vector2 Move { get; private set; }          // 이동 방향(세기 포함, 최대 1)
    public static bool FireHeld { get; private set; }         // 연사 중인가
    public static Vector2 AimDirection { get; private set; }  // 조준 방향(정규화)
    public static bool DashPressed { get; private set; }      // 이번 프레임 대시 입력

    // 절대 좌표가 필요한 능력(♠ 비격진천뢰)용 조준점.
    // 마우스면 커서 위치, 터치면 조준 방향으로 일정 거리 앞.
    public static Vector2 AimWorld { get; private set; }

    // 포인터 화면 좌표 — 스펠 드래그가 쓴다(마우스 커서 또는 끌고 있는 손가락)
    public static Vector2 PointerScreen { get; private set; }
    public static bool PointerDown { get; private set; }
    public static bool PointerPressedThisFrame { get; private set; }
    public static bool PointerReleasedThisFrame { get; private set; }

    // 스펠 선택 모드 — 키보드는 Shift 홀드, 터치는 스와이프 토글
    public static bool SpellSelecting { get { return keySelecting || touchSelectToggle; } }

    // 터치 컨트롤을 화면에 띄울지. 기기를 맞히려 들지 않고 "지금 뭘로 조작 중인가"를 본다.
    public static bool TouchMode { get; private set; }

    // ── 터치 쪽에서 밀어 넣는 값 ───────────────────────────────
    static Vector2 tMove, tAim;
    static bool tFire, tDash;
    static Vector2 tPointer;
    static bool tPointerDown, tPointerPressed, tPointerReleased;
    static bool touchSelectToggle;
    static bool keySelecting;

    // 필드 탭(컨트롤 영역 밖) — ♠ 비격진천뢰가 소비한다
    static bool fieldTapPending;
    static Vector2 fieldTapWorld;

    // 마우스/키보드 ↔ 터치 전환. 미세한 마우스 떨림으로 컨트롤이 깜빡이지 않게 유예를 둔다.
    const float SwitchGrace = 0.5f;
    static float lastTouchTime = -999f;
    static float lastMouseKeyTime = -999f;

    [Tooltip("터치 조준 시 AimWorld를 플레이어 앞 몇 유닛으로 볼지")]
    const float TouchAimNear = 2.5f;
    const float TouchAimFar = 9f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Reset()
    {
        // 도메인 리로드가 꺼져 있으면 static이 씬 전환에도 살아남는다 — 시작 시 초기화.
        Move = Vector2.zero; FireHeld = false; DashPressed = false;
        touchSelectToggle = false; keySelecting = false;
        fieldTapPending = false;
        lastTouchTime = -999f; lastMouseKeyTime = -999f;
        // 터치스크린이 있는 기기면 처음부터 컨트롤을 보여준다(휴대폰에서 뭘 눌러야 할지 알 수 있게)
        TouchMode = Touchscreen.current != null;
    }

    // TouchControls가 매 프레임 호출
    public static void PushTouch(Vector2 move, Vector2 aim, bool fire, bool dash)
    {
        tMove = move; tAim = aim; tFire = fire; tDash = dash;
        if (move.sqrMagnitude > 0.0001f || fire || dash) lastTouchTime = Time.unscaledTime;
    }

    public static void PushTouchPointer(Vector2 screen, bool down, bool pressed, bool released)
    {
        tPointer = screen; tPointerDown = down; tPointerPressed = pressed; tPointerReleased = released;
        if (down || pressed) lastTouchTime = Time.unscaledTime;
    }

    public static void SetSpellSelectToggle(bool on) { touchSelectToggle = on; }
    public static void ToggleSpellSelect() { touchSelectToggle = !touchSelectToggle; }

    public static void PushFieldTap(Vector2 world) { fieldTapPending = true; fieldTapWorld = world; }

    // ♠ 비격진천뢰 — 탭한 자리에 떨군다. 한 번 쓰면 사라진다(같은 탭으로 두 번 떨어지지 않게).
    public static bool TryConsumeFieldTap(out Vector2 world)
    {
        world = fieldTapWorld;
        if (!fieldTapPending) return false;
        fieldTapPending = false;
        return true;
    }

    // GameInputPump가 매 프레임 호출 — 두 입력을 합쳐 최종값을 만든다
    public static void Tick(Transform player)
    {
        Keyboard kb = Keyboard.current;
        Mouse ms = Mouse.current;

        // ── 키보드/마우스 ──
        Vector2 kMove = Vector2.zero;
        bool kFire = false, kDash = false;
        keySelecting = false;

        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) kMove.x -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) kMove.x += 1f;
            if (kb.upArrowKey.isPressed || kb.wKey.isPressed) kMove.y += 1f;
            if (kb.downArrowKey.isPressed || kb.sKey.isPressed) kMove.y -= 1f;
            kMove = Vector2.ClampMagnitude(kMove, 1f); // 대각선이 √2배 빨라지지 않게
            kDash = kb.spaceKey.wasPressedThisFrame;
            keySelecting = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;

            if (kMove.sqrMagnitude > 0f || kDash || keySelecting) lastMouseKeyTime = Time.unscaledTime;
        }

        Vector2 mouseScreen = Vector2.zero;
        bool mDown = false, mPressed = false, mReleased = false;
        if (ms != null)
        {
            mouseScreen = ms.position.ReadValue();
            kFire = ms.leftButton.isPressed;
            mDown = kFire;
            mPressed = ms.leftButton.wasPressedThisFrame;
            mReleased = ms.leftButton.wasReleasedThisFrame;
            if (kFire || ms.delta.ReadValue().sqrMagnitude > 4f) lastMouseKeyTime = Time.unscaledTime;
        }

        // ── 어느 쪽을 쓸지 ──
        bool useTouch = lastTouchTime > lastMouseKeyTime;
        if (Time.unscaledTime - Mathf.Max(lastTouchTime, lastMouseKeyTime) > SwitchGrace)
            useTouch = TouchMode; // 아무 입력도 없으면 직전 상태 유지(깜빡임 방지)
        TouchMode = useTouch;

        // ── 합치기 ──
        if (useTouch)
        {
            Move = tMove;
            FireHeld = tFire;
            DashPressed = tDash;
            AimDirection = tAim.sqrMagnitude > 0.0001f ? tAim.normalized : AimDirection;

            // 스틱을 살짝 밀면 가깝게, 끝까지 밀면 멀리 — 절대 좌표가 필요한 능력의 조준감
            float reach = Mathf.Lerp(TouchAimNear, TouchAimFar, Mathf.Clamp01(tAim.magnitude));
            Vector2 origin = player != null ? (Vector2)player.position : Vector2.zero;
            AimWorld = origin + AimDirection * reach;

            PointerScreen = tPointer;
            PointerDown = tPointerDown;
            PointerPressedThisFrame = tPointerPressed;
            PointerReleasedThisFrame = tPointerReleased;
        }
        else
        {
            Move = kMove;
            FireHeld = kFire;
            DashPressed = kDash;

            Vector2 world = ScreenToWorld(mouseScreen);
            AimWorld = world;
            Vector2 origin = player != null ? (Vector2)player.position : Vector2.zero;
            Vector2 d = world - origin;
            if (d.sqrMagnitude > 0.0001f) AimDirection = d.normalized;

            PointerScreen = mouseScreen;
            PointerDown = mDown;
            PointerPressedThisFrame = mPressed;
            PointerReleasedThisFrame = mReleased;

            touchSelectToggle = false; // 마우스로 돌아오면 터치 토글은 풀어준다
        }

        // 다음 프레임을 위해 1프레임짜리 신호는 비운다(터치 쪽이 다시 밀어 넣는다)
        tDash = false;
        tPointerPressed = false;
        tPointerReleased = false;
    }

    public static Vector2 ScreenToWorld(Vector2 screen)
    {
        Camera c = Camera.main;
        if (c == null) return Vector2.zero;
        Vector3 w = c.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
        return new Vector2(w.x, w.y);
    }
}

// GameInput.Tick을 매 프레임 돌려주는 구동기.
// 다른 어떤 스크립트보다 먼저 돌아야 한다 — 그래야 같은 프레임에 읽는 값이 최신이다.
[DefaultExecutionOrder(-1000)]
public class GameInputPump : MonoBehaviour
{
    private Transform player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        GameObject go = new GameObject("~GameInput");
        go.AddComponent<GameInputPump>();
        Object.DontDestroyOnLoad(go);
    }

    void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
        GameInput.Tick(player);
    }
}
