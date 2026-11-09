using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class unshow : Poolbb
{
    public float time;
    public float speed;
    public Vector2 direction;
    public bool unshowColor;
    public bool bigger;
    public float biggerMutiplier;

    SpriteRenderer spriteRenderer;

    private Vector3 size;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        size = transform.localScale;
    }

    private void OnEnable()
    {
        transform.localScale = size;
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 1);
        StartCoroutine(bb());
    }

    IEnumerator bb()
    {
        float t = 0;
        while (t < time)
        {
            yield return null;
            t += Time.deltaTime;
            if (unshowColor) spriteRenderer.color -= new Color(0, 0, 0, (Time.deltaTime / time));
            if (bigger) transform.localScale += new Vector3(1, 1, 0) * Time.deltaTime * biggerMutiplier;
            transform.position += (new Vector3(direction.x, direction.y, 0)) * speed * Time.deltaTime / time;
        }
        yield return new WaitForSeconds(0.5f);
        base.End();
    }
}
