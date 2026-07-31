using System.Collections.Generic;
using UnityEngine;

// ♠ Void Slash 상태 — 지속시간 동안 대시(Space)가 "베어내는 순간이동"으로 바뀐다.
//
// 대시를 대체하는 방식이라, 발동 횟수는 스태미너와 대시 쿨다운이 알아서 제한한다.
// (별도 횟수 제한을 두지 않는 이유 — 두 개의 제약이 겹치면 체감이 예측 불가능해진다)
public class VoidSlashStatus : MonoBehaviour, IPlayerDashOverride, IBuffDisplay
{
    private float remain;
    private SpellMarble marble;

    private float distance, travelTime, invulnDuration, damage, slashWidth, wallClearance;
    private Color tint;

    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return true; } }
    public float BuffRemaining { get { return remain; } }
    public int BuffCharges { get { return 0; } }

    public static void Apply(GameObject player, float duration, float distance, float travelTime,
                             float invulnDuration, float damage, float slashWidth, float wallClearance,
                             Color tint, SpellMarble marble = null)
    {
        VoidSlashStatus s = player.GetComponent<VoidSlashStatus>();
        if (s == null) s = player.AddComponent<VoidSlashStatus>();
        s.remain = Mathf.Max(s.remain, duration);
        s.distance = distance;
        s.travelTime = travelTime;
        s.invulnDuration = invulnDuration;
        s.damage = damage;
        s.slashWidth = slashWidth;
        s.wallClearance = wallClearance;
        s.tint = tint;
        s.marble = marble;
    }

    // 대시 입력을 가로채 순간이동 참격으로 바꾼다.
    public bool TryOverrideDash(Vector2 origin, Vector2 direction)
    {
        if (remain <= 0f) return false;
        if (direction.sqrMagnitude < 0.0001f) return false;

        Vector2 dir = direction.normalized;
        Vector2 to = ClampDestination(origin, origin + dir * distance);

        // ① 판정 먼저 — 시작 시점에 선분을 한 번에 훑는다.
        //    이동하며 매 프레임 검사하면 속도가 빨라 적을 뛰어넘는다(터널링).
        SlashAlongPath(origin, to);

        // ② 이동과 연출
        VoidSlashRunner.Run(gameObject, origin, to, travelTime, invulnDuration, tint);

        SpellVfx.SpawnRing(origin, 0.8f, tint, 0.35f);
        SpellParticleVfx.SpawnBurst(origin, 0.9f, tint, 16, 0.3f);
        SpellParticleVfx.SpawnImplode(to, 1.0f, tint, 0f, 20, 0.3f);
        DrawSlashTrail(origin, to, tint);
        return true;
    }

    // 경로를 감싸는 캡슐 판정. 같은 적을 두 번 때리지 않게 한 번만 모아 처리한다.
    void SlashAlongPath(Vector2 from, Vector2 to)
    {
        Vector2 mid = (from + to) * 0.5f;
        float len = Vector2.Distance(from, to);
        float angle = Mathf.Atan2((to - from).y, (to - from).x) * Mathf.Rad2Deg;

        Collider2D[] hits = Physics2D.OverlapCapsuleAll(
            mid, new Vector2(len + slashWidth * 2f, slashWidth * 2f), CapsuleDirection2D.Horizontal, angle);

        HashSet<int> done = new HashSet<int>();
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null || !hits[i].CompareTag("Enemy")) continue;
            GameObject e = hits[i].gameObject;
            if (!done.Add(e.GetInstanceID())) continue;

            // 벤 자국을 먼저 띄운다 — 적이 즉사해 사라져도 연출은 남아야 한다
            //  (ApplyHit 뒤에 그리면 처치된 적의 위치를 못 읽는 경우가 생긴다)
            SpawnCutMark(e.transform.position);
            SpellParticleVfx.SpawnBurst(e.transform.position, 0.5f, tint, 10, 0.25f);

            IDamageable d = e.GetComponent<IDamageable>();
            if (d != null) d.ApplyHit(damage);
            else { Character ch = e.GetComponent<Character>(); if (ch != null && !ch.Hit(damage)) e.SetActive(false); }
        }
    }

    // 적 하나하나에 남는 참격 자국 — 방향은 매번 무작위라 여럿을 벨 때 같은 그림이 반복되지 않는다.
    // 살짝 휜 호(弧)로 그려야 직선보다 "칼로 그은 것"처럼 읽힌다.
    void SpawnCutMark(Vector2 center)
    {
        GameObject go = new GameObject("VoidCutMark");
        go.transform.position = center;

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.useWorldSpace = false;   // 로컬 좌표 — 회전만으로 방향을 준다
        lr.sortingOrder = 32100;    // 경로 검광(32000)보다 위
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) lr.sortingLayerID = layers[layers.Length - 1].id;

        const int segs = 10;
        const float half = 0.62f;   // 자국 길이의 절반
        const float bow = 0.16f;    // 휘어짐
        lr.positionCount = segs + 1;
        for (int i = 0; i <= segs; i++)
        {
            float t = i / (float)segs;
            float x = Mathf.Lerp(-half, half, t);
            float y = Mathf.Sin(t * Mathf.PI) * bow;   // 가운데가 볼록한 호
            lr.SetPosition(i, new Vector3(x, y, 0f));
        }

        // 양 끝이 뾰족하게 — 칼끝이 스치고 빠지는 인상
        AnimationCurve w = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));
        lr.widthCurve = w;
        lr.widthMultiplier = 0.26f;

        lr.startColor = Color.white;
        lr.endColor = Color.white;

        go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        go.AddComponent<VoidCutMarkFade>().Init(lr, tint, 0.22f);
    }

    // 경로에 남는 검광 — 어디를 베었는지 읽히게
    static void DrawSlashTrail(Vector2 a, Vector2 b, Color c)
    {
        GameObject go = new GameObject("VoidSlashTrail");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 1f, 1f, 0.95f);
        lr.endColor = new Color(c.r, c.g, c.b, 0f);
        lr.startWidth = 0.5f;
        lr.endWidth = 0.05f;
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(a.x, a.y, 0f));
        lr.SetPosition(1, new Vector3(b.x, b.y, 0f));
        lr.sortingOrder = 32000;
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) lr.sortingLayerID = layers[layers.Length - 1].id;
        Object.Destroy(go, 0.18f);
    }

    // 목적지 보정 — Teleport와 같은 블링크 규칙(맵 밖으로 나가지 않게)
    Vector2 ClampDestination(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float dist = delta.magnitude;
        if (dist < 0.01f) return from;
        Vector2 dir = delta / dist;

        RaycastHit2D[] hits = Physics2D.LinecastAll(from, to);
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D h = hits[i];
            if (h.collider == null || h.collider.isTrigger || !h.collider.CompareTag("Wall")) continue;
            dist = Mathf.Min(dist, Mathf.Max(0f, h.distance - wallClearance));
        }

        Vector2 result = from + dir * dist;
        for (int guard = 0; guard < 8 && IsInsideWall(result); guard++)
        {
            dist = Mathf.Max(0f, dist - 0.3f);
            result = from + dir * dist;
        }
        return IsInsideWall(result) ? from : result;
    }

    bool IsInsideWall(Vector2 p)
    {
        Collider2D[] cs = Physics2D.OverlapCircleAll(p, wallClearance * 0.85f);
        for (int i = 0; i < cs.Length; i++)
            if (!cs[i].isTrigger && cs[i].CompareTag("Wall")) return true;
        return false;
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) Destroy(this);
    }
}
