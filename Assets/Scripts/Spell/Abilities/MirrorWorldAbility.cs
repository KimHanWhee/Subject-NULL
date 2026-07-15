using UnityEngine;

// ♦ Mirror World (Legend, SelfBuff) — 지속시간 동안 완전 무적 + 받았을 피해를 주변 모든 적에게 전량 반사.
[CreateAssetMenu(fileName = "MirrorWorld", menuName = "Spell/Abilities/MirrorWorld")]
public class MirrorWorldAbility : SpellAbility
{
    public float radius = 4f;   // 반사/범위 반경
    public float duration = 5f;

    [Header("Code VFX")]
    public Color vfxColor = new Color(0.8f, 0.5f, 1f, 1f); // 신비한 보라
    public float vfxRadius = 1f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        MirrorWorldStatus.Apply(ctx.caster, radius, duration, ctx.marble);
        Transform anchor = SpellVfx.VisualAnchor(ctx.caster); // 몸통 시각 중심(스프라이트 상단 여백 보정)

        // 범위 표시(무지갯빛 일렁임)는 항상 — effectPrefab 유무와 무관하게 지속시간 내내 보이게.
        SpellVfx.SpawnMirrorField(anchor, radius, duration);

        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, anchor.position, Quaternion.identity, anchor);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellParticleVfx.SpawnOrbit(anchor, vfxRadius, vfxColor, duration); // 플레이어 주변 보라 궤도 입자
        }
    }
}
