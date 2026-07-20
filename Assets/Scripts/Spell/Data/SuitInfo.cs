using System.Collections.Generic;
using UnityEngine;

// 슈트 → 표시용(기호/역할/색). 툴팁·HUD 공용. GradePalette(등급)와 대칭.
// Design Ref: §3.1 — ♠공격 ♥회복 ♣유틸 ♦방어
public static class SuitInfo
{
    // 슈트 문양은 "폰트 글리프가 아니라 스프라이트"로 표시한다.
    // 이유: 본문 폰트(Pretendard)에 ♠♣♦ 글리프가 없고, 폴백 폰트 방식은 빌드에서
    //       스트립/정적 아틀라스 문제로 실패했다(하트만 보이는 증상). 스프라이트는 폰트와 무관.
    // 흰색 실루엣이므로 Image.color = ColorOf(suit) 로 틴트해서 쓴다.
    static readonly Dictionary<Suit, Sprite> iconCache = new Dictionary<Suit, Sprite>();

    public static Sprite Icon(Suit s)
    {
        Sprite cached;
        if (iconCache.TryGetValue(s, out cached) && cached != null) return cached;
        Sprite sp = Resources.Load<Sprite>("UI/Suits/" + IconName(s));
        iconCache[s] = sp;
        return sp;
    }

    static string IconName(Suit s)
    {
        switch (s)
        {
            case Suit.Spade:   return "SuitSpade";
            case Suit.Heart:   return "SuitHeart";
            case Suit.Club:    return "SuitClub";
            default:           return "SuitDiamond";
        }
    }

    public static string Role(Suit s)
    {
        switch (s)
        {
            case Suit.Spade:   return "공격";
            case Suit.Heart:   return "회복";
            case Suit.Club:    return "유틸";
            case Suit.Diamond: return "방어";
            default:           return "";
        }
    }

    // 카드 관례: 하트/다이아 = 빨강, 스페이드/클럽 = 밝은 회색(검은 툴팁 배경 대비)
    public static Color ColorOf(Suit s)
    {
        switch (s)
        {
            case Suit.Heart:
            case Suit.Diamond: return new Color(1f, 0.42f, 0.42f);
            default:           return new Color(0.9f, 0.9f, 0.9f);
        }
    }

    // 리치텍스트 라벨: "<color=#..>회복</color>"
    // Legacy Text는 스프라이트 삽입을 지원하지 않으므로 문양 없이 색상+역할어로만 표기한다.
    // (툴팁에는 이미 스킬 아이콘이 따로 붙어 있어 정보 손실 없음)
    public static string RichLabel(Suit s)
    {
        string hex = ColorUtility.ToHtmlStringRGB(ColorOf(s));
        return "<color=#" + hex + ">" + Role(s) + "</color>";
    }
}
