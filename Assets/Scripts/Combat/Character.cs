using UnityEngine;

public class Character : MonoBehaviour
{
    public float maxHp = 3;
    public HeartPoint heartPoint;

    private float hp;

    private float hpMaxWidth;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {   
    }

    public void Initialize()
    {
        hp = maxHp;
        if (heartPoint != null)
        {
            heartPoint.UpdateHeart(hp, maxHp);
        }
    }
    /**
     * 살아있으면 true 리턴
     */
    public bool Hit(float damage)
    {
        hp -= damage;
        if (hp <= 0) hp = 0;

        if (heartPoint != null)
        {
            heartPoint.UpdateHeart(hp, maxHp);
        }

        return hp > 0;
    }
}
