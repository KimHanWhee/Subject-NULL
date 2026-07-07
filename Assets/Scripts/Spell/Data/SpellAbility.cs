using UnityEngine;

// Design Ref: §3.1 — 발동 대상 방식. 아무데나 드롭=자신 버프 / 위치 지정 발동.
public enum TargetMode
{
    SelfBuff,   // 아무 곳에 드롭해도 플레이어에게 적용(버프)
    Targeted    // 드롭한 위치에 발동
}

// Design Ref: §3.1/§4.3 — 개별 능력 추상 SO. 확장 지점.
// 구체 능력은 이 클래스를 상속한 에셋으로 추가 → SpellCaster 수정 불필요(스위치 없음).
// Plan SC: FR-02 — 능력 추상화 + 타겟 모드
public abstract class SpellAbility : ScriptableObject
{
    public string abilityName;
    [TextArea] public string description;
    public Sprite icon;               // 능력 고유 아이콘(툴팁/상세용). 벨트 구슬은 SpellMarble.icon
    public TargetMode targetMode = TargetMode.Targeted;
    public GameObject effectPrefab;   // 풀링될 이펙트/투사체(선택)

    // 등급별 위력은 마블 고유 속성이므로 ctx.grade를 참조해 구현체가 자유롭게 반영
    public abstract void Activate(SpellContext ctx);
}
