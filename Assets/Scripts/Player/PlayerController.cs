using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;

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
    [Header("Movemnet details")]
    [SerializeField] private float moveSpeed = 8.3f;
    [SerializeField] private float jumpForce = 15.7f;
    [SerializeField] private bool isHardLand;
    [HideInInspector] public Vector2 moveInput;
    private bool isJumpHold;
    private bool isJumpCancle;
    private bool isJumpRise;
    private float MaxVelocityY = 20.9f;

    [Header("Attack details")]
    public float NAchargeTime;
    public float bigSlashFallSpeed = 0.9f;
    public float cycloneSlashFallSpeed = 4.9f;
    public float cycloneSlashMoveSpeed;

    [Header("Dash details")]
    public float dashSpeed = 20f;//冲刺速度
    public float dashTime = 0.15f;//冲刺时间
    public float dashCD;
    public float shadowDashCD;
    public float superDashSpeed;






    [Header("Collision details")]
    [SerializeField] private float groundCheckDistace;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Check state")]
    [HideInInspector] public float preInputTime = 0.2f;
    [HideInInspector] public bool attackPress;
    [HideInInspector] public bool facingRight;
    [HideInInspector] public bool dashAttack = false;
    public bool canControl = true;
    public bool isAttacking;
    public bool isGround;
    public bool isDashing;



    [Header("Timer")]
    [SerializeField] private float highJumpTime = 0.2f;//最长跳跃时间
    [SerializeField] private float lowJumpTime = 0.08f;//最短跳跃时间
    [HideInInspector] public float gravityscale = 4.84f;
    [HideInInspector] public int cycloneSlashCounter;
    [HideInInspector] public float dashTimer;//冲刺持续时间
    [HideInInspector] public float dashCDTimer;
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
        SkillInit();
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
        playerAction.Player.Move.performed += Move;
        playerAction.Player.Move.canceled += Move;
        playerAction.Player.Jump.performed += Jump;
        playerAction.Player.Jump.canceled += Jump;
        playerAction.Player.Dash.performed += Dash;
        playerAction.Player.Attack.started += AttackStart;
        playerAction.Player.Attack.canceled += AttackCancle;

        gravityscale = rb.gravityScale;
    }
    private void OnDisable()
    {
        playerAction.Player.Move.performed -= Move;
        playerAction.Player.Move.canceled -= Move;
        playerAction.Player.Jump.performed -= Jump;
        playerAction.Player.Jump.canceled -= Jump;
        playerAction.Player.Dash.performed -= Dash;
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
        //if(!canControl)
        //    return;
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
    #region DASH

    void Dash(InputAction.CallbackContext context)
    {
        StartCoroutine(dash());
    }

    IEnumerator dash()
    {
        float t = preInputTime;
        yield return null;
        isDashing = true;
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
            effectManager.NACharge();
            t = NAchargeTime;
            while(attackPress && t > 0)
            {
                t -= Time.deltaTime;
                yield return null;
            }
            EventManager.instance.VoidEventInvoke("NAChargeOver");
            EventManager.instance.RemoveVoidEvent("NAChargeOver");
            if (t > 0)
                yield break;
            if(attackPress)
            {
                effectManager.NACharged();
                while (attackPress)
                {
                    yield return null;
                }
                EventManager.instance.VoidEventInvoke("NAChargedOver");
                EventManager.instance.RemoveVoidEvent("NAChargeOver");
                if (moveInput.y < 0.3f) 
                {
                    isAttacking = true;
                    stateMechine.ChangeState(StateType.bigSlash);
                }
                else
                {
                    Debug.Log("cyclone");
                    isAttacking = true;
                    stateMechine.ChangeState(StateType.cycloneSlash);
                }
            }
        }
    }


    public void AttackOver()
    {
        isAttacking = false;
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
        anim.SetBool("isDashing", isDashing);
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
        AddNormalDash();
        AddBigNail();
        AddCycloneNail();
    }
    public void AddNormalDash()
    {
        stateMechine.AddState(StateType.dash, new PlayerDashState(this));
    }
    public void AddBigNail()
    {
        stateMechine.AddState(StateType.bigSlash, new PlayerNABigSlashState(this));
    }
    public void AddCycloneNail()
    {
        stateMechine.AddState(StateType.cycloneSlash, new PlayerNACycloneState(this));
    }











    #endregion
    public void SetGravityScale(float gravityValue)
    {
        rb.gravityScale = gravityValue;
    }




}
