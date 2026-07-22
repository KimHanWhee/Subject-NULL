using UnityEngine;

// ♣ Time Stop (Legend, SelfBuff 드롭) — 3초간 모든 적과 적 총알을 완전 정지.
[CreateAssetMenu(fileName = "TimeStop", menuName = "Spell/Abilities/TimeStop")]
public class TimeStopAbility : SpellAbility
{
    public float duration = 3f;

    [Header("Wave (정지 파동)")]
    public float waveDuration = 0.7f;   // 파동이 다 퍼지는 시간(초, unscaled)
    public float waveRadius = 14f;      // 파동 최대 반경(화면 전체 커버)
    public Material grayscaleMaterial;  // 파장에 맞은 개체 회색화(SubjectNull/SpriteGrayscale)

    [Header("Stored Volley (정지 중 사격)")]
    [Tooltip("정지 중 발사한 총알이 해제 순간 주는 피해 — 맞은 적이 소멸하도록 크게 잡는다")]
    public float lethalDamage = 999f;

    [Header("Code VFX")]
    public Color vfxColor = new Color(0.8f, 0.95f, 1f, 1f); // 시간 정지 청백

    public override void Activate(SpellContext ctx)
    {
        Vector2 center = ctx.caster != null ? (Vector2)ctx.caster.transform.position : ctx.targetPosition;
        SpellParticleVfx.SpawnImplode(center, 8f, vfxColor, 0f, 48, 0.7f); // 시간이 빨려드는 청백 입자
        // 파동이 퍼지며 닿은 개체를 일그러뜨리고 회색+정지 → 완료 시 원복(정지 유지) + 유지 필드 생성
        TimeStopWave.Spawn(center, waveRadius, waveDuration, duration, grayscaleMaterial, vfxColor, lethalDamage);
    }
}
