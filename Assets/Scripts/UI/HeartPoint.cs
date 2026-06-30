using UnityEngine;
using UnityEngine.UI;

public class HeartPoint : MonoBehaviour
{
    [Tooltip("0번 = 풀피, 마지막 인덱스 = 빈피 순서로 등록")]
    public Sprite[] heartSprites;

    private Image image;

    void Awake()
    {
        image = GetComponent<Image>();
    }

    public void UpdateHeart(float hp, float maxHp)
    {
        float ratio = Mathf.Clamp01(hp / maxHp);

        // ratio 1(풀피) -> index 0, ratio 0(빈피) -> 마지막 index
        int index = Mathf.RoundToInt((1f - ratio) * (heartSprites.Length - 1));
        index = Mathf.Clamp(index, 0, heartSprites.Length - 1);

        image.sprite = heartSprites[index];
    }
}