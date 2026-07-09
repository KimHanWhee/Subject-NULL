using UnityEngine;

// ♣ Teleport (Gold, Targeted) — 드롭한 위치로 플레이어가 순간이동한다.
// (구 Swap에서 변경: 랜덤 적과 교체 → 지정 위치 텔레포트)
// 맵 밖 방지: 경로상 "Wall" 콜라이더에 막히면 벽 앞까지만 이동(블링크 규칙).
[CreateAssetMenu(fileName = "Teleport", menuName = "Spell/Abilities/Teleport")]
public class TeleportAbility : SpellAbility
{
    [Header("Code VFX")]
    public Color vfxColor = new Color(0.75f, 0.4f, 1f, 1f); // 공간 보라

    [Header("Blocking")]
    [Tooltip("벽 앞 정지 간격(플레이어 반지름 ≈0.49 + 여유)")]
    public float wallClearance = 0.55f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;

        Vector3 from = ctx.caster.transform.position;
        Vector2 clamped = ClampDestination(from, ctx.targetPosition);
        Vector3 to = new Vector3(clamped.x, clamped.y, from.z);

        ctx.caster.transform.position = to;
        Physics2D.SyncTransforms(); // 순간이동 직후 물리 위치 동기화(충돌/피격 판정 정확)

        SpellVfx.SpawnRing(from, 0.9f, vfxColor, 0.5f);    // 출발지 잔광
        SpellVfx.SpawnConverge(to, 1.1f, vfxColor, 0.35f); // 도착지 수렴 이펙트
        SpellParticleVfx.SpawnBurst(from, 0.9f, vfxColor, 18, 0.3f);       // 출발지 공간 파열 입자
        SpellParticleVfx.SpawnImplode(to, 1.1f, vfxColor, 0f, 22, 0.35f);  // 도착지로 모여드는 입자
    }

    // 목적지 보정: 경로 첫 벽 앞에서 정지 → 그래도 벽과 겹치면 원점 방향 후퇴 → 실패 시 제자리
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
}
