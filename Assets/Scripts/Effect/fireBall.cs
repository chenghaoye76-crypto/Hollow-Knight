using System.Collections;
using UnityEngine;

public class fireBall : bb
{
    public Vector2 frontOffset;
    public LayerMask wallLayer;
    public LayerMask groundLayer;
    public float checkRaduis;
    public float speed;
    public float time;
    private Rigidbody2D rb;
    private Animator animator;
    [HideInInspector] public ParticleSystem point;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }
    private void Start()
    {
        StartCoroutine(dash());
    }
    IEnumerator dash()
    {
        if(point != null)
        {
            point.Play();
        }
        float t = time;
        while (t > 0 && !CheckFrontWall())
        {
            yield return null;
            t -= Time.deltaTime;
            rb.linearVelocity = new Vector2((transform.localScale.x > 0 ? 1 : -1) * speed, 0f);
            if (point != null)
                point.transform.position = transform.localPosition;
        }
        animator.SetTrigger("destroy2");
        if(point != null)
        {
            point.Stop();
        }

    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere((Vector2)transform.position + frontOffset, checkRaduis);
    }

    public bool CheckFrontWall()
    {
        return Physics2D.OverlapCircle((Vector2)transform.position + frontOffset, checkRaduis, wallLayer) || Physics2D.OverlapCircle((Vector2)transform.position + frontOffset, checkRaduis, groundLayer);
    }
}