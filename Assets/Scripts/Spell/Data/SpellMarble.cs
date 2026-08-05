using UnityEngine;

// Design Ref: §3.1 — 마블 = 슈트 + 등급 + 능력 참조 + 표시용.
// 기존 WeaponData SO 패턴 준수(데이터 주도).
// Plan SC: FR-01 — SpellMarble 데이터 모델
[CreateAssetMenu(fileName = "NewMarble", menuName = "Spell/SpellMarble")]
public class SpellMarble : ScriptableObject
{
    public string marbleName;   // 저장 시 참조 키(SpellMarbleRegistry)
    public Suit suit;
    public Grade grade;         // 능력에 따라 부여
    public SpellAbility ability; // 다형성 진입점
    public Sprite icon;         // 스킬 고유 아이콘(호버 툴팁용). 벨트 구슬은 MarbleSkinTable(슈트×등급)에서 결정.

    [Tooltip("패시브 오브 — 손패로 뽑히지 않고 패시브 칸에만 편성된다. 일반 덱에는 넣을 수 없다.")]
    public bool isPassive;      // 편성 칸이 갈린다: 일반↔패시브 교차 편성 불가

    [Header("패시브 발동 방식 (isPassive일 때만)")]
    public PassiveMode passiveMode = PassiveMode.Always;

    [Tooltip("Periodic — 발동 간격(초). 지속시간은 능력 에셋의 duration을 따른다.")]
    public float passiveInterval = 5f;

    [Tooltip("OnKillCount — 발동에 필요한 처치 수. 발동 후 다시 0부터 센다.")]
    public int passiveKillCount = 100;
}

// 패시브가 "언제" 발동하는지. "무엇을" 하는지는 능력(SpellAbility)이 이미 안다.
public enum PassiveMode
{
    Always,      // 상시 — 짧은 주기로 재적용(상태들이 시간 갱신이라 멱등)
    Periodic,    // N초마다 발동, 지속은 능력 duration
    OnKillCount, // 적 N마리 처치할 때마다 발동
    Once,        // 게임 시작 시 1회 (부활처럼 소모될 때까지 남는 것)
}
