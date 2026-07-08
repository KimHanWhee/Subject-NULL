using Unity.VisualScripting;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15;
    public float damage = 1;
    Vector2 direction;

    public Vector2 Direction
    {
        get { return direction; }
        set { direction = value.normalized; }
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(direction * (speed * Time.deltaTime));
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Wall" || collision.tag == "Enemy")
        {
            // 스펠 마블 ♥ Lifesteal — 적 명중 통지(데미지 적용 자체는 적 쪽 ApplyHit이 처리)
            if (collision.tag == "Enemy")
                LifestealStatus.NotifyBulletHit(damage);
            gameObject.SetActive(false);
        }
    }
}
