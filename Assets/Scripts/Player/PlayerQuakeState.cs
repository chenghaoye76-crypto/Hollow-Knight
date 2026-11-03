using System.Collections;
using UnityEngine;

public class PlayerQuakeState : BaseState<PlayerController>
{
    public PlayerQuakeState(PlayerController owner)
    {
        this.owner = owner;
    }
    public override void OnEnter()
    {
        owner.coll.enabled = false;
        owner.SetGravityScale(0f);
        owner.canControl = false;
        owner.rb.linearVelocity = Vector2.zero;
        owner.isQuake = false;
        //owner.audioManager.QuakePrepare();
        //owner.effectManager.QuakeCharge();

        owner.quakeCollider.enabled = true;
    }

    public override void OnExit()
    {
        owner.SetGravityScale(owner.gravityscale);
        owner.quakeCollider.enabled = false;
    }

    public override void OnUpdate()
    {
        if (owner.isQuake)
            owner.rb.linearVelocity = new Vector2(0, -owner.quakeSpeed);

        if (owner.isGround)
        {
            owner.stateMechine.ChangeState(StateType.idel);
        }
    }
}