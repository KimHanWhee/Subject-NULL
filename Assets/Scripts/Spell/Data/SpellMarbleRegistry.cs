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

    // 이름이 바뀐 마블의 옛 id → 새 id.
    // 저장된 소유 목록·덱에는 옛 id가 그대로 남아 있으므로, 여기서 흡수하지 않으면
    // 이미 가진 마블이 사라진 것처럼 보인다.
    private static readonly Dictionary<string, string> legacyIds = new Dictionary<string, string>
    {
        { "blink-strike", "void-slash" },   // 2026-07-31 Void Slash로 개명
    };

    // 미존재 id는 null 반환 → 호출부에서 스킵(폴백). Design §6 에러 처리.
    public SpellMarble Get(string marbleName)
    {
        if (lookup == null) Build();
        if (marbleName != null && legacyIds.TryGetValue(marbleName, out var renamed)) marbleName = renamed;
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
