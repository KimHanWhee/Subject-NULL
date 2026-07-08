using UnityEngine;

// 슈트 → 표시용(기호/역할/색). 툴팁·HUD 공용. GradePalette(등급)와 대칭.
// Design Ref: §3.1 — ♠공격 ♥회복 ♣유틸 ♦방어
public static class SuitInfo
{
    public static string Symbol(Suit s)
    {
        switch (s)
        {
            case Suit.Spade:   return "♠";
            case Suit.Heart:   return "♥";
            case Suit.Club:    return "♣";
            case Suit.Diamond: return "♦";
            default:           return "?";
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

    // 리치텍스트 라벨: "<color=#..>♥</color> 회복"
    public static string RichLabel(Suit s)
    {
        string hex = ColorUtility.ToHtmlStringRGB(ColorOf(s));
        return "<color=#" + hex + ">" + Symbol(s) + "</color> " + Role(s);
    }
}
