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
    public Sprite icon;
}
