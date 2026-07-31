using System.Collections.Generic;
using UnityEngine;

// ♠ Void Slash 실행체 — 지정 방향으로 순식간에 돌진하며 경로의 적을 베어낸다.
//
// "순간이동처럼 보이되 경로 판정은 있어야 한다"는 요구를 이렇게 나눴다:
//   · 판정 — 시작 시점에 시작점~도착점 선분을 한 번에 훑어 맞은 적을 전부 확정한다.
//     프레임마다 검사하면 돌진이 빨라 적을 뛰어넘는 터널링이 생긴다.
//   · 연출 — 그 뒤 짧은 시간 동안 실제로 미끄러지며 잔상을 남긴다.
//     즉시 순간이동시키면 "어디로 갔는지" 읽히지 않아 방향감이 사라진다.
public class VoidSlashRunner : MonoBehaviour
{
    private Vector2 from, to;
    private float dur, elapsed;
    private PlayerController pc;
    private Rigidbody2D rb;
    private Collider2D selfCol;
    private readonly List<Collider2D> ignored = new List<Collider2D>();
    private SpriteRenderer body;
    private float nextGhost;
    private Color tint;

    public static void Run(GameObject player, Vector2 from, Vector2 to, float duration,
                           float invulnDuration, Color tint)
    {
        VoidSlashRunner r = player.GetComponent<VoidSlashRunner>();
        if (r == null) r = player.AddComponent<VoidSlashRunner>();
        r.Begin(player, from, to, duration, invulnDuration, tint);
    }

    void Begin(GameObject player, Vector2 a, Vector2 b, float duration, float invulnDuration, Color c)
    {
        from = a; to = b; dur = Mathf.Max(0.01f, duration); elapsed = 0f; tint = c;
        pc = player.GetComponent<PlayerController>();
        rb = player.GetComponent<Rigidbody2D>();
        selfCol = player.GetComponent<Collider2D>();
        body = player.GetComponent<SpriteRenderer>();

        // 무적 — 기존 실드 경로를 그대로 쓴다(피격 판정이 한 곳에서만 결정되도록)
        if (pc != null) pc.GrantShield(invulnDuration);

        IgnoreEnemyCollisions(true); // 충돌 없이 통과
    }

    // 돌진 중 적과 밀치지 않도록 콜라이더 쌍을 무시한다.
    // 레이어를 바꾸지 않는 이유: 이 프로젝트는 적/총알 판정을 태그로 하므로
    // 레이어를 건드리면 피격·소멸 규약이 어긋난다.
    void IgnoreEnemyCollisions(bool on)
    {
        if (selfCol == null) return;
        if (on)
        {
            GameObject[] es = GameObject.FindGameObjectsWithTag("Enemy");
            for (int i = 0; i < es.Length; i++)
            {
                Collider2D ec = es[i].GetComponent<Collider2D>();
                if (ec == null || ec.isTrigger) continue;
                Physics2D.IgnoreCollision(selfCol, ec, true);
                ignored.Add(ec);
            }
        }
        else
        {
            for (int i = 0; i < ignored.Count; i++)
                if (ignored[i] != null) Physics2D.IgnoreCollision(selfCol, ignored[i], false);
            ignored.Clear();
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / dur);
        // 가속 후 급감속 — 시작이 빨라야 "순간이동" 인상이 남는다
        float e = 1f - Mathf.Pow(1f - t, 3f);
        Vector2 p = Vector2.Lerp(from, to, e);

        if (rb != null) rb.position = p;
        transform.position = new Vector3(p.x, p.y, transform.position.z);

        // 잔상 — 경로를 눈으로 따라갈 수 있게
        if (body != null && Time.time >= nextGhost)
        {
            DashGhost.Spawn(body, tint);
            nextGhost = Time.time + 0.018f;
        }

        if (t >= 1f)
        {
            Physics2D.SyncTransforms(); // 도착 직후 물리 위치 동기화(피격 판정 정확)
            Destroy(this);
        }
    }

    // 컴포넌트가 어떤 이유로 사라지든 충돌 무시를 반드시 되돌린다.
    // 남아 있으면 그 적과는 영영 충돌하지 않는다.
    void OnDisable() { IgnoreEnemyCollisions(false); }
    void OnDestroy() { IgnoreEnemyCollisions(false); }
}
