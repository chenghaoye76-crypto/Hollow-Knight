using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Fog : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image image;
    [SerializeField] private Vector3 pos;
    [SerializeField] float speed;
    [SerializeField] Vector2 direction;
    [SerializeField] float duration;
    [SerializeField] float startTime;
    bool flag;
    private float alpha;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        pos = rectTransform.position;
        alpha = image.color.a;
        image.color = new Color(image.color.r, image.color.g, image.color.b, 0f);
        flag = false;
        StartCoroutine(Action());
    }
    private void Update()
    {
        if (flag)
            rectTransform.position = rectTransform.position + new Vector3(direction.x, direction.y, 0f) * speed * Time.deltaTime;
    }

    IEnumerator Action()
    {
        flag = false;
        yield return new WaitForSeconds(startTime);
        flag = true;
        //Show
        while (image.color.a < alpha)
        {
            yield return null;
            image.color = new Color(image.color.r, image.color.g, image.color.b, image.color.a + Time.deltaTime * alpha);
        }
        //move
        yield return new WaitForSeconds(duration);
        //UnShow
        while (image.color.a > 0f)
        {
            yield return null;
            image.color = new Color(image.color.r, image.color.g, image.color.b, image.color.a - Time.deltaTime * alpha);
        }
        flag = false;
        rectTransform.position = pos;
        StartCoroutine(Action());
    }
}
