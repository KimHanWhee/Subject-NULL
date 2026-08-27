using UnityEngine;

// 스펠 선택 중 자유 시점 — 포인터가 화면 가장자리에 가면 그쪽으로 카메라가 이동한다.
// (LoL의 잠금 해제 카메라와 같은 감각)
//
// 왜 필요한가:
//   스펠은 드롭한 "위치"에 발동한다. 화면이 곧 사거리라서, 화면 밖의 적 무리에는
//   손을 쓸 수 없었다. 벨트를 연 동안만 시야를 풀어 조준 범위를 넓힌다.
//
// 왜 로컬 오프셋인가:
//   카메라는 Player의 자식이라 부모를 따라다닌다. 월드 좌표를 직접 쓰면 추종과 싸우게 되므로
//   localPosition에 오프셋만 얹는다. 선택이 끝나면 0으로 되돌아가며 자연히 플레이어에게 복귀한다.
//
// 데스크톱: 커서를 가장자리로
// 모바일  : 오브를 끌고 가장자리로 (같은 규칙 — 포인터 좌표만 다르다)
[DefaultExecutionOrder(100)] // SpellSelectionUI가 줌을 정한 뒤에 움직인다
public class SpellCameraPan : MonoBehaviour
{
    [Tooltip("가장자리로 판정할 화면 안쪽 폭(화면 짧은 변 대비 비율)")]
    public float edgeBand = 0.16f;

    [Tooltip("최대 이동 속도(유닛/초)")]
    public float panSpeed = 22f;

    [Tooltip("플레이어로부터 벗어날 수 있는 최대 거리(유닛)")]
    public float maxDistance = 26f;

    [Tooltip("복귀 속도 — 선택이 끝나면 이 속도로 플레이어에게 돌아온다")]
    public float returnLerp = 8f;

    private Camera cam;
    private SpellSelectionUI selection;
    private Vector2 offset;      // 플레이어 기준 현재 오프셋
    private float baseZ;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => TryAttach();
        TryAttach();
    }

    static void TryAttach()
    {
        Camera c = Camera.main;
        if (c == null || c.GetComponent<SpellCameraPan>() != null) return;
        // 스펠 선택 UI가 있는 씬(게임 화면)에만
        if (FindFirstObjectByType<SpellSelectionUI>() == null) return;
        c.gameObject.AddComponent<SpellCameraPan>();
    }

    void Awake()
    {
        cam = GetComponent<Camera>();
        baseZ = transform.localPosition.z; // -10 유지(직교라 값 자체는 무관하지만 원본을 지킨다)
    }

    void LateUpdate()
    {
        if (cam == null) return;
        if (selection == null) selection = FindFirstObjectByType<SpellSelectionUI>();

        bool active = selection != null && selection.IsSelecting;

        if (active) Pan();
        else
        {
            // 선택이 끝나면 부드럽게 복귀 — 툭 끊기면 화면이 튄다
            float k = 1f - Mathf.Exp(-returnLerp * Time.unscaledDeltaTime);
            offset = Vector2.Lerp(offset, Vector2.zero, k);
            if (offset.sqrMagnitude < 0.0004f) offset = Vector2.zero;
        }

        transform.localPosition = new Vector3(offset.x, offset.y, baseZ);
    }

    void Pan()
    {
        // 포인터가 없으면(모바일에서 아직 오브를 안 집었으면) 움직이지 않는다
        if (!HasPointer()) return;

        Vector2 p = GameInput.PointerScreen;
        Vector2 dir = EdgeDirection(p);
        if (dir == Vector2.zero) return;

        // 선택 중에는 시간이 거의 멈춰 있다(TimeController) — unscaled로 움직여야 반응한다
        offset += dir * panSpeed * Time.unscaledDeltaTime;
        offset = Vector2.ClampMagnitude(offset, maxDistance);
    }

    bool HasPointer()
    {
        // 터치는 실제로 끌고 있을 때만 유효하다. 손을 뗀 뒤의 마지막 좌표로 계속 밀리면 안 된다.
        if (GameInput.TouchMode) return GameInput.PointerDown;
        return true; // 마우스는 항상 화면 위에 있다
    }

    // 화면 가장자리 밴드 안이면 그 방향, 아니면 0.
    // 가장자리에 가까울수록 빨라진다(0~1 가중).
    Vector2 EdgeDirection(Vector2 screen)
    {
        float band = Mathf.Min(Screen.width, Screen.height) * edgeBand;
        if (band <= 1f) return Vector2.zero;

        float x = 0f, y = 0f;
        if (screen.x < band) x = -(band - screen.x) / band;
        else if (screen.x > Screen.width - band) x = (screen.x - (Screen.width - band)) / band;

        if (screen.y < band) y = -(band - screen.y) / band;
        else if (screen.y > Screen.height - band) y = (screen.y - (Screen.height - band)) / band;

        Vector2 d = new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
        return d;
    }

    // 화면 밖으로 나갔다 돌아올 때를 대비한 안전망
    void OnDisable() { if (cam != null) transform.localPosition = new Vector3(0f, 0f, baseZ); }
}
