using UnityEngine;

public class Cursor : MonoBehaviour
{
    private Animator animator;
    private bool normal = true;
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.SetBool("normal", normal);
    }

    public void SetBtnState(bool isnormal)
    {
        normal = isnormal;
    }

}
