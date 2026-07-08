using UnityEngine;

// ♥ Lifesteal (Normal, SelfBuff) — 10초간 적에게 총알이 명중할 때마다 체력 1 회복.
[CreateAssetMenu(fileName = "Lifesteal", menuName = "Spell/Abilities/Lifesteal")]
public class LifestealAbility : SpellAbility
{
    public float healPerHit = 1f;
    public float duration = 10f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.9f, 0.2f, 0.4f, 1f); // 진홍(흡혈)
    public float vfxRadius = 0.75f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        LifestealStatus.Apply(ctx.caster, healPerHit, duration);
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, ctx.caster.transform.position, Quaternion.identity, ctx.caster.transform);
            Object.Destroy(fx, duration);
        }
        else SpellVfx.SpawnAura(ctx.caster.transform, vfxRadius, vfxColor, duration);
    }
}
