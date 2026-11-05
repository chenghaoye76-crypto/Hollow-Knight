using System.Collections;
using UnityEngine;

public class PlayerDoubleJumpState : BaseState<PlayerController>
{
    public PlayerDoubleJumpState(PlayerController owner)
    {
        this.owner = owner;
    }
    bool flag;
    public override void OnEnter()
    {
        owner.isHardLand = false;
        owner.hardLandTimer = 0f;
        owner.isGround = false;

        if (owner.isJumping)
            flag = true;
        else
            flag = false;
    }

    public override void OnExit()
    {
        owner.isDoubelJumping = false;
        owner.isJumping = false;
        owner.isFalling = false;
        owner.highJumpTimer = 0;
        owner.highJumpLowTimer = 0;
        owner.SetGravityScale(owner.gravityscale);
        //if (owner.effectManager.jumpEffectObj != null)
        //    owner.effectManager.jumpEffectObj.transform.GetComponent<Poolbb>().End();
    }

    public override void OnUpdate()
    {
        if (!owner.highJump)
            owner.highJumpTimer = 0f;
        {
            owner.highJumpTimer -= Time.deltaTime;
        }
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
        owner.Move();
        if (owner.highJumpLowTimer > 0 || owner.highJumpTimer > 0f && owner.highJump)
            owner.rb.linearVelocity = new Vector2(owner.rb.linearVelocity.x, owner.doubleJumpForce);
        else if (owner.rb.linearVelocity.y > 0 && (/*owner.Checkfloor() ||*/ !owner.highJump)) 
        {
            owner.rb.linearVelocity = new Vector2(owner.rb.linearVelocity.x, 0f);
        }
        else if (owner.rb.linearVelocity.y < 0)
        {
            if (!owner.isFalling)
            {
                owner.highJump = false;
                owner.isFalling = true;
                owner.SetGravityScale(owner.gravityscale /** owner.fallGravityMultiplier*/);
            }
            owner.rb.linearVelocity = new Vector2(owner.rb.linearVelocity.x, Mathf.Max(-owner.maxFallSpeed, owner.rb.linearVelocity.y));
        }
        else
            owner.SetGravityScale(owner.gravityscale);


        //状态切换
        if (owner.isDashing)
            owner.stateMechine.ChangeState(StateType.dash);
        else if (owner.isFalling && owner.isGround) 
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