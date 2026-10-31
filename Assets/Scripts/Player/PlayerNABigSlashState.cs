using System.Collections;
using UnityEngine;

public class PlayerNABigSlashState : BaseState<PlayerController>
{

    public PlayerNABigSlashState(PlayerController owner)
    {
        this.owner = owner;
    }


    public override void OnEnter()
    {
        owner.SetGravityScale(0f);
        owner.rb.linearVelocity = new Vector2(0, -owner.bigSlashFallSpeed);
        owner.anim.SetTrigger("bigAttack");
    }

    public override void OnExit()
    {
        owner.SetGravityScale(owner.gravityscale);

        Debug.Log(owner.gravityscale);
    }

    public override void OnUpdate()
    {
        if (!owner.isAttacking)
            owner.stateMechine.ChangeState(StateType.idel);
    }


}