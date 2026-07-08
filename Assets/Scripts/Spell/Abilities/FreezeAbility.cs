using UnityEngine;

// ♣ Freeze (Gold, Targeted) — 범위 내 적을 잠시 완전히 정지시킨다.
[CreateAssetMenu(fileName = "Freeze", menuName = "Spell/Abilities/Freeze")]
public class FreezeAbility : SpellAbility
{
    public float radius = 2f;
    public float duration = 2f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.55f, 0.85f, 1f, 1f); // 얼음빛
    public float vfxLifetime = 0.6f;

    public override void Activate(SpellContext ctx)
    {
        Vector2 pos = ctx.targetPosition;

        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, pos, Quaternion.identity);
            Object.Destroy(fx, vfxLifetime);
        }
        else
        {
            SpellVfx.SpawnRing(pos, radius, vfxColor, vfxLifetime);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy")) continue;
            FreezeStatus.Apply(hits[i].gameObject, duration);
        }
    }
}
