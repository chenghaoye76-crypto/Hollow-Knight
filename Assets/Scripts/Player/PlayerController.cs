using System;
using System.Collections;
using System.Threading;
using TMPro;
using Unity.AppUI.Redux;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using static UnityEditor.PlayerSettings;

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
                _stateMachine.AddState(StateType.focus, new PlayerFoucusState(this));
                _stateMachine.AddState(StateType.hurt, new PlayerHurtState(this));
                //_stateMachine.AddState(StateType.spikeHurt, new PlayerSpikeHurtState(this));
                _stateMachine.AddState(StateType.death, new PlayerDeathState(this));
                //_stateMachine.AddState(StateType.sit, new PlayerSitState(this));
            }
            return _stateMachine;
        }
    }
    [HideInInspector] private PlayerAction playerAction;
    [HideInInspector] public PlayerAudioManager audioManager;
    [HideInInspector] public Animator anim;
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public CapsuleCollider2D coll;
    [HideInInspector] public CapsuleCollider2D coll_2;
    [HideInInspector] public Collider2D quakeCollider;
    [HideInInspector] public Character character;
    [HideInInspector] public SpriteRenderer spriteRenderer;
    [HideInInspector] public PlayerEffectManager effectManager;


    [Header("Check details")]
    public Vector2 bottomOffset;//偏移量
    public Vector2 topOffset;
    public Vector2 frontOffset;
    public float checkRaduis;
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public bool faceRight = true;

    [Header("Movemnet details")]
    [SerializeField] public bool isHardLand;
    [HideInInspector] public Vector2 moveInput;
    private bool isJumpCancle;
    private bool isJumpRise;
    private float MaxVelocityY = 20.9f;
    public float moveSpeed = 9f;
    public float walkSpeed = 8.3f;
    public float mapSpeed = 4f;
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
    [Header("Hurt details")]
    public float hurtTime = 0.2f;
    public float invincibleTime = 1.3f;
    public float spikeHurtInvincibleTime = 4f;
    int hurtDir;
    [Header("Attack details")]
    public float attackBreakTime;
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


    [Header("Climbing details")]
    public float maxSlideSpeed = 8.6f;//滑落速度
    public float wallJumpTime;
    public float minWallJumpTime;
    public float accelerationOfWallJump;
    public float maxVelocityX;
    public float slideGravityMultplier;




    [Header("Collision details")]
    [SerializeField] private float groundCheckDistace;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Check state")]
    [HideInInspector] public float longPressJudgeTime = 0.15f;
    [HideInInspector] public float preInputTime = 0.2f;
    [HideInInspector] public float jumpPreInputTime = 0.4f;
    [HideInInspector] public bool attackPress;
    [HideInInspector] public bool facingRight;
    [HideInInspector] public bool touchWall;//碰墙
    [HideInInspector] public bool highJump;//高跳
    [HideInInspector] public bool preAttackisNormal = false;
    [HideInInspector] public bool canFocus;
    [HideInInspector] public float gravityscale = 4.84f;
    [HideInInspector] public bool dashAttack = false;
    [HideInInspector] public bool skillPress;
    [HideInInspector] public bool fireBallPushStarted;
    [HideInInspector] public bool isQuake;
    [HideInInspector] public bool inputDirCantControl = false;
    public bool canControl = true;
    public bool canMove = true;
    public bool canTurn = true;
    public bool canDash = true;
    public bool isGround;
    public bool isJumping;
    public bool isMinJumping;
    public bool isDoubelJumping;
    public bool isFalling;
    public bool isAttacking;
    public bool isDashing;
    public bool isUsingSkill;
    public bool isFocus;
    public bool isHurt;
    public bool isDeath;
    public bool isInvincible;
    public bool isTurn;
    public bool isMap;
    public bool isWalking;
    public bool isWallJumping;
    public bool isUpPushing;
    public bool isClimbing;

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
    [HideInInspector] public float attackBreakTimer;
    [HideInInspector] public float foucusTimer;
    Vector3 pos = new Vector3(0, 2.45f, 0);
    private float dropTime = 1.1f;
    private float dropTimer;


    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        coll = transform.Find("enemyCollider").GetComponent<CapsuleCollider2D>();
        quakeCollider = transform.Find("quakeCollider").GetComponent<BoxCollider2D>();
        effectManager = GetComponentInChildren<PlayerEffectManager>();
        character = GetComponent<Character>();
        playerAction = new PlayerAction();
        playerAction.Player.Enable();
        SkillInit();
    }
    private void Update()
    {
        touchWall = CheckFrontWall();
        UpdateAnim();
        CheckGround();
        CheckHardLand();
        stateMechine.Update();
        if (isGround)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;
        attackBreakTimer -= Time.deltaTime;
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
        coll_2 = transform.Find("ObjectCollider").GetComponent<CapsuleCollider2D>();
        audioManager = GetComponentInChildren<PlayerAudioManager>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        character.Init();
        #region AddAction
        EventManager.instance.AddVoidAction("GameStart", name, GameStart);
        EventManager.instance.AddIntAction("playerHurt", name, Hurt);












        #endregion
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


        #region RemoveAction

        EventManager.instance.RemoveVoidAction("GameStart", name);

        #endregion
    }
    public void GameStart()
    {
        coll_2.enabled = true;
        //canControl = false;
        _stateMachine.AddState(StateType.jump, new PlayerJumpState(this));
        if (stateMechine.currentStateID == StateType.sit)
        {
            canMove = false;
            isGround = true;
            anim.SetTrigger("sleep");
            EventManager.instance.VoidEventInvoke("UIShow");
            StartCoroutine(gameStart1());
        }
        //else if (GameDataManager.instant.data.savePoint.isGet)
        //{
        //    anim.SetTrigger("idle");
        //    EventManager.instance.VoidEventInvoke("UIShow");
        //    anim.SetTrigger("prostrate");
        //}
        else
            StartCoroutine(gameStart2());

        SkillInit();
    }
    IEnumerator gameStart1()
    {
        yield return new WaitForSeconds(1f);
        if (stateMechine.currentStateID == StateType.sit)
        {
            anim.SetTrigger("awake");
        }
    }
    IEnumerator gameStart2()
    {
        while (!isGround)
        {
            yield return null;
        }
        anim.SetTrigger("idle");
        anim.SetTrigger("longLand");
        //MenuAudioManager.Instant.FirstGameBG();
        //GameDataManager.instant.data.savePoint = new SavePoint(false, transform.position, SceneLoadManager.instance.currentGameScene);
        //GameDataManager.instant.Save(false);
        yield return new WaitForSeconds(2f);
        EventManager.instance.VoidEventInvoke("UIShow");
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
        if (!canTurn || !canMove || Mathf.Abs(moveInput.x) <= 0.1f)
            return;

        bool turnRight = moveInput.x > 0f;
        bool directionChanged = facingRight != turnRight;
        facingRight = turnRight;

        Vector3 scale = transform.localScale;
        scale.x = facingRight ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;

        if (isGround && directionChanged)
            anim.SetTrigger("isTurning");
    }
    
    private void TurnOver()
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
        float speed;
        if (!canControl)
            return;
        if (!canMove)
            speed = 0f;
        else if (isMap)
            speed = mapSpeed;
        else if (isWalking && isGround)
            speed = walkSpeed;
        else
            speed = moveSpeed;
        if (moveInput.x > 0.3f)
            rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
        else if (moveInput.x < -0.3f)
            rb.linearVelocity = new Vector2(-speed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
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
            if (!canControl || isMap || isUpPushing || coyoteTimer < 0 && stateMechine.currentStateID == StateType.douleJump)
            {
                t -= Time.deltaTime;
                yield return null;
            }
            else
                break;
        }
        yield return new WaitForFixedUpdate();
        if (!canControl || isMap || isUpPushing || coyoteTimer < 0 && stateMechine.currentStateID == StateType.douleJump)
            yield break;

        if (isTurn)
            TurnOver();

        if (isClimbing && highJump && !isWallJumping)
        {
            WallJump();
        }
        else if (highJump && coyoteTimer > 0)
        {
            NormalJump();
        }
        else if (highJump && stateMechine.currentStateID == StateType.jump)
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
            effectManager.JumpPuff();
        else
            effectManager.MoveJumpPuff();
        audioManager.JumpSound();
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
        float t = 0.05f;
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

        isMinJumping = true;
        isDoubelJumping = true;
        isJumping = true;
        highJumpTimer = highDoubelJumpTime;
        highJumpLowTimer = highJumpLowTime + 0.05f;
        audioManager.Swing();
        anim.SetTrigger("doubleJump");
        effectManager.DoubleJumpEffect();
        effectManager.DoubleWings();
    }
    void WallJump()
    {
        audioManager.WallJumpSound();
        effectManager.WallJumpPuff();
        StartCoroutine(wallJump());
    }
    IEnumerator wallJump()
    {
        yield return new WaitForFixedUpdate();
        isWallJumping = true;
        NormalJump();
        highJumpLowTimer = minWallJumpTime + 0.05f;
        CantMove();
        canTurn = false;
        float time = 0f;
        float velocityX = maxVelocityX;
        transform.localScale = new Vector3(-transform.localScale.x, 1, 1);
        while (time < minWallJumpTime)
        {
            yield return null;
            time += Time.deltaTime;
            velocityX -= Time.deltaTime * accelerationOfWallJump;
            if (facingRight)
                rb.linearVelocity = new Vector2(-velocityX, rb.linearVelocity.y);
            else
                rb.linearVelocity = new Vector2(velocityX, rb.linearVelocity.y);
        }
        while (time < wallJumpTime && highJump)
        {
            yield return null;
            time += Time.deltaTime;
            velocityX -= Time.deltaTime * accelerationOfWallJump;
            if (facingRight)
                rb.linearVelocity = new Vector2(-velocityX, rb.linearVelocity.y);
            else
                rb.linearVelocity = new Vector2(velocityX, rb.linearVelocity.y);
        }
        CanMove();
        canTurn = true;
        if (moveInput.x == 0f)
            highJump = false;
        isWallJumping = false;
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
        EventManager.instance.IntEventInvoke("soulIncrease", 11);
        StopCoroutine(attackPressJudge());
        StartCoroutine(attackPressJudge());
    }

    private IEnumerator attackPressJudge()
    {
        yield return new WaitForFixedUpdate();
        float t = preInputTime;
        bool isLong = true;
        //预输入判断
        while (t > 0)
        {
            if (isAttacking || !canControl || attackBreakTimer > 0.1f)
            {
                t -= Time.fixedDeltaTime;
                yield return null;
            }
            else
                break;
        }
        if (isAttacking || !canControl || attackBreakTimer > 0.1f)
            yield break;
        if (moveInput.y > 0.2f) 
        {
            preAttackisNormal = false;
            anim.SetTrigger("upAttack");
            EventManager.instance.IntEventInvoke("playerHurt", 9);
        }
        else if (moveInput.y < -0.2f && !isGround)
        {
            preAttackisNormal = false;
            anim.SetTrigger("downAttack");
        }
        else if (preAttackisNormal && attackBreakTimer > 0f)
        {
            preAttackisNormal = false;
            anim.SetTrigger("secondAttack");
        }
        else
        {
            preAttackisNormal = true;
            anim.SetTrigger("firstAttack");
        }
        isAttacking = true;
        canTurn = false;
        t = longPressJudgeTime;
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
        attackBreakTimer = attackBreakTime;
    }









    #endregion

    #region Skill
    public void SkillOver()
    {
        isUsingSkill = false;
        canControl = true;
        canMove = true;
    }
    public void StartFocus()
    {
        if (!isFocus)
            return;
        //audioManager.Foucus();
        canFocus = true;
        foucusTimer = foucusTime;
        effectManager.FocusEffect();
    }



    private void ToSkill()
    {
        if (character.Attribute.MainSoul < 33)
            return;
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
        t = longPressJudgeTime;
        while (t > 0)
        {
            yield return null;
            t -= Time.deltaTime;
            if (!skillPress || isJumping)
            {
                isLong = false;
                break;
            }
        }
        if (!canControl || /*isMap ||*/ isMinJumping)
            yield break;
        if (!isLong)
        {
            if (moveInput.y < -0.2f)
            {
                isUsingSkill = true;
                rb.linearVelocity = Vector2.zero;
                if (isGround)
                {
                    StartCoroutine(quakeJump());
                }
                else
                {
                    anim.SetTrigger("quake2");
                    stateMechine.ChangeState(StateType.quake);
                    canMove = false;
                    EventManager.instance.IntEventInvoke("soulDecrease", 33);
                }
            }
            else if (moveInput.y > 0.2f)
            {
                isUsingSkill = true;
                rb.linearVelocity = Vector2.zero;
                anim.SetTrigger("scream2");
                stateMechine.ChangeState(StateType.scream);
                canMove = false;
                EventManager.instance.IntEventInvoke("soulDecrease", 33);
            }
            else
            {
                isUsingSkill = true;
                anim.SetTrigger("fireBall2");
                stateMechine.ChangeState(StateType.fireBall);
                EventManager.instance.IntEventInvoke("soulDecrease", 33);
            }
        }
        else if (isGround)
        {
            isFocus = true;
            canFocus = false;
            stateMechine.ChangeState(StateType.focus);
            canMove = false;
        }
    }
    IEnumerator quakeJump()
    {
        canControl = false;
        canMove = false;
        highJump = true;
        NormalJump();

        float timer = highJumpTime;
        while (timer > 0)
        {
            yield return null;
            timer -= Time.deltaTime;
            //if (isHurt)
            //{
            //    highJump = false;
            //    canMove = true;
            //    yield break;
            //}
        }

        rb.linearVelocity = Vector2.zero;
        //if (character.Skill.ShadeQuake)
            anim.SetTrigger("quake2");
        //else if (character.Skill.Quake)
        //    anim.SetTrigger("quake1");
        stateMechine.ChangeState(StateType.quake);
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

    #region Hurt
    public void HurtOver()
    {
        isHurt = false;
        isAttacking = false;
        isDashing = false;
        isUsingSkill = false;
        isMinJumping = false;
        dashCDTimer = 0;
        canDash = true;
        canMove = true;
    }

    public void Hurt(int n)
    {
        if (isHurt)
            return;

        if (isTurn)
            TurnOver();

        rb.linearVelocity = Vector2.zero;
        JudgeHurt(n);
    }

    public void JudgeHurt(int n)
    {
        // 转向
        if (hurtDir * transform.localScale.x > 0)
        {
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
            facingRight = !facingRight;
        }

        // 血量判断
        if (character.Attribute.CurrentHealth <= n)
            Death();
        else
        {
            stateMechine.ChangeState(StateType.hurt);
            StartCoroutine(_hurt());
        }

        EventManager.instance.IntEventInvoke("healthDecrease", n);
    }

    public void SpikeHurt(int n)
    {
        if (isHurt)
            return;
        JudgeSpikeHurt(n);
    }

    private void JudgeSpikeHurt(int n)
    {
        if (character.Attribute.CurrentHealth <= n)
            Death();
        else
        {
            stateMechine.ChangeState(StateType.spikeHurt);
            StartCoroutine(spikeHurt());
        }
        Debug.Log("spikeHurt");
        EventManager.instance.IntEventInvoke("healthDecrease", n);
    }

    private void Death()
    {
        if (isDeath)
            return;
        stateMechine.ChangeState(StateType.death);
        StartCoroutine(death());
    }

    IEnumerator _hurt()
    {
        coll.enabled = false;
        isInvincible = true;

        int dir = facingRight ? 1 : -1;

        TimeManager.Instant.PlayerHurt();
        //MenuAudioManager.Instant.PlayerHurt();

        float t = 0;
        while (t < hurtTime)
        {
            yield return null;
            t += Time.deltaTime;
            rb.linearVelocity = new Vector2(dir * 15f, 7.5f);
        }

        HurtOver();
        StartCoroutine(Invincible(invincibleTime));
    }

    public void HurtTurn()
    {
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        facingRight = !facingRight;
    }

    private IEnumerator spikeHurt()
    {
        coll.enabled = false;
        isInvincible = true;

        rb.linearVelocity = Vector2.zero;
        TimeManager.Instant.PlayerHurt();
        //MenuAudioManager.Instant.PlayerHurt();
        //MaskCanvas.Instant.Show(SpikeHurtOver);
        while (isHurt)
        {
            yield return null;
            rb.linearVelocity = new Vector2(hurtDir * 7f, 4f);
        }
        HurtOver();
    }

    private IEnumerator death()
    {
        coll.enabled = false;
        yield return new WaitForFixedUpdate();
        isHurt = true;

        TimeManager.Instant.PlayerHurt();
        //MenuAudioManager.Instant.PlayerHurt();

        rb.linearVelocity = new Vector2(hurtDir * 7f, 4);
        yield return new WaitForSeconds(0.3f);
        rb.linearVelocity = Vector2.zero;
    }

    public void DeathAnimOver()
    {
        spriteRenderer.color = new Color(1, 1, 1, 0);
        //MaskCanvas.Instant.Show(DeathOver);
        effectManager.Head();
    }

    public void DeathOver()
    {
        GameDataManager.instant.Save();
        UIManager.Instant.ClearPanel();
        StartCoroutine(deathOver());
    }

    IEnumerator deathOver()
    {
        yield return new WaitForSeconds(2f);
        Addressables.LoadSceneAsync("Scenes/Game");
    }

    public void SpikeHurtOver()
    {
        StartCoroutine(spikeHurtOver());
    }

    IEnumerator spikeHurtOver()
    {
        transform.position = pos;
        isHurt = false;
        anim.SetTrigger("idle");
        yield return new WaitForSeconds(0.1f);
        //MaskCanvas.Instant.Unshow();
        StartCoroutine(Invincible(spikeHurtInvincibleTime));
        yield return new WaitForSeconds(0.1f);
        canControl = true;
    }

    private IEnumerator Invincible(float time)
    {
        yield return new WaitForSeconds(time);
        coll.enabled = true;
        isInvincible = false;
    }

    public void UpdateSpikePos(Vector3 pos)
    {
        this.pos = pos;
    }






    #endregion

    #endregion

    #region Control

    #region PlayerControl
    public void CantMove()
    {
        canControl = false;
        canMove = false;
    }

    public void CanMove()
    {
        canControl = true;
        canMove = true;
    }

    public void CantControl()
    {
        canControl = false;
        inputDirCantControl = true;
        canMove = false;
    }

    public void CanControl()
    {
        canMove = true;
        canControl = true;
        inputDirCantControl = false;
    }

    public void Move_can()
    {
        canMove = true;
    }

    public void Move_cant()
    {
        canMove = false;
    }

    public void CanTurn()
    {
        canTurn = true;
    }

    public void CantTurn()
    {
        canTurn = false;
    }

    public void PlayerDisable()
    {
        playerAction.Disable();
        stateMechine.ChangeState(StateType.blank);
        rb.linearVelocity = Vector2.zero;
        SetGravityScale(0);
        anim.SetTrigger("idle");
        canControl = false;
        StartCoroutine(playerDisable());
    }

    IEnumerator playerDisable()
    {
        yield return new WaitForSeconds(0.6f);
        coll_2.enabled = false;
    }

    public void PlayerEnable()
    {
        SetGravityScale(gravityscale);
        stateMechine.ChangeState(StateType.idel);
        anim.SetTrigger("idle");
    }

    public void Disable()
    {
        playerAction.Disable();
    }

    public void Enable()
    {
        playerAction.Enable();
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
    private void UpdateAnim()
    {
        bool isMoving = rb.linearVelocity.x != 0;
        anim.SetFloat("VelocityY", rb.linearVelocityY);
        anim.SetFloat("inputX", Math.Abs(moveInput.x));
        anim.SetBool("isGround", isGround);
        anim.SetBool("isMoving", isMoving);
        //anim.SetBool("isHardLand", isHardLand);
        anim.SetBool("isDashing", isDashing);
        anim.SetBool("isFocus", isFocus);
        anim.SetBool("isClimbing", isClimbing);
        anim.SetBool("isWallJumping", isWallJumping);
        //anim.SetBool("isInvincible", isInvincible);
        //anim.SetBool("isWalking", isWalking);
    }

    private void CheckGround()
    {
        isGround = Physics2D.OverlapCircle((Vector2)transform.position + bottomOffset, checkRaduis, groundLayer);
    }
    public bool CheckFrontWall()
    {
        //if (character.Skill.Climb)
        return Physics2D.OverlapCircle(transform.TransformPoint(frontOffset), checkRaduis, wallLayer);
        //return false;

    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere((Vector2)transform.position + bottomOffset, checkRaduis);//绘制检测范围
        Gizmos.DrawWireSphere((Vector2)transform.position + topOffset, checkRaduis);
        Gizmos.DrawWireSphere((Vector2)transform.position + frontOffset, checkRaduis);
    }
    public bool Checkfloor()
    {
        return Physics2D.OverlapCircle((Vector2)transform.position + topOffset, checkRaduis, groundLayer);
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
    public void SetGravityScale(float gravityValue)
    {
        rb.gravityScale = gravityValue;
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
        AddClimb();
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
    public void AddClimb()
    {
        stateMechine.AddState(StateType.climb, new PlayerClimbState(this));
    }









    #endregion




}