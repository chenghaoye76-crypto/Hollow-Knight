using UnityEngine;

public class LoadingIcon : MonoBehaviour
{
    Animator animator;
    private void Awake()
    {
        animator=GetComponent<Animator>();
    }
    public void Show()
    {
        animator.SetBool("isShow", true);
    }
    public void UnShow()
    {
        animator.SetBool("isShow", false);
    }
}
