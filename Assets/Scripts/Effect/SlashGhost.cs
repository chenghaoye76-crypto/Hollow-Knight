using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlashGhost : MonoBehaviour
{
    private bool isMoving;
    private float speed;
    private Vector3 dir;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        isMoving = false;
    }

    public void Motion(float speed, float rotation)
    {
        isMoving = true;
        this.speed = speed;

        transform.rotation = Quaternion.Euler(new Vector3(0, 0, rotation));
        float radians = rotation * Mathf.Deg2Rad;
        dir = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0);

        animator.SetTrigger("motion");
    }

    private void Update()
    {
        if (isMoving)
            transform.position = dir * speed * Time.deltaTime + transform.position;
    }
}
