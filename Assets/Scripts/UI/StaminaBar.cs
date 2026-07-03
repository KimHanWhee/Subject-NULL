using UnityEngine;
using UnityEngine.UI;

// 대시 v2 UI: 플레이어 스태미너를 채워지는 Image(fillAmount)로 표시
public class StaminaBar : MonoBehaviour
{
    [Tooltip("스태미너를 읽어올 플레이어. 비우면 'Player' 태그로 자동 탐색")]
    public PlayerController player;

    [Tooltip("Image Type=Filled 로 설정된 게이지 이미지")]
    public Image fillImage;

    [Header("색상 (선택)")]
    public bool useColorFeedback = true;
    public Color readyColor = new Color(0.30f, 0.75f, 1f);   // 대시 가능(가득)
    public Color lowColor = new Color(1f, 0.55f, 0.15f);     // 부족

    void Awake()
    {
        // fillImage 미지정 시 자기 자신에서 탐색
        if (fillImage == null)
            fillImage = GetComponent<Image>();

        // player 미지정 시 태그로 자동 탐색
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.GetComponent<PlayerController>();
        }
    }

    void Update()
    {
        if (player == null || fillImage == null) return;

        float ratio = player.StaminaRatio;
        fillImage.fillAmount = ratio;

        if (useColorFeedback)
            fillImage.color = Color.Lerp(lowColor, readyColor, ratio);
    }
}
