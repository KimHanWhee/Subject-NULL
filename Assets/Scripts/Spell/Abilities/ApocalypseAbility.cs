using UnityEngine;

// ♠ Apocalypse (Legend, SelfBuff 드롭) — 화면(씬) 내 모든 적에게 즉시 대량 피해.
[CreateAssetMenu(fileName = "Apocalypse", menuName = "Spell/Abilities/Apocalypse")]
public class ApocalypseAbility : SpellAbility
{
    public float damage = 10f;

    [Header("Code VFX")]
    public Color vfxColor = new Color(1f, 0.3f, 0.15f, 1f); // 종말의 적색

    public override void Activate(SpellContext ctx)
    {
        Vector2 center = ctx.caster != null ? (Vector2)ctx.caster.transform.position : ctx.targetPosition;

        // 전화면 충격파(대형 링 연발)
        SpellVfx.SpawnRing(center, 4f, vfxColor, 0.5f);
        SpellVfx.SpawnRing(center, 8f, vfxColor, 0.7f);
        SpellVfx.SpawnRing(center, 12f, new Color(1f, 0.8f, 0.4f, 1f), 0.9f);
        SpellParticleVfx.SpawnBurst(center, 6f, vfxColor, 60, 0.6f);                       // 근거리 화염 파편
        SpellParticleVfx.SpawnBurst(center, 11f, new Color(1f, 0.8f, 0.4f, 1f), 50, 0.9f); // 원거리 잔불 파편

        // 활성 적 전체 타격(태그 오염 회피 — 컨트롤러 기준 수집)
        foreach (EnemyController ec in Object.FindObjectsOfType<EnemyController>())
            ec.ApplyHit(damage);
        foreach (RangedEnemyController rc in Object.FindObjectsOfType<RangedEnemyController>())
        {
            IDamageable dmg = rc.GetComponent<IDamageable>();
            if (dmg != null) { dmg.ApplyHit(damage); continue; }
            Character ch = rc.GetComponent<Character>();
            if (ch != null && !ch.Hit(damage)) rc.gameObject.SetActive(false);
        }
    }
}
