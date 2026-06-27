using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    
    public float speed = 8;
    public GameObject bulletPrefab;
    
    Vector3 move;
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
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        worldPosition.z = 0;
        worldPosition -= (transform.position + new Vector3(0, -0.5f, 0));

        GameObject newBullet = GetComponent<ObjectPool>().Get();
        if (newBullet != null)
        {
            newBullet.transform.position = transform.position + new Vector3(0, -0.5f);
            newBullet.GetComponent<Bullet>().Direction = worldPosition;
        }
    }

    private void FixedUpdate()
    {
        transform.Translate(move * (speed * Time.fixedDeltaTime));
    }
}
