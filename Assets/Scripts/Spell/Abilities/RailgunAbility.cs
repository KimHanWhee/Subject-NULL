using UnityEngine;

// ♠ Railgun (Diamond, SelfBuff) — 다음 3번의 공격이 관통 레일건으로 대체.
// 경로상 모든 적에게 강한 피해 + 넉백, 플레이어도 반동으로 미세 넉백.
[CreateAssetMenu(fileName = "Railgun", menuName = "Spell/Abilities/Railgun")]
public class RailgunAbility : SpellAbility
{
    public int charges = 3;
    public float damage = 12f;
    public float beamLength = 14f;
    [Tooltip("빔 판정 폭(넓게 — 관통 레일)")]
    public float beamWidth = 1.4f;
    public float knockbackDistance = 2f;
    [Tooltip("발사 반동으로 플레이어가 밀리는 거리")]
    public float playerRecoil = 0.35f;
    [Tooltip("클릭 후 기를 모으는 시간(초) — 끝나는 순간 커서 방향으로 발사")]
    public float chargeTime = 0.5f;

    [Header("Code VFX")]
    public Color vfxColor = new Color(0.5f, 0.85f, 1f, 1f); // 전자기 청백

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        RailgunStatus.Apply(ctx.caster, charges, damage, beamLength, beamWidth,
                            knockbackDistance, playerRecoil, chargeTime, ctx.marble);
        Transform anchor = SpellVfx.VisualAnchor(ctx.caster);
        SpellVfx.SpawnAura(anchor, 0.7f, vfxColor, 0.7f);
        SpellParticleVfx.SpawnImplode(anchor.position, 1.6f, vfxColor, 0f, 20, 0.4f); // 충전 연출
    }
}
