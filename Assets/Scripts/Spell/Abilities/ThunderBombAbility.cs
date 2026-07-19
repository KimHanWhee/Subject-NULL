using UnityEngine;

// ♠ 비격진천뢰 (Gold, SelfBuff) — 10초간 기본 공격 시 커서 위치에 폭탄이 낙하해 소규모 폭발.
// effectPrefab에 수류탄 폭발(GrenadeExplosionVFX)을 연결하면 같은 연출을 재사용한다.
[CreateAssetMenu(fileName = "ThunderBomb", menuName = "Spell/Abilities/ThunderBomb")]
public class ThunderBombAbility : SpellAbility
{
    [Tooltip("폭발 반경 — 수류탄(2.8)의 절반 크기")]
    public float radius = 1.4f;
    public float damage = 2f;
    [Tooltip("폭탄 투하 최소 간격(초) — 연사 과열 방지")]
    public float minInterval = 0.25f;
    public float duration = 10f;

    [Header("Code VFX")]
    public Color vfxColor = new Color(1f, 0.55f, 0.15f, 1f); // 불씨 주황

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        ThunderBombStatus.Apply(ctx.caster, radius, damage, minInterval, duration, effectPrefab, ctx.marble);
        Transform anchor = SpellVfx.VisualAnchor(ctx.caster);
        SpellVfx.SpawnAura(anchor, 0.65f, vfxColor, 0.6f);
        SpellParticleVfx.SpawnRise(anchor, vfxColor, 0.8f);
    }
}
