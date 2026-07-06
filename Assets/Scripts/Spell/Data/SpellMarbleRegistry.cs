using System.Collections.Generic;
using UnityEngine;

// Design Ref: §3.2 — marbleName ↔ SpellMarble 매핑.
// JSON 저장은 문자열 id만 저장하고, 로드 시 여기서 실제 SO로 해석(에셋 GUID 의존 회피).
// Plan SC: FR-04 — 저장 참조 해석
[CreateAssetMenu(fileName = "SpellMarbleRegistry", menuName = "Spell/Registry")]
public class SpellMarbleRegistry : ScriptableObject
{
    public List<SpellMarble> allMarbles = new List<SpellMarble>();

    private Dictionary<string, SpellMarble> lookup;

    // 미존재 id는 null 반환 → 호출부에서 스킵(폴백). Design §6 에러 처리.
    public SpellMarble Get(string marbleName)
    {
        if (lookup == null) Build();
        return lookup.TryGetValue(marbleName, out var m) ? m : null;
    }

    void Build()
    {
        lookup = new Dictionary<string, SpellMarble>();
        foreach (var m in allMarbles)
            if (m != null && !string.IsNullOrEmpty(m.marbleName))
                lookup[m.marbleName] = m;
    }
}
