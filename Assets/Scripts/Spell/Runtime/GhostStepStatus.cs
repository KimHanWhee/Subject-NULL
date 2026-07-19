using System.Collections.Generic;
using UnityEngine;

// ♦ Ghost Step 상태 — 지속시간 동안 이동속도 증가 + 모든 피해 무효 + 적 통과, 대신 기본 공격 불가.
// 유령화 연출: 본체 반투명 + 이동 시 잔상(DashGhost 재사용).
public class GhostStepStatus : MonoBehaviour, IPlayerDamageModifier, IBuffDisplay
{
    private PlayerController pc;
    private SpriteRenderer sr;
    private Collider2D myCol;
    private float originalSpeed;
    private Color originalColor;
    private float remain;
    private SpellMarble marble;

    // 유령화 동안 충돌을 꺼둔 적 콜라이더들 — 종료 시 원복(신규 스폰은 주기 스캔으로 편입)
    private readonly List<Collider2D> ignored = new List<Collider2D>();
    private float nextScan;

    private Vector3 lastGhostPos;
    private float nextGhost;
    private static readonly Color ghostTint = new Color(0.75f, 0.7f, 1f, 0.5f); // 창백한 보라

    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return true; } }
    public float BuffRemaining { get { return remain; } }
    public int BuffCharges { get { return 0; } }

    public static void Apply(GameObject player, float speedMult, float duration, SpellMarble marble = null)
    {
        GhostStepStatus s = player.GetComponent<GhostStepStatus>();
        if (s == null)
        {
            s = player.AddComponent<GhostStepStatus>();
            s.pc = player.GetComponent<PlayerController>();
            if (s.pc == null) { Destroy(s); return; }
            s.originalSpeed = s.pc.speed;
            s.pc.speed = s.originalSpeed * Mathf.Max(1f, speedMult);
            s.pc.attackLocked = true; // 무적의 대가 — 공격 불가
            s.sr = player.GetComponent<SpriteRenderer>();
            if (s.sr != null)
            {
                s.originalColor = s.sr.color;
                s.sr.color = new Color(1f, 1f, 1f, 0.55f); // 반투명 유령화
            }
            s.myCol = player.GetComponent<Collider2D>();
            s.ScanIgnoreEnemies(); // 유령화 — 현재 적들과의 물리 충돌 해제(통과)
            s.lastGhostPos = player.transform.position;
        }
        s.marble = marble;
        s.remain = Mathf.Max(s.remain, duration);
    }

    // 모든 적 콜라이더와의 충돌을 끈다 — 주기 호출로 버프 중 스폰된 적도 편입
    void ScanIgnoreEnemies()
    {
        if (myCol == null) return;
        foreach (EnemyBase e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
        {
            Collider2D ec = e.GetComponent<Collider2D>();
            if (ec == null || ignored.Contains(ec)) continue;
            Physics2D.IgnoreCollision(myCol, ec, true);
            ignored.Add(ec);
        }
    }

    public float ModifyIncomingDamage(float damage, GameObject attacker) => 0f; // 완전 무효

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) { Destroy(this); return; }

        // 신규 스폰 적도 통과 대상에 편입
        if (Time.time >= nextScan)
        {
            nextScan = Time.time + 0.2f;
            ScanIgnoreEnemies();
        }

        // 이동 잔상 — 정지 중엔 만들지 않음
        if (sr == null || Time.time < nextGhost) return;
        float moved = ((Vector2)transform.position - (Vector2)lastGhostPos).magnitude;
        if (moved < 0.1f) return;
        DashGhost.Spawn(sr, ghostTint, 0.5f);
        lastGhostPos = transform.position;
        nextGhost = Time.time + 0.05f;
    }

    void Restore()
    {
        if (pc != null)
        {
            pc.speed = originalSpeed;
            pc.attackLocked = false;
        }
        pc = null;
        if (sr != null) sr.color = originalColor;
        sr = null;

        // 적 충돌 원복 — 겹친 채 끝나면 물리 보정이 자연스럽게 밀어냄
        if (myCol != null)
            foreach (Collider2D ec in ignored)
                if (ec != null) Physics2D.IgnoreCollision(myCol, ec, false);
        ignored.Clear();
        myCol = null;
    }

    void OnDisable() { Restore(); }
    void OnDestroy() { Restore(); }
}
