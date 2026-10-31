using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{


    private StateMechine<PlayerController> _stateMachine;
    public StateMechine<PlayerController> stateMechine
    {
        get
        {
            if (_stateMachine == null)
            {
                _stateMachine = new StateMechine<PlayerController>();
                _stateMachine.AddState(StateType.idel, new PlayerIdelState(this));
            }
            return _stateMachine;
        }
    }
    [HideInInspector] public Animator anim;
    [HideInInspector] public Rigidbody2D rb;




    private PlayerAction playerAction;
    private PlayerEffectManager effectManager;
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

    [Header("Attack details")]
    private int cycloneSlashCounter;
    public float bigSlashFallSpeed = 0.9f;


    [Header("Check state")]
    [HideInInspector] public float preInputTime = 0.2f;
    [HideInInspector] public bool attackPress;
    public bool isAttacking;
    public bool isGround;


    [Header("Collision details")]
    [SerializeField] private float groundCheckDistace;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Timer")]
    [SerializeField] private float highJumpTime = 0.2f;//最长跳跃时间
    [SerializeField] private float lowJumpTime = 0.08f;//最短跳跃时间
    [HideInInspector] public float gravityscale = 4.84f;
    private float dropTime = 1.1f;
    private float dropTimer;

    private bool jumpReleased;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        effectManager = GetComponentInChildren<PlayerEffectManager>();
        playerAction = new PlayerAction();
        playerAction.Player.Enable();
        AddBigNail();
    }
    private void Update()
    {
        HandleAnimations();
        HandleCollison();
        CheckHardLand();
        HandleFlip();
        JumpUpdate();
        stateMechine.Update();
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
        playerAction.Player.Attack.started += AttackStart;
        playerAction.Player.Attack.canceled += AttackCancle;

        gravityscale = rb.gravityScale;
    }
    private void OnDisable()
    {
        playerAction.Player.Jump.performed -= Jump;
        playerAction.Player.Move.performed -= Move;
        playerAction.Player.Move.canceled -= Move;
        playerAction.Player.Move.canceled -= Move;
        playerAction.Player.Attack.started -= AttackStart;
        playerAction.Player.Attack.canceled -= AttackCancle;
    }

    #region InputEvent
    public void AttackStart(InputAction.CallbackContext context)
    {
        attackPress = true;
        ToAttack();
    }
    public void AttackCancle(InputAction.CallbackContext context)
    {
        attackPress = false;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && isGround)
        {
            JumpStart();
        }
        if (context.canceled)
        {
            JumpCancle();
        }
    }














    #endregion

    #region Motion
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

    private void JumpStart()
    {
        StartCoroutine(HighJumpTime());
        StartCoroutine(LowJumpTime());
        anim.SetTrigger("Jump");
        jumpReleased = false;
        isJumpHold = true;
    }
    private void JumpCancle()
    {
        jumpReleased = true;

        if (isJumpCancle && isJumpRise)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocityX, 0);
            isJumpHold = false;
            jumpReleased = false;
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

    #region Attack
    private void ToAttack()
    {
        StopCoroutine(attackPressJudge());
        StartCoroutine(attackPressJudge());
    }

    private IEnumerator attackPressJudge()
    {
        yield return new WaitForFixedUpdate();
        float t = preInputTime;
        bool isLong = true;
        while (t > 0)
        {
            if (isAttacking)
            {
                t -= Time.fixedDeltaTime;
                yield return null;
            }
            else
                break;
        }
        if (moveInput.y > 0.2f)
            {
                //preAttackisNormal = false;
                anim.SetTrigger("upAttack");
            }
            else if (moveInput.y < -0.2f && !isGround)
            {
                //preAttackisNormal = false;
                anim.SetTrigger("downAttack");
            }
            else
            {
                //preAttackisNormal = true;
                anim.SetTrigger("firstAttack");
            }
        while (t > 0)
        {
            yield return null;
            t -= Time.deltaTime;
            if (!attackPress)
            {
                isLong = false;
                break;
            }
        }
        
            
        if (isLong) 
        {
            isAttacking = true;
            stateMechine.ChangeState(StateType.bigSlash);
        }
    }


    public void AttackOver()
    {
        isAttacking = false;
        Debug.Log(isAttacking);
    }









    #endregion

    #region TURN
    private void HandleFlip()
    {
        if (moveInput.x > 0.1f && facingRight == false)
        {
            transform.localScale = new Vector3(-Math.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            Flip();
        }
        else if (moveInput.x < -0.1f && facingRight == true)
        {
            transform.localScale = new Vector3(Math.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            Flip();
        }
    }
    
    private void Flip()
    {
        facingRight = !facingRight;
        if (isGround)
            anim.SetTrigger("isTurning");
    }








    #endregion

    #endregion

    #region AttackEffect

    public void Attack1()
    {
        effectManager.FisrtAttack();
    }

    public void Attack2()
    {
        effectManager.SecondAttack();
    }

    public void UpAttack()
    {
        effectManager.UpAttack();


    }

    public void DownAttack()
    {
        effectManager.DownAttack();
    }

    public void BigAttack()
    {
        effectManager.BigAttack();
    }



    public void CycloneAttackOneRound()
    {
        cycloneSlashCounter--;
    }



    public void CycloneAttack()
    {
        effectManager.CycloneEffect();
    }


    public void DashAttack()
    {
        effectManager.DashAttack();
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
    #region AddSkll
    public void SkillInit()
    {
        AddBigNail();
    }
    public void AddBigNail()
    {
        stateMechine.AddState(StateType.bigSlash, new PlayerNABigSlashState(this));
    }












    #endregion
    public void SetGravityScale(float gravityValue)
    {
        rb.gravityScale = gravityValue;
    }




}
