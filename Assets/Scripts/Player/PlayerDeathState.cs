using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDeathState : BaseState<PlayerController>
{
    public PlayerDeathState(PlayerController owner)
    {
        this.owner = owner;
    }

    public override void OnEnter()
    {
        owner.isDeath = true;
        owner.canControl = false;

        owner.rb.linearVelocity = Vector2.zero;
        owner.coll.enabled = false;
        owner.SetGravityScale(0f);

        owner.anim.SetTrigger("death");

        owner.effectManager.Dead();

        //owner.audioManager.DeathSound();
    }

    public override void OnExit()
    {
        owner.SetGravityScale(owner.gravityscale);
    }

    public override void OnUpdate()
    {
        if (!owner.isDeath)
            if (owner.isGround)
                owner.stateMechine.ChangeState(StateType.idel);
            else
                owner.stateMechine.ChangeState(StateType.jump);
    }
}
