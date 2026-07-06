using System.Collections.Generic;
using UnityEngine;

// Design Ref: §3.2 — 덱(15~25 마블 참조). 런타임 손패 소스 + 기본 덱.
// Plan SC: FR-04 — 덱 데이터 모델
[CreateAssetMenu(fileName = "NewDeck", menuName = "Spell/DeckData")]
public class DeckData : ScriptableObject
{
    public List<SpellMarble> marbles = new List<SpellMarble>();
}
