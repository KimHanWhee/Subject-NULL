using UnityEngine;

// ♠ Void Slash (Gold, SelfBuff) — 지속시간 동안 대시(Space)가 "베어내는 순간이동"으로 바뀐다.
// 대시 방향으로 순식간에 파고들며 경로의 적을 베고, 그동안 무적이며 적과 충돌하지 않는다.
//
// 버프형인 이유: 발동 횟수를 스태미너와 대시 쿨다운이 알아서 제한한다.
// 별도 횟수 제한을 겹쳐 두면 언제 쓸 수 있는지 예측하기 어려워진다.
[CreateAssetMenu(fileName = "VoidSlash", menuName = "Spell/Abilities/VoidSlash")]
public class VoidSlashAbility : SpellAbility
{
    [Header("지속")]
    [Tooltip("이 시간 동안 대시가 순간이동 참격으로 바뀐다")]
    public float duration = 10f;

    [Header("돌진")]
    [Tooltip("한 번에 이동할 거리(일반 대시는 약 4.8)")]
    public float distance = 6f;
    [Tooltip("이동에 걸리는 시간. 짧을수록 순간이동에 가깝다.")]
    public float travelTime = 0.12f;
    // 이동시간(0.12초)만 덮으면 안 된다. 근접한 적을 베고 나면 그 적 옆에 착지하게 되는데,
    // 무적이 바로 풀리면 접촉 피해를 그대로 맞아 이득보다 손해가 크다.
    // 빠져나갈 여유까지 포함해 1초.
    [Tooltip("무적 지속. 착지 후 적 틈에서 빠져나올 시간까지 포함한다.")]
    public float invulnDuration = 1f;

    [Header("피해")]
    public float damage = 40f;
    [Tooltip("경로 좌우 판정 폭(반지름)")]
    public float slashWidth = 0.9f;

    [Header("벽")]
    [Tooltip("벽 앞 정지 간격(플레이어 반지름 ≈0.49 + 여유)")]
    public float wallClearance = 0.55f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.85f, 0.95f, 1f, 1f); // 검광(백청)
    public float vfxRadius = 0.8f;
    public float vfxDuration = 0.9f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;

        VoidSlashStatus.Apply(ctx.caster, duration, distance, travelTime, invulnDuration,
                                damage, slashWidth, wallClearance, vfxColor, ctx.marble);

        Transform anchor = SpellVfx.VisualAnchor(ctx.caster); // 몸통 시각 중심(스프라이트 상단 여백 보정)
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, anchor.position, Quaternion.identity, anchor);
            Object.Destroy(fx, vfxDuration);
        }
        else
        {
            SpellVfx.SpawnAura(anchor, vfxRadius, vfxColor, vfxDuration);
            SpellParticleVfx.SpawnOrbit(anchor, vfxRadius, vfxColor, duration); // 지속 중 검광 궤도
        }
    }
}
