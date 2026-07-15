using UnityEngine;

// ♥ Second Wind (SelfBuff) — 일정 시간 동안 대시가 스태미너를 소모하지 않는다(무한 질주).
// 쿨타임은 그대로 유지되므로 "무제한 연속 대시"가 아니라 "스태미너 걱정 없는 질주".
[CreateAssetMenu(fileName = "SecondWind", menuName = "Spell/Abilities/SecondWind")]
public class SecondWindAbility : SpellAbility
{
    [Header("Effect")]
    public float duration = 10f;                 // 스태미너 무소모 지속시간(초)
    public float dashCooldownWhileActive = 0.2f; // 지속시간 동안 적용할 대시 쿨타임(초)

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.4f, 0.9f, 1f, 1f); // 청록빛(질주/에너지)
    public float vfxRadius = 0.8f;
    public float vfxDuration = 0.9f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        PlayerController pc = ctx.caster.GetComponent<PlayerController>();
        if (pc != null) pc.GrantStaminaFree(duration, dashCooldownWhileActive);
        SecondWindStatus.Apply(ctx.caster, duration, ctx.marble); // 버프 HUD 표시용 미러

        Transform anchor = SpellVfx.VisualAnchor(ctx.caster); // 몸통 시각 중심(스프라이트 상단 여백 보정)
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, anchor.position, Quaternion.identity, anchor);
            Object.Destroy(fx, vfxDuration);
        }
        else
        {
            SpellVfx.SpawnAura(anchor, vfxRadius, vfxColor, vfxDuration);
            SpellParticleVfx.SpawnRise(anchor, vfxColor, 1f); // 청록 입자 상승(질주 에너지)
        }
    }
}
