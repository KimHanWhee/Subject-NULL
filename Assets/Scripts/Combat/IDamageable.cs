// Design Ref: §8.5 — 적 피격의 단일 진입점. 총알/스펠 등 데미지 소스가 공통으로 호출.
// 구현체(EnemyBase — 모든 적 컨트롤러의 공통 부모)가 Flash/Die 애니메이션까지 소유 → 사망 모션 보존.
public interface IDamageable
{
    // damage 적용. 내부에서 살아있으면 Flash, 죽으면 Die(사망 애니) 처리.
    void ApplyHit(float damage);
}
