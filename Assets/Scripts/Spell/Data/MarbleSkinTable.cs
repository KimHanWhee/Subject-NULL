using System.Collections.Generic;
using UnityEngine;

// Design Ref: §3.1 — 벨트에 뜨는 구슬 스프라이트를 (슈트 × 등급)으로 결정하는 룩업 테이블.
// 그림이 카테고리의 순수 함수일 때의 정석: 단일 진실 출처 + 신규 마블 연결 0.
// SpellMarble.icon 이 비어있을 때 이 테이블에서 자동 매칭(개별 icon 지정 시 그쪽이 우선).
[CreateAssetMenu(fileName = "MarbleSkinTable", menuName = "Spell/MarbleSkinTable")]
public class MarbleSkinTable : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public Suit suit;
        public Grade grade;
        public Sprite sprite;
    }

    [Tooltip("슈트×등급 조합별 구슬 스프라이트. 최대 4×4=16개")]
    public Entry[] entries;

    [Tooltip("조합이 표에 없을 때 대신 쓸 기본 스프라이트(선택)")]
    public Sprite fallback;

    private Dictionary<int, Sprite> map;

    // 슈트/등급을 하나의 int 키로 (IL2CPP 안전)
    private static int KeyOf(Suit suit, Grade grade) => (int)suit * 100 + (int)grade;

    public Sprite Get(Suit suit, Grade grade)
    {
        if (map == null) Build();
        return (map.TryGetValue(KeyOf(suit, grade), out Sprite s) && s != null) ? s : fallback;
    }

    private void Build()
    {
        map = new Dictionary<int, Sprite>();
        if (entries == null) return;
        foreach (Entry e in entries)
            map[KeyOf(e.suit, e.grade)] = e.sprite;
    }

    // 에디터에서 표를 수정하면 다음 Get 때 재빌드
    private void OnValidate() => map = null;
}
