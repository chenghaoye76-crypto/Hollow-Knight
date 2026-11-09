using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHurtState : BaseState<PlayerController>
{
    public PlayerHurtState(PlayerController owner)
    {
        this.owner = owner;
    }
    public override void OnEnter()
    {
        owner.isHurt = true;
        owner.canControl = false;
        owner.SetGravityScale(0f);

        owner.anim.SetTrigger("Hurt");

        //owner.audioManager.HurtSound();

        owner.effectManager.Hurt();
    }

    public override void OnExit()
    {
        owner.canControl = true;
        owner.SetGravityScale(owner.gravityscale);
    }

    public override void OnUpdate()
    {
        if (!owner.isHurt)
            if (owner.isGround)
                owner.stateMechine.ChangeState(StateType.idel);
            else if (owner.stateMechine.previousStateID == StateType.douleJump)
                owner.stateMechine.ChangeState(StateType.douleJump);
            else
                owner.stateMechine.ChangeState(StateType.jump);
    }
}
