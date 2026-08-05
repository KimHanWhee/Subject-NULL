using UnityEngine;

// Design Ref: §3.1 — 능력 발동에 필요한 컨텍스트. 능력 구현체가 참조.
// Plan SC: FR-10 — SpellContext(caster/위치/등급/이펙트풀) 전달
public struct SpellContext
{
    public GameObject caster;        // 시전자(플레이어)
    public Vector2 targetPosition;   // Targeted 드롭 위치(SelfBuff면 caster 위치)
    public Grade grade;              // 위력 스케일 힌트(구현체가 자유롭게 반영)
    public ObjectPool effectPool;    // effectPrefab용 풀(없으면 null)
    public SpellMarble marble;       // 발동한 마블(버프 HUD 아이콘/툴팁 해석용, 선택)

    // 상시 패시브 — 만료 없이 계속 유지한다.
    // 주기적으로 재적용하는 대신 한 번만 걸고 지속시간을 무한으로 준다.
    // (Adrenaline처럼 체력에 따라 매 프레임 재계산하는 것도 있어서
    //  "능력치만 한 번 바꾸기"로는 안 되고, 상태 컴포넌트는 살아 있어야 한다)
    public bool permanent;

    // 상시면 무한, 아니면 에셋에 적힌 지속시간
    public float Duration(float assetDuration)
    {
        return permanent ? float.PositiveInfinity : assetDuration;
    }
}
