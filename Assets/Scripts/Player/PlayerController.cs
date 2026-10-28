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
    private bool jumpHold;
    private bool isJumpCancle;
    private bool isJumpRise;

    [Header("Collision details")]
    [SerializeField] private float groundCheckDistace;
    [SerializeField] private bool isGrounded;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Timer")]
    [SerializeField] private float highJumpTime = 0.2f;//最长跳跃时间
    [SerializeField] private float lowJumpTime = 0.08f;//最短跳跃时间
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
        isJumpRise = rb.linearVelocityY > 0f;
        HandleAnimations();
        HandleCollison();
        HandleFlip();
        JumpUpdate();
    }


    private void HandleAnimations()
    {
        bool isMoving = rb.linearVelocity.x != 0;
        anim.SetBool("isMoving", isMoving);
        anim.SetFloat("yVelocity", rb.linearVelocityY);
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
    }


    private void FixedUpdate()
    {
        if (moveInput != null)
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

    #region MOVE
    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }






    #endregion

    #region JUMP
    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            StartCoroutine(HighJumpTime());
            StartCoroutine(LowJumpTime());
            anim.SetTrigger("Jump");
            jumpReleased = false;
            jumpHold = true;
        }
        if (context.canceled) 
        {
            jumpReleased = true;

            if (isJumpCancle && isJumpRise)  
            {
                rb.linearVelocity = new Vector2(rb.linearVelocityX, 0);
                jumpHold = false;
                jumpReleased = false;
            }
        }
    }

    private void JumpUpdate()
    {
        if (jumpHold)
            rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce);

        if (isJumpCancle && jumpReleased && isJumpRise)  
        {
            rb.linearVelocity = new Vector2(rb.linearVelocityX, 0);
            jumpHold = false;
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
        jumpHold = false;
    }







    #endregion
    private void HandleCollison()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistace, whatIsGround);
    }

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

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(0, -groundCheckDistace));
    }
}
