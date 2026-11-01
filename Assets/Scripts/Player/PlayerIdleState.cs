using System.Collections;
using UnityEngine;
public class PlayerIdelState : BaseState<PlayerController>
{
    public PlayerIdelState(PlayerController owner)
    {
        this.owner = owner;
    }
    public override void OnEnter()
    {
        owner.isGround = true;
        owner.rb.linearVelocity = Vector2.zero;
    }

    public override void OnExit()
    {

    }

    public override void OnUpdate()
    {
        if (owner.isDashing)
            owner.stateMechine.ChangeState(StateType.dash);
    }
}