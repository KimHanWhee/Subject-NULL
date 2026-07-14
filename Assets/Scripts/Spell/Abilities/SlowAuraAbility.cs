using UnityEngine;

// ♦ Slow Aura / 감속 장막 (SelfBuff) — 지속시간 동안 플레이어 주변 범위의 적 이동 속도를 대폭 감소.
[CreateAssetMenu(fileName = "SlowAura", menuName = "Spell/Abilities/SlowAura")]
public class SlowAuraAbility : SpellAbility
{
    [Header("Effect")]
    public float duration = 10f;
    public float radius = 4.5f;
    [Range(0f, 1f)] public float slowFactor = 0.1f; // 남는 속도 비율(0.1 = 90% 감소)

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.5f, 0.7f, 1f, 0.9f); // 감속장 파랑
    public float outlineWidth = 0.06f;                       // 필드 테두리 두께(월드 유닛)

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        SlowAuraStatus.Apply(ctx.caster, radius, slowFactor, duration);

        Transform anchor = SpellVfx.VisualAnchor(ctx.caster); // 플레이어 추종
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, anchor.position, Quaternion.identity, anchor);
            Object.Destroy(fx, duration);
        }
        else
        {
            // 반경과 무관하게 두께 일정한 얇은 원 테두리(큰 필드도 안 두꺼움)
            SpellVfx.SpawnFieldOutline(anchor, radius, vfxColor, duration, outlineWidth);
        }
    }
}
