using System.Collections;
using UnityEngine;

public class PlayerJumpState : BaseState<PlayerController>
{
    public PlayerJumpState(PlayerController owner)
    {
        this.owner = owner;
    }
    float timer;
    bool flag = false;
    public override void OnEnter()
    {
        owner.isHardLand = false;
        owner.hardLandTimer = 0f;
        owner.isGround = false;
        owner.anim.SetTrigger("Jump");
        owner.canTurn = true;

        if (owner.isJumping)
        {
            flag = true;
            owner.isFalling = false;
        }
        else
            flag = false;
    }

    public override void OnExit()
    {
        if (!owner.isDoubelJumping && !(owner.highJump && owner.isFalling))
        {
            owner.isJumping = false;
            owner.highJumpTimer = 0;
            owner.highJumpLowTimer = 0;
        }
        owner.isFalling = false;
        owner.SetGravityScale(owner.gravityscale);
    }

    public override void OnUpdate()
    {
        if (owner.isDoubelJumping)
            owner.stateMechine.ChangeState(StateType.douleJump);

        if (!owner.highJump)
            owner.highJumpTimer = 0f;

        if (owner.highJumpTimer > 0)
            owner.highJumpTimer -= Time.deltaTime;
        if (owner.highJumpLowTimer > 0)
        {
            owner.highJumpLowTimer -= Time.deltaTime;
        }
        else if (flag)
        {
            flag = false;
            owner.isMinJumping = false;
        }
        if (owner.rb.linearVelocity.y <= 0)
            owner.hardLandTimer += Time.deltaTime;
        else
            owner.hardLandTimer = 0f;

        if (owner.hardLandTimer >= owner.hardLandTime)
            owner.isHardLand = true;

        //角色速度
       // if (!owner.isWallJumping)
            owner.Move();

        if (owner.highJumpLowTimer > 0 || owner.highJumpTimer > 0f && owner.highJump)
            owner.rb.linearVelocity = new Vector2(owner.rb.linearVelocity.x, owner.jumpForce);
        else if ((/*owner.Checkfloor() || */owner.rb.linearVelocity.y > 0f && owner.highJump == false) /*&& !owner.isWallJumping*/)
        {
            owner.rb.linearVelocity = new Vector2(owner.rb.linearVelocity.x, 0f);
        }
        else if (owner.rb.linearVelocity.y < 0)
        {
            if (!owner.isFalling)
            {
                owner.highJumpTimer = 0f;
                owner.isFalling = true;
                owner.SetGravityScale(owner.gravityscale);
            }
        }
        //状态切换

        if (owner.isDashing)
            owner.stateMechine.ChangeState(StateType.dash);
        if (owner.isFalling && owner.isGround) 
        {
            if (owner.isHardLand)
            {
                owner.stateMechine.ChangeState(StateType.idel);
                //owner.audioManager.HardLandSound();
            }
            else
            {
                if (Mathf.Abs(owner.rb.linearVelocity.x) < 0.01f)
                    owner.stateMechine.ChangeState(StateType.idel);
                else
                    owner.stateMechine.ChangeState(StateType.move);
                //owner.audioManager.SoftLandSound();
            }
        }





    }
}