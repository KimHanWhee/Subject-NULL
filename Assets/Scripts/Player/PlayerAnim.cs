using UnityEngine;

// 플레이어 방향별 스프라이트 애니메이션 — east/west가 단순 반전이 아니라 별도 스프라이트라
// Animator+flipX 대신 방향별 프레임을 직접 재생한다. PlayerController가 상태/방향을 주입.
public class PlayerAnim : MonoBehaviour
{
    public Sprite[] idleEast, idleWest, runEast, runWest, dead;
    public float idleFps = 6f, runFps = 8f, deadFps = 12f;

    enum St { Idle, Run, Dead }
    private St state = St.Idle;
    private bool right = true;
    private SpriteRenderer sr;
    private float timer;
    private int frame;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        Apply(); // 첫 프레임 즉시 표시(빈 스프라이트 방지)
    }

    // 이동 여부 — Run/Idle 전환(사망 중엔 무시)
    public void SetMoving(bool moving)
    {
        if (state == St.Dead) return;
        St next = moving ? St.Run : St.Idle;
        if (next != state) { state = next; timer = 0f; frame = 0; }
    }

    // 좌우 방향(true=east/오른쪽, false=west/왼쪽)
    public void SetFacing(bool facingRight) { right = facingRight; }

    // 사망 애니메이션(1회 재생 후 마지막 프레임 유지)
    public void Die()
    {
        if (state == St.Dead) return;
        state = St.Dead; timer = 0f; frame = 0;
    }

    void Update()
    {
        Sprite[] frames = CurrentFrames();
        if (frames == null || frames.Length == 0) return;

        float fps = state == St.Dead ? deadFps : (state == St.Run ? runFps : idleFps);
        timer += Time.deltaTime * fps;

        if (state == St.Dead) frame = Mathf.Min(frames.Length - 1, (int)timer); // 끝에서 정지
        else frame = ((int)timer) % frames.Length;                              // 루프

        sr.sprite = frames[frame];
    }

    void Apply()
    {
        var f = CurrentFrames();
        if (f != null && f.Length > 0 && sr != null) sr.sprite = f[0];
    }

    Sprite[] CurrentFrames()
    {
        switch (state)
        {
            case St.Run: return right ? runEast : runWest;
            case St.Dead: return dead;
            default: return right ? idleEast : idleWest;
        }
    }
}
