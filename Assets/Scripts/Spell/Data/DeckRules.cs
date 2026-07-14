using UnityEngine;

// 덱 편성 규칙 — 등급별 동일 마블 최대 중복 수. Resources/DeckRules.asset에서 인스펙터로 조절.
// 애셋이 없으면 기본값(6/3/2/1)으로 폴백하므로 씬 배선 불필요.
[CreateAssetMenu(fileName = "DeckRules", menuName = "Spell/DeckRules")]
public class DeckRules : ScriptableObject
{
    [Header("등급별 덱 내 최대 중복 수")]
    public int normalMax = 6;
    public int goldMax = 3;
    public int diamondMax = 2;
    public int legendMax = 1;

    public int MaxCopies(Grade grade)
    {
        switch (grade)
        {
            case Grade.Normal:  return Mathf.Max(1, normalMax);
            case Grade.Gold:    return Mathf.Max(1, goldMax);
            case Grade.Diamond: return Mathf.Max(1, diamondMax);
            case Grade.Legend:  return Mathf.Max(1, legendMax);
            default:            return 1;
        }
    }

    private static DeckRules instance;
    public static DeckRules Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<DeckRules>("DeckRules");
            if (instance == null) instance = CreateInstance<DeckRules>(); // 애셋 없으면 기본값 폴백
            return instance;
        }
    }
}
