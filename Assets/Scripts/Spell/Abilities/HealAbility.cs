using UnityEngine;

// ♥ 회복(SelfBuff) — 플레이어 HP를 회복. 등급이 높을수록 회복량 증가.
// 공개 Character.Heal API만 사용(플레이어 컨트롤러 수정 불필요).
[CreateAssetMenu(fileName = "Heal", menuName = "Spell/Abilities/Heal")]
public class HealAbility : SpellAbility
{
    public float baseHeal = 1f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.35f, 1f, 0.45f, 1f); // 초록(회복)
    public float vfxRadius = 0.8f;
    public float vfxDuration = 0.6f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;

        float amount = baseHeal * GradeMultiplier(ctx.grade); // 회복량만 등급 스케일(이 능력의 선택)
        Character ch = ctx.caster.GetComponent<Character>();
        if (ch != null) ch.Heal(amount);

        // 프리팹 있으면 그쪽, 없으면 코드 VFX(플레이어 추종 초록 오라)
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, ctx.caster.transform.position, Quaternion.identity, ctx.caster.transform);
            Object.Destroy(fx, vfxDuration);
        }
        else
        {
            SpellVfx.SpawnAura(ctx.caster.transform, vfxRadius, vfxColor, vfxDuration);
            SpellParticleVfx.SpawnRise(ctx.caster.transform, vfxColor, 1f); // 초록 입자 상승(회복감)
        }
    }

    static float GradeMultiplier(Grade g)
    {
        switch (g)
        {
            case Grade.Gold:    return 1.5f;
            case Grade.Diamond: return 2f;
            case Grade.Legend:  return 3f;
            default:            return 1f;
        }
    }
}
