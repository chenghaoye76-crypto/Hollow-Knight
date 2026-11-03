using System.Collections;
using UnityEngine;

public class PlayerNADashSlashState : BaseState<PlayerController>
{
    public PlayerNADashSlashState(PlayerController owner)
    {
        this.owner = owner;
    }
    public override void OnEnter()
    {
        owner.SetGravityScale(0f);
        owner.canControl = false;
        owner.rb.linearVelocity = new Vector2((owner.facingRight ? 1 : -1) * owner.dashSlashSpeed, -owner.dashSlashFallSpeed);
        owner.anim.SetTrigger("dashAttack"); 
    }

    public override void OnExit()
    {
        if (owner.isAttacking)
            owner.AttackOver();
        owner.SetGravityScale(owner.gravityscale);
        owner.canControl = true;
    }

    public override void OnUpdate()
    {
        owner.rb.linearVelocity = new Vector2(owner.rb.linearVelocity.x + (owner.facingRight ? -1 : 1) * owner.dashSlashDeceleration * Time.deltaTime, owner.rb.linearVelocity.y);
        owner.stateMechine.ChangeState(StateType.idel);
    }
}