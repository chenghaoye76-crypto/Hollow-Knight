using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
                _stateMachine.AddState(StateType.jump, new PlayerJumpState(this));
                _stateMachine.AddState(StateType.move, new PlayerMoveState(this));
            }
            return _stateMachine;
        }
    }
    [HideInInspector] public Animator anim;
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public CapsuleCollider2D coll;
    [HideInInspector] public Collider2D quakeCollider;



    private PlayerAction playerAction;
    private PlayerEffectManager effectManager;
    [Header("Movemnet details")]
    [SerializeField] private float moveSpeed = 8.3f;
    [SerializeField] public bool isHardLand;
    [HideInInspector] public Vector2 moveInput;
    private bool isJumpCancle;
    private bool isJumpRise;
    private float MaxVelocityY = 20.9f;
    [Header("Jump details")]
    [SerializeField] private float highJumpTime = 0.2f;//最长跳跃时间
    [SerializeField] private float highJumpLowTime = 0.08f;//最短跳跃时间
    public float jumpForce = 10f;
    public float doubleJumpForce = 10f;
    public float wallJumpForce = 10f;
    public float jumpMoveSpeed = 6f;
    public float maxFallSpeed = 30f;//最大下落速度
    public float highDoubelJumpTime;
    public float coyoteTime = 0.1f;
    public float hardLandTime = 1.1f;
    [Header("Attack details")]
    public float NAchargeTime;
    public float bigSlashFallSpeed = 0.9f;
    public float cycloneSlashFallSpeed = 4.9f;
    public float cycloneSlashMoveSpeed;
    public float dashSlashFallSpeed = 0.9f;
    public float dashSlashSpeed = 15f;
    public float dashSlashDeceleration = 187.5f;

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
    [HideInInspector] public float jumpPreInputTime = 0.4f;
    [HideInInspector] public bool attackPress;
    [HideInInspector] public bool facingRight;
    [HideInInspector] public bool touchWall;//碰墙
    [HideInInspector] public bool highJump;//高跳
    [HideInInspector] public float gravityscale = 4.84f;
    [HideInInspector] public bool dashAttack = false;
    [HideInInspector] public bool skillPress;
    [HideInInspector] public bool fireBallPushStarted;
    [HideInInspector] public bool isQuake;
    public bool canControl = true;
    public bool canMove = true;
    public bool canTurn = true;
    public bool isGround;
    public bool isJumping;
    public bool isMinJumping;
    public bool isDoubelJumping;
    public bool isFalling;
    public bool isAttacking;
    public bool isDashing;
    public bool isUsingSkill; 

    [Header("Skill details")]
    public float foucusTime = 0.83f;
    public float fireBallPushForce = 3f;
    public float fireBallPushTime = 0.2f;
    public float quakeSpeed = 30f;

    [Header("Timer")]
    public float highJumpTimer;//最长跳跃时间
    public float highJumpLowTimer;//最短跳跃时间
    [HideInInspector] public float hardLandTimer;//用于判断是否为硬着陆
    [HideInInspector] public float coyoteTimer;
    [HideInInspector] public int cycloneSlashCounter;
    [HideInInspector] public float dashTimer;//冲刺持续时间
    [HideInInspector] public float dashCDTimer;
    private float dropTime = 1.1f;
    private float dropTimer;


    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        coll = transform.Find("enemyCollider").GetComponent<CapsuleCollider2D>();
        quakeCollider = transform.Find("quakeCollider").GetComponent<BoxCollider2D>();
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
        stateMechine.Update();
        if (isGround)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;
    }
    private void FixedUpdate()
    {
        MoveFixedUpdate();
        LimitVelocityY();
    }
    private void OnEnable()
    {
        playerAction.Player.Jump.started += JumpStart;
        playerAction.Player.Jump.canceled += JumpCancled;
        playerAction.Player.Dash.performed += Dash;
        playerAction.Player.Attack.started += AttackStart;
        playerAction.Player.Attack.canceled += AttackCancle;
        playerAction.Player.Skill.started += SkillStart;
        playerAction.Player.Skill.canceled += SkillCancled;

        gravityscale = rb.gravityScale;
    }
    private void OnDisable()
    {
        playerAction.Player.Jump.started -= JumpStart;
        playerAction.Player.Jump.canceled -= JumpCancled;
        playerAction.Player.Dash.performed -= Dash;
        playerAction.Player.Attack.started -= AttackStart;
        playerAction.Player.Attack.canceled -= AttackCancle;
        playerAction.Player.Skill.started -= SkillStart;
        playerAction.Player.Skill.canceled -= SkillCancled;
    }

    #region InputEvent
    public void JumpStart(InputAction.CallbackContext context)
    {
        highJump = true;
        Tojump();
    }
    public void JumpCancled(InputAction.CallbackContext context)
    {
        highJump = false;
    }
    public void AttackStart(InputAction.CallbackContext context)
    {
        attackPress = true;
        ToAttack();
    }
    public void AttackCancle(InputAction.CallbackContext context)
    {
        attackPress = false;
    }
    public void SkillStart(InputAction.CallbackContext context)
    {
        skillPress = true;
        ToSkill();
    }
    public void SkillCancled(InputAction.CallbackContext context)
    {
        skillPress = false;
    }




    #endregion

    #region Motion

    #region TURN
    private void Turn()
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

    #region MOVE
    public void MoveFixedUpdate()
    {
        moveInput = playerAction.Player.Move.ReadValue<Vector2>();
    }

    public void Move()
    {
        if (!canControl)
            return;
        if (moveInput != null)
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        Turn();
    }




    #endregion

    #region JUMP
    private void Tojump()
    {
        StartCoroutine(toJump());
    }
    IEnumerator toJump()
    {
        float t = jumpPreInputTime;
        while (t > 0)
        {
            if (!canControl && stateMechine.currentStateID == StateType.douleJump)
            {
                t -= Time.deltaTime;
                yield return null;
            }
            else
                break;
        }
        yield return new WaitForFixedUpdate();
        if (!canControl && stateMechine.currentStateID == StateType.douleJump)
            yield break;


        if (highJump && coyoteTimer > 0)
        {
            NormalJump();
        }
        else if(highJump && stateMechine.currentStateID == StateType.jump)
        {
            DoubleJump();
        }


    }

    void NormalJump()
    {
        isMinJumping = true;
        isJumping = true;
        highJumpTimer = highJumpTime;
        highJumpLowTimer = highJumpLowTime;
        if (moveInput.x == 0)
            //effectManager.JumpPuff();
            ;
        else
            //effectManager.MoveJumpPuff();
            ;
        //audioManager.JumpSound();
        isGround = false;
        coyoteTimer = 0;
    }

    void DoubleJump()
    {
        StartCoroutine(doubleJump());
    }
    IEnumerator doubleJump()
    {
        isMinJumping = true;
        float t = 0.1f;
        while (t > 0)
        {
            yield return null;
            t -= Time.deltaTime;
            if (isGround)
            {
                NormalJump();
                yield break;
            }
        }
        //if (!character.Skill.DoubleJump)
        //{
        //    isMinJumping = false;
        //    yield break;
        //}
        isDoubelJumping = true;
        isJumping = true;
        highJumpTimer = highDoubelJumpTime;
        highJumpLowTimer = highJumpLowTime + 0.05f;
        //audioManager.Swing();
        anim.SetTrigger("doubleJump");
        effectManager.DoubleJumpEffect();
        effectManager.DoubleWings();
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
                if (stateMechine.currentStateID == StateType.dash)
                    dashAttack = true;
                else if (moveInput.y < 0.3f) 
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

    #region Skill
    public void SkillOver()
    {
        isUsingSkill = false;
        canControl = true;
        canMove = true;
    }
    private void ToSkill()
    {
        //if (character.Attribute.CurrentSoul < 33)
        //    return;
        StartCoroutine(skillPressJudge());
    }
    IEnumerator skillPressJudge()
    {
        bool isLong = true;
        float t = preInputTime;
        //预输入判断
        while (t > 0)
        {
            if (!canControl /*|| isMap || isMinJumping*/)
            {
                t -= Time.deltaTime;
                yield return null;
            }
            else
                break;
        }
        if (!canControl /*|| isMap || isMinJumping*/)
            yield break;
        if (moveInput.y < -0.2f)
        {
            isUsingSkill = true;
            rb.linearVelocity = Vector2.zero;
            if (isGround)
            {

            }
            else
            {
                Debug.Log("quake");
                anim.SetTrigger("quake2");
                stateMechine.ChangeState(StateType.quake);
                canMove = false;
            }
        }
        else if (moveInput.y > 0.2f)
        {
            isUsingSkill = true;
            rb.linearVelocity = Vector2.zero;
            anim.SetTrigger("scream2");
            stateMechine.ChangeState(StateType.scream);
            canMove = false;
        }
        else
        {
            isUsingSkill = true;
            anim.SetTrigger("fireBall2");
            stateMechine.ChangeState(StateType.fireBall);
        }
    }
    public void StartFireBallPush()
    {
        fireBallPushStarted = true;
    }
    public void QuakeOver()
    {
        SkillOver();
        StartCoroutine(quakeOver());
    }
    IEnumerator quakeOver()
    {
        yield return new WaitForSeconds(0.4f);
        //if (!isInvincible)
        coll.enabled = true;
    }
    public void StartQuake2()
    {
        //audioManager.QuakeDash();
        isQuake = true;
    }
    public void QuakeLand2_Extra()
    {
        effectManager.QuakeBlast2();
        effectManager.QuakeMage2();

        //CameraImpluseManager.instance.Skill();
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
        canTurn = false;
        effectManager.DashAttack();
    }







    #endregion

    #region SkillEffect
    public void FireBall2()
    {
        effectManager.FireBall2();
    }
    public void Scream2()
    {
        effectManager.ScreamEffect2();
        if (isGround)
            effectManager.ScreamBase2();
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
        AddDashNail();
        AddShadeFireBall();
        AddShadeScream();
        AddShadeQuake();
        AddDoubleJump();
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
    public void AddDashNail()
    {
        stateMechine.AddState(StateType.dashSlash, new PlayerNADashSlashState(this));
    }
    public void AddShadeFireBall()
    {
        stateMechine.AddState(StateType.fireBall, new PlayerFireBallState(this));
    }
    public void AddShadeScream()
    {
        stateMechine.AddState(StateType.scream, new PlayerScreamState(this));
    }
    public void AddShadeQuake()
    {
        stateMechine.AddState(StateType.quake, new PlayerQuakeState(this));
    }
    public void AddDoubleJump()
    {
        stateMechine.AddState(StateType.douleJump, new PlayerDoubleJumpState(this));
    }









    #endregion
    public void SetGravityScale(float gravityValue)
    {
        rb.gravityScale = gravityValue;
    }




}