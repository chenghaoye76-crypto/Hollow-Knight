using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{   private Animator anim;
    private Rigidbody2D rb;
    private PlayerAction playerAction;
    private bool facingRight;
    private Vector2 moveInput;
    [Header("Movemnet details")]
    [SerializeField] private float moveSpeed = 8.3f;
    [SerializeField] private float jumpForce = 15.7f;
    private bool isJumpHold;
    private bool isJumpCancle;
    private bool isJumpRise;
    private float MaxVelocityY = 20.9f;
    [SerializeField] private bool isHardLand;

    [Header("Collision details")]
    [SerializeField] private float groundCheckDistace;
    [SerializeField] private bool isGround;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Timer")]
    [SerializeField] private float highJumpTime = 0.2f;//最长跳跃时间
    [SerializeField] private float lowJumpTime = 0.08f;//最短跳跃时间
    private float dropTime = 1.1f;
    private float dropTimer;

    private bool jumpReleased;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerAction = new PlayerAction();
        playerAction.Player.Enable();
    }
    private void Update()
    {
        HandleAnimations();
        HandleCollison();
        CheckHardLand();
        HandleFlip();
        JumpUpdate();
    }

    private void FixedUpdate()
    {
        MoveFixedUpdate();
        LimitVelocityY();
    }


    private void OnEnable()
    {
        playerAction.Player.Jump.performed += Jump;
        playerAction.Player.Jump.canceled += Jump;
        playerAction.Player.Move.performed += Move;
        playerAction.Player.Move.canceled += Move;
    }
    private void OnDisable()
    {
        playerAction.Player.Jump.performed -= Jump;
        playerAction.Player.Move.performed -= Move;
        playerAction.Player.Move.canceled -= Move;
        playerAction.Player.Move.canceled -= Move;
    }



    #region MOVE
    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void MoveFixedUpdate()
    {
        if (moveInput != null)
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }




    #endregion

    #region JUMP
    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && isGround)
        {
            StartCoroutine(HighJumpTime());
            StartCoroutine(LowJumpTime());
            anim.SetTrigger("Jump");
            jumpReleased = false;
            isJumpHold = true;
        }
        if (context.canceled) 
        {
            jumpReleased = true;

            if (isJumpCancle && isJumpRise)  
            {
                rb.linearVelocity = new Vector2(rb.linearVelocityX, 0);
                isJumpHold = false;
                jumpReleased = false;
            }
        }
    }

    private void JumpUpdate()
    {
        isJumpRise = rb.linearVelocityY > 0f;
        if (isJumpHold)
            rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce);

        if (isJumpCancle && jumpReleased && isJumpRise)  
        {
            rb.linearVelocity = new Vector2(rb.linearVelocityX, 0);
            isJumpHold = false;
            jumpReleased = false;
        }
    }
    IEnumerator LowJumpTime()
    {
        isJumpCancle = false;

        yield return new WaitForSeconds(lowJumpTime);

        isJumpCancle = true;
    }
    IEnumerator HighJumpTime()
    {
        yield return new WaitForSeconds(highJumpTime);
        isJumpHold = false;
    }







    #endregion

    #region CHECK
    private void HandleAnimations()
    {
        bool isMoving = rb.linearVelocity.x != 0;
        anim.SetFloat("VelocityY", rb.linearVelocityY);
        anim.SetFloat("inputX", Math.Abs(moveInput.x));
        anim.SetBool("isGround", isGround);
        anim.SetBool("isMoving", isMoving);
        anim.SetBool("isHardLand", isHardLand);
    }

    private void HandleCollison()
    {
        isGround = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistace, whatIsGround);
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(0, -groundCheckDistace));
    }
    private void CheckHardLand()
    {
        if (!isGround && rb.linearVelocityY < 0)
        {
            dropTimer += Time.deltaTime;
        }
        else if(isGround)
        {
            dropTimer = 0f;
        }
        if (dropTimer > dropTime)
        {
            isHardLand = true;
        }
    }
    //通过动画事件来取消硬着陆
    private void AnimHardLand()
    {
        isHardLand = false;
        Debug.Log(isHardLand);
        dropTimer = 0f;
    }
    private void LimitVelocityY()
    {
        rb.linearVelocityY = Mathf.Clamp(rb.linearVelocityY, -MaxVelocityY, MaxVelocityY);
    }








    #endregion

    #region TURN
    private void HandleFlip()
    {
        if (moveInput.x > 0.1f && facingRight == false)
            Flip();
        else if (moveInput.x < -0.1f && facingRight == true)
            Flip();
    }
    
    private void Flip()
    {
        transform.Rotate(0, 180, 0);
        facingRight = !facingRight;
        anim.SetTrigger("isTurning");
    }








    #endregion


}
