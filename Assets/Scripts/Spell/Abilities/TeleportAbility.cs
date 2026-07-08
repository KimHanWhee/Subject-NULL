using UnityEngine;

// ♣ Teleport (Gold, Targeted) — 드롭한 위치로 플레이어가 순간이동한다.
// (구 Swap에서 변경: 랜덤 적과 교체 → 지정 위치 텔레포트)
[CreateAssetMenu(fileName = "Teleport", menuName = "Spell/Abilities/Teleport")]
public class TeleportAbility : SpellAbility
{
    [Header("Code VFX")]
    public Color vfxColor = new Color(0.75f, 0.4f, 1f, 1f); // 공간 보라

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;

        Vector3 from = ctx.caster.transform.position;
        Vector3 to = new Vector3(ctx.targetPosition.x, ctx.targetPosition.y, from.z);

        ctx.caster.transform.position = to;
        Physics2D.SyncTransforms(); // 순간이동 직후 물리 위치 동기화(충돌/피격 판정 정확)

        SpellVfx.SpawnRing(from, 0.9f, vfxColor, 0.5f);    // 출발지 잔광
        SpellVfx.SpawnConverge(to, 1.1f, vfxColor, 0.35f); // 도착지 수렴 이펙트
    }
}
