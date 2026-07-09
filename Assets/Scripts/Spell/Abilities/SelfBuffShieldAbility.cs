using UnityEngine;

// Design Ref: §8.5 참조 능력(SelfBuff, ♦ 방어) — 프레임워크 검증용. 실제 콘텐츠는 후속 데이터로 교체.
// 발동 시 플레이어에게 잠깐 무적 실드 부여. 에셋에서 targetMode = SelfBuff 로 설정.
[CreateAssetMenu(fileName = "SelfBuffShield", menuName = "Spell/Abilities/SelfBuffShield")]
public class SelfBuffShieldAbility : SpellAbility
{
    public float baseDuration = 2f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.3f, 0.8f, 1f, 1f); // 하늘색(보호막)
    public float vfxRadius = 0.8f;
    public float vfxWidth = 0.1f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;

        float duration = baseDuration * GradeMultiplier(ctx.grade); // 등급별 지속 스케일(이 능력의 선택)
        PlayerController pc = ctx.caster.GetComponent<PlayerController>();
        if (pc != null) pc.GrantShield(duration);

        // 프리팹 있으면 그쪽, 없으면 코드 VFX(플레이어 추종 오라 — 실드 지속 동안)
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, ctx.caster.transform.position, Quaternion.identity, ctx.caster.transform);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(ctx.caster.transform, vfxRadius, vfxColor, duration, vfxWidth);
            SpellParticleVfx.SpawnOrbit(ctx.caster.transform, vfxRadius, vfxColor, duration); // 하늘색 궤도 입자(실드 지속 표시)
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
