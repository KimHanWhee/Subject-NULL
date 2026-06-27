using System;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    enum State
    {
        Spawning,
        Moving,
        Dying
    }
    public float speed = 2;
    
    public Material flashMaterial;
    public Material defaultMaterial;

    GameObject target;
    State state;
    
    private SpriteRenderer sr;
    private Animator anim;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    public void Spawn(GameObject target)
    {
        this.target = target;
        Debug.Log("Spawn called, target: " + target);
        state = State.Spawning;
        GetComponent<Character>().Initialize();
        GetComponent<Animator>().SetTrigger("Spawn");
        Invoke("StartMoving", 1);
        GetComponent<Collider2D>().enabled = false;
    }

    void StartMoving()
    {
        GetComponent<Collider2D>().enabled = true;
        state = State.Moving;
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if (state == State.Moving)
        {
            Vector2 direction = target.transform.position - transform.position;
            transform.Translate(direction.normalized * (speed * Time.fixedDeltaTime));

            if (direction.x < 0)
            {
                sr.flipX = true;
            }

            if (direction.x > 0)
            {
                sr.flipX = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Bullet")
        {
            float d = collision.gameObject.GetComponent<Bullet>().damage;

            if (GetComponent<Character>().Hit(d))
            {
                // 살아있을 때
                Flash();
            }
            else
            {
                // 죽었을 떄
                Die();
            }
        }
    }

    void Flash()
    {
        sr.material = flashMaterial;
        Invoke("AfterFlash", 0.5f);
    }

    void AfterFlash()
    {
        sr.material = defaultMaterial;
    }

    void Die()
    {
        state = State.Dying;
        anim.SetTrigger("Die");
        Invoke("AfterDying", 0.6f);
    }
    
    void AfterDying()
    {
        gameObject.SetActive(false);
    }
}
