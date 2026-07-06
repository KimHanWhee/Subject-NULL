using UnityEngine;

// Design Ref: §3.1 — 능력 발동에 필요한 컨텍스트. 능력 구현체가 참조.
// Plan SC: FR-10 — SpellContext(caster/위치/등급/이펙트풀) 전달
public struct SpellContext
{
    public GameObject caster;        // 시전자(플레이어)
    public Vector2 targetPosition;   // Targeted 드롭 위치(SelfBuff면 caster 위치)
    public Grade grade;              // 위력 스케일 힌트(구현체가 자유롭게 반영)
    public ObjectPool effectPool;    // effectPrefab용 풀(없으면 null)
}
