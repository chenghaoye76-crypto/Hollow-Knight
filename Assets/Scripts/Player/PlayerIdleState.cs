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
        owner.Move();
        if (owner.isDashing)
            owner.stateMechine.ChangeState(StateType.dash);
        else if (owner.isGround && Mathf.Abs(owner.moveInput.x) > 0.01f && Mathf.Abs(owner.rb.linearVelocity.x) > 3f && owner.canMove)
            owner.stateMechine.ChangeState(StateType.move);
        else if (owner.isJumping || !owner.isGround)
            owner.stateMechine.ChangeState(StateType.jump);
    }
}