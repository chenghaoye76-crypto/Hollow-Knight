using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{   private Animator anim;
    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private PlayerAction playerAction;
    private Vector2 moveInput;
    private bool facingRight = true;
    [SerializeField] private float moveSpeed = 8.3f;
    [SerializeField] private float jumpForce = 8f;
    
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

        HandleFlip();
    }


    private void HandleAnimations()
    {
        bool isMoving = rb.linearVelocity.x != 0;
        anim.SetBool("isMoving", isMoving);
    }

    private void OnEnable()
    {
        playerAction.Player.Jump.performed += Jump;
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
    

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }


    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed)
            rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpForce);
    }

    private void HandleFlip()
    {
        if (rb.linearVelocity.x > 0 && facingRight == false)
            Flip();
        else if (rb.linearVelocity.x < 0 && facingRight == true) 
            Flip();
        
    }
    
    private void Flip()
    {
        transform.Rotate(0, 180, 0);
        facingRight = !facingRight;
        anim.SetTrigger("isTurning");
    }
}
