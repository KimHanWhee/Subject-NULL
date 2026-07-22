using UnityEngine;

// 스펠 능력의 표시 문자열을 현재 언어로 돌려준다.
//
// 왜 SO에 영어 필드를 추가하지 않는가:
//  - SpellAbility.id는 애초에 로컬라이제이션 훅으로 만들어진 안정 키다(에셋 주석 참고).
//  - 에셋 33개를 건드리지 않으므로 guid/참조가 깨질 위험이 없고,
//    언어를 하나 더 늘려도 Loc 테이블에 열만 추가하면 된다.
//  - 번역문이 한 파일에 모여 있어 누락을 찾기 쉽다.
//
// 이름(abilityName)은 이미 영문 고유명("Black Hole" 등)이라 언어와 무관하게 그대로 쓴다.
public static class SpellText
{
    public static string Name(SpellAbility a)
    {
        if (a == null) return "";
        string key = "spell." + a.id + ".n";
        return Loc.Has(key) ? Loc.T(key) : a.abilityName;
    }

    public static string Desc(SpellAbility a)
    {
        if (a == null) return "";
        string key = "spell." + a.id + ".d";
        // 키가 없으면 에셋에 적힌 원문(한국어)으로 폴백 — 번역 누락이어도 설명이 비지 않는다.
        return Loc.Has(key) ? Loc.T(key) : a.description;
    }

    // 마블 기준 헬퍼(능력이 비어 있으면 저장 키로 폴백)
    public static string Name(SpellMarble m)
    {
        if (m == null) return "";
        return m.ability != null ? Name(m.ability) : m.marbleName;
    }

    public static string Desc(SpellMarble m)
    {
        return m != null && m.ability != null ? Desc(m.ability) : "";
    }
}
