using Unity.VisualScripting;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15;
    public float damage = 1;
    [System.NonSerialized] public bool pierce; // 스펠 마블 ♠ Piercing — true면 적 명중에도 소멸 안 함(발사 시마다 재설정)
    [System.NonSerialized] public bool frozen; // 스펠 마블 ♣ Time Stop — 정지 중 발사분은 제자리 대기(발사 시마다 재설정)
    Vector2 direction;
    SpriteRenderer sr;

    // 시간 정지 대기 상태 토글 — 대기 중인 탄은 청백으로 물들여 "쌓여 있다"는 걸 보여준다
    public void SetFrozen(bool on)
    {
        frozen = on;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = on ? new Color(0.7f, 0.95f, 1f) : Color.white;
    }

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
        if (frozen) return; // ♣ Time Stop — 정지가 풀릴 때까지 제자리
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
