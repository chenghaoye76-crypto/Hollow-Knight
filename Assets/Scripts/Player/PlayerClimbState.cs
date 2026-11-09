using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClimbState : BaseState<PlayerController>
{
    public PlayerClimbState(PlayerController owner)
    {
        this.owner = owner;
    }

    public override void OnEnter()
    {
        owner.SetGravityScale(0f);
        owner.audioManager.WallSlideSound();
        owner.isClimbing = true;
        owner.rb.linearVelocity = Vector2.zero;
        owner.highJump = false;
        owner.canDash = true;
        owner.canTurn = false;
    }

    public override void OnExit()
    {
        owner.canTurn = true;
        owner.isClimbing = false;
        owner.SetGravityScale(owner.gravityscale);
    }

    public override void OnUpdate()
    {
        owner.Move();
        //if (owner.isSDCharge)
        //    owner.rb.linearVelocity = Vector2.zero;
        //else
            owner.rb.linearVelocity = new Vector2(owner.rb.linearVelocity.x, -owner.maxSlideSpeed);

        if (owner.isGround)
        {
            owner.stateMechine.ChangeState(StateType.idel);
            owner.audioManager.SoftLandSound();
        }
        else if (!owner.touchWall || owner.moveInput.y < -0.5f)
        {
            owner.stateMechine.ChangeState(StateType.jump);
        }
        else if (owner.isJumping)
            owner.stateMechine.ChangeState(StateType.jump);
        else if (owner.isDashing)
        {
            owner.stateMechine.ChangeState(StateType.dash);
        }
    }
}
