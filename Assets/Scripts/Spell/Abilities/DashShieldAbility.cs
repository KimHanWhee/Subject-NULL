using UnityEngine;

// ♦ Dash Shield (Normal, SelfBuff) — 10초간 대시 무적 시간 2배.
[CreateAssetMenu(fileName = "DashShield", menuName = "Spell/Abilities/DashShield")]
public class DashShieldAbility : SpellAbility
{
    public float duration = 10f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.4f, 0.8f, 1f, 1f); // 대시 하늘색
    public float vfxRadius = 0.7f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        DashShieldStatus.Apply(ctx.caster, duration);
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, ctx.caster.transform.position, Quaternion.identity, ctx.caster.transform);
            Object.Destroy(fx, duration);
        }
        else SpellVfx.SpawnAura(ctx.caster.transform, vfxRadius, vfxColor, 0.8f); // 짧은 확인 오라
    }
}
