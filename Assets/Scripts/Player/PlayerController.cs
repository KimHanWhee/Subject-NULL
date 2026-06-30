using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    
    public float speed = 8;
    public WeaponData currentWeapon;
    public BulletPoolManager bulletPoolManager; // currentBulletPool 대신
    public Material flashMaterial;
    public Material defaultMaterial;

    public AudioClip hitSound;
    public AudioClip deadSound;
    
    Vector3 move;
    private SpriteRenderer sr;
    private Animator anim;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        GetComponent<Character>().Initialize();
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        move = Vector3.zero;
        
        if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
        {
            move += new Vector3(-1, 0, 0);
            // transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
        } 
        if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
        {
            move += new Vector3(1, 0, 0);
            // transform.Translate(new Vector3(-speed * Time.deltaTime, 0, 0));
        } 
        if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed)
        {
            move += new Vector3(0, 1, 0);
            // transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
        } 
        if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed)
        {
            move += new Vector3(0, -1, 0);
            // transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
        } 
        
        move = move.normalized;
        
        if (move.x < 0)
        {
            sr.flipX = true;
        }

        if (move.x > 0)
        {
            sr.flipX = false;
        }

        if (move.magnitude > 0)
        {
            anim.SetTrigger("Move");
        }
        else
        {
            anim.SetTrigger("Stop");
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }

    }

    void Shoot()
    {
        GetComponent<AudioSource>().PlayOneShot(currentWeapon.shotSound);
    
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        worldPosition.z = 0;
        worldPosition -= (transform.position + new Vector3(0, -0.5f, 0));

        ObjectPool pool = bulletPoolManager.GetPool(currentWeapon.bulletPrefab);
        GameObject newBullet = pool.Get();
        if (newBullet != null)
        {
            Bullet bulletScript = newBullet.GetComponent<Bullet>();
            newBullet.transform.position = transform.position + new Vector3(0, -0.5f);
            bulletScript.Direction = worldPosition;
            bulletScript.damage = currentWeapon.damage;
            bulletScript.speed = currentWeapon.bulletSpeed;
        }
    }

    private void FixedUpdate()
    {
        transform.Translate(move * (speed * Time.fixedDeltaTime));
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy")
        {
            
            if (GetComponent<Character>().Hit(1))
            {
                GetComponent<AudioSource>().PlayOneShot(hitSound);
                Flash();
            }
            else
            {
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
        GetComponent<AudioSource>().PlayOneShot(deadSound);
        anim.SetTrigger("Die");
        Invoke("AfterDying", 0.875f);
    }

    void AfterDying()
    {
        SceneManager.LoadScene("GameOverScene");
    }
}