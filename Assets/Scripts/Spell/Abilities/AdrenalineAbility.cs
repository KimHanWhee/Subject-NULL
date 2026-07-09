using UnityEngine;

// ♥ Adrenaline (Gold, SelfBuff) — 10초간 체력이 낮을수록 이동속도/공격속도 증가.
[CreateAssetMenu(fileName = "Adrenaline", menuName = "Spell/Abilities/Adrenaline")]
public class AdrenalineAbility : SpellAbility
{
    [Tooltip("빈사(HP 0%) 시 최대 보너스 배율(0.5 = +50%)")]
    public float maxBonus = 0.5f;
    public float duration = 10f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(1f, 0.55f, 0.15f, 1f); // 아드레날린 주황
    public float vfxRadius = 0.75f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        AdrenalineStatus.Apply(ctx.caster, maxBonus, duration);
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, ctx.caster.transform.position, Quaternion.identity, ctx.caster.transform);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(ctx.caster.transform, vfxRadius, vfxColor, duration);
            SpellParticleVfx.SpawnOrbit(ctx.caster.transform, vfxRadius, vfxColor, duration); // 주황 궤도 입자(버프 지속 표시)
        }
    }
}
