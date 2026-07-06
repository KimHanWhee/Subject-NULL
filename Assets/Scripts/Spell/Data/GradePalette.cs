using UnityEngine;

// Design Ref: §3.3 — 등급별 색상 매핑. HUD/선택창/툴팁 공용.
// Plan SC: FR-03 — 등급 시각 표현(무색/금색/푸른색/무지개)
public static class GradePalette
{
    public static Color ColorOf(Grade grade)
    {
        switch (grade)
        {
            case Grade.Normal:  return new Color(0.85f, 0.85f, 0.85f); // 무색(회백)
            case Grade.Gold:    return new Color(1f, 0.84f, 0f);       // 금색
            case Grade.Diamond: return new Color(0.35f, 0.65f, 1f);    // 푸른색
            case Grade.Legend:  return new Color(0.8f, 0.4f, 1f);      // 레전드 대표색(무지개 셰이더는 후속)
            default:            return Color.white;
        }
    }
}
