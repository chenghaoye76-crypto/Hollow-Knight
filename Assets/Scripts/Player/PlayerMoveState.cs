using System.Collections;
using UnityEngine;

public class PlayerMoveState : BaseState<PlayerController>
{
    public PlayerMoveState(PlayerController owner)
    {
        this.owner = owner;
    }
    float t = 0;
    float t1;
    public override void OnEnter()
    {
        owner.isGround = true;
        owner.isHardLand = false;
        owner.canTurn = true;
        t = 0;
        t1 = 0;
    }

    public override void OnExit()
    {
        //owner.audioManager.StopSound();
    }

    public override void OnUpdate()
    {
        owner.Move(); if (owner.isGround && owner.moveInput.x == 0)
            t += Time.deltaTime;
        else
            t = 0;
        t1 += Time.deltaTime;

        if (t1 > 0.1f && t1 < 2f)
        {
            //owner.audioManager.RunSound();
            t1 = 3;
        }
        if (owner.isDashing)
            owner.stateMechine.ChangeState(StateType.dash);
        else if (owner.isJumping || !owner.isGround)
            owner.stateMechine.ChangeState(StateType.jump);
        else if (t > 0.1f /*|| owner.isSDCharge*/)
        {
            owner.stateMechine.ChangeState(StateType.idel);
        }
    }
}