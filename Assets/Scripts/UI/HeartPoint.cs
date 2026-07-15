using UnityEngine;
using UnityEngine.UI;

// HP 바 — 스프라이트 교체(정수 단위)가 아니라 코드 fill로 연속 표현(0.5 데미지 등 소수 반영).
// 배경 = empty 프레임 스프라이트(프레임+십자가+빈 트랙), 그 위에 빨간 fill을 HP 비율만큼 덮는다.
public class HeartPoint : MonoBehaviour
{
    [Tooltip("0번 = 풀피, 마지막 인덱스 = 빈피 (마지막 스프라이트를 배경 프레임으로 사용)")]
    public Sprite[] heartSprites;

    // 스프라이트 내 빨간 바(트랙) 영역 — 정규화 측정값
    const float TrackXMin = 0.238f, TrackXMax = 0.912f, TrackYMin = 0.304f, TrackYMax = 0.696f;

    private Image image;
    private Image fill;
    private static Sprite whiteSprite;

    void Awake()
    {
        image = GetComponent<Image>();
        BuildFill();
    }

    static Sprite White()
    {
        if (whiteSprite != null) return whiteSprite;
        var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        t.SetPixel(0, 0, Color.white); t.Apply();
        whiteSprite = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        return whiteSprite;
    }

    void BuildFill()
    {
        // 배경 = 빈 프레임(프레임+십자가+빈 트랙) 고정
        if (image != null && heartSprites != null && heartSprites.Length > 0)
        {
            image.sprite = heartSprites[heartSprites.Length - 1];
            image.type = Image.Type.Simple;
        }
        // 빨간 fill(코드 생성) — 트랙 영역에 앵커, 폭만 ratio로 조절
        var go = new GameObject("HpFill", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        fill = go.GetComponent<Image>();
        fill.sprite = White();
        fill.color = new Color(177f / 255f, 62f / 255f, 83f / 255f, 1f);
        fill.raycastTarget = false;
        var rt = fill.rectTransform;
        rt.anchorMin = new Vector2(TrackXMin, TrackYMin);
        rt.anchorMax = new Vector2(TrackXMax, TrackYMax);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.transform.SetAsLastSibling(); // 배경 위로
    }

    public void UpdateHeart(float hp, float maxHp)
    {
        float ratio = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
        if (fill == null) return;
        var rt = fill.rectTransform;
        rt.anchorMin = new Vector2(TrackXMin, TrackYMin);
        rt.anchorMax = new Vector2(TrackXMin + (TrackXMax - TrackXMin) * ratio, TrackYMax);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        fill.enabled = ratio > 0f;
    }
}
