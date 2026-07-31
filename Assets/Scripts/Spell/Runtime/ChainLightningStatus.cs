using System.Collections.Generic;
using UnityEngine;

// ♠ Chain Lightning 상태 — 지속시간 동안 총알이 적에게 명중하면 주변 적으로 번개가 연쇄.
// PlayerBulletEvents.EnemyHit 구독. 연쇄: 맞은 적 → 반경 내 미방문 적 → ... 최대 jumps회.
public class ChainLightningStatus : MonoBehaviour, IBuffDisplay
{
    private float chainDamage;
    private float jumpRadius;
    private int maxJumps;
    private float remain;
    private bool subscribed;
    private SpellMarble marble;

    // 버프 HUD 표시 — 다른 지속형 스펠과 동일 규약(남은 시간 게이지)
    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return true; } }
    public float BuffRemaining { get { return remain; } }
    public int BuffCharges { get { return 0; } }

    public static void Apply(GameObject player, float chainDamage, float jumpRadius, int maxJumps,
                             float duration, SpellMarble marble = null)
    {
        ChainLightningStatus s = player.GetComponent<ChainLightningStatus>();
        if (s == null) s = player.AddComponent<ChainLightningStatus>();
        s.chainDamage = chainDamage;
        s.jumpRadius = jumpRadius;
        s.maxJumps = maxJumps;
        s.marble = marble;
        s.remain = Mathf.Max(s.remain, duration);
        s.Subscribe();
    }

    void Subscribe()
    {
        if (subscribed) return;
        PlayerBulletEvents.EnemyHit += OnEnemyHit;
        subscribed = true;
    }

    void Unsubscribe()
    {
        if (!subscribed) return;
        PlayerBulletEvents.EnemyHit -= OnEnemyHit;
        subscribed = false;
    }

    void OnEnemyHit(GameObject firstEnemy, float bulletDamage)
    {
        var visited = new HashSet<int> { firstEnemy.GetInstanceID() };
        GameObject current = firstEnemy;

        for (int j = 0; j < maxJumps; j++)
        {
            GameObject next = FindNearestUnvisited(current.transform.position, visited);
            if (next == null) break;
            visited.Add(next.GetInstanceID());

            DrawBolt(current.transform.position, next.transform.position);
            SpellParticleVfx.SpawnBurst(next.transform.position, 0.4f, new Color(0.6f, 0.85f, 1f, 1f), 8, 0.22f); // 감전 스파크
            DealDamage(next, chainDamage);
            current = next;
        }
    }

    GameObject FindNearestUnvisited(Vector2 from, HashSet<int> visited)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(from, jumpRadius);
        GameObject best = null;
        float bestSq = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy")) continue;
            if (visited.Contains(hits[i].gameObject.GetInstanceID())) continue;
            float sq = ((Vector2)hits[i].transform.position - from).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = hits[i].gameObject; }
        }
        return best;
    }

    static void DealDamage(GameObject enemy, float damage)
    {
        IDamageable dmg = enemy.GetComponent<IDamageable>();
        if (dmg != null) { dmg.ApplyHit(damage); return; }
        Character ch = enemy.GetComponent<Character>();
        if (ch != null && !ch.Hit(damage)) enemy.SetActive(false);
    }

    // 두 점 사이 지그재그 번개 선(LineRenderer) — 짧게 나타났다 사라짐
    static void DrawBolt(Vector2 a, Vector2 b)
    {
        GameObject go = new GameObject("ChainBolt");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(0.6f, 0.85f, 1f, 1f);
        lr.endColor = new Color(0.9f, 0.98f, 1f, 1f);
        lr.startWidth = 0.07f;
        lr.endWidth = 0.05f;
        lr.sortingOrder = 32000;
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) lr.sortingLayerID = layers[layers.Length - 1].id;

        const int segs = 6;
        lr.positionCount = segs + 1;
        Vector2 dir = b - a;
        Vector2 normal = new Vector2(-dir.y, dir.x).normalized;
        for (int i = 0; i <= segs; i++)
        {
            float t = i / (float)segs;
            Vector2 p = Vector2.Lerp(a, b, t);
            if (i > 0 && i < segs) p += normal * Random.Range(-0.18f, 0.18f); // 지그재그
            lr.SetPosition(i, new Vector3(p.x, p.y, 0f));
        }
        Object.Destroy(go, 0.15f);
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) Destroy(this);
    }

    void OnDisable() { Unsubscribe(); }
    void OnDestroy() { Unsubscribe(); }
}
