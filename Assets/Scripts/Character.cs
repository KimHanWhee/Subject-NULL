using UnityEngine;

public class Character : MonoBehaviour
{
    public float maxHp = 3;
    public GameObject hpGauge;

    private float hp;

    private float hpMaxWidth;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {   
    }

    public void Initialize()
    {
        hp = maxHp;
        if(hpGauge != null) {
            hpMaxWidth = hpGauge.GetComponent<RectTransform>().sizeDelta.x;
        }
    }
    /**
     * 살아있으면 true 리턴
     */
    public bool Hit(float damage)
    {
        hp -= damage;
        if (hp <= 0)
        {
            hp = 0;
        }

        if (hpGauge != null)
        {
            hpGauge.GetComponent<RectTransform>().sizeDelta = new Vector2(hp / maxHp * hpMaxWidth,
                hpGauge.GetComponent<RectTransform>().sizeDelta.y);
        }

        return hp > 0;
    }
}
