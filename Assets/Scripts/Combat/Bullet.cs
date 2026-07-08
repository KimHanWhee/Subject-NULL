using Unity.VisualScripting;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15;
    public float damage = 1;
    [System.NonSerialized] public bool pierce; // 스펠 마블 ♠ Piercing — true면 적 명중에도 소멸 안 함(발사 시마다 재설정)
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
            if (collision.tag == "Enemy")
            {
                // 스펠 마블 명중 통지(Lifesteal 회복, Chain Lightning 연쇄 등). 데미지 적용은 적 쪽 ApplyHit.
                PlayerBulletEvents.NotifyEnemyHit(collision.gameObject, damage);
                if (pierce) return; // 스펠 마블 ♠ Piercing — 적은 관통(벽은 여전히 소멸)
            }
            gameObject.SetActive(false);
        }
    }
}
