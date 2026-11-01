using System.Collections;
using UnityEngine;

public class PlayerNACycloneState : BaseState<PlayerController>
{
    public PlayerNACycloneState(PlayerController owner)
    {
        this.owner = owner;
    }

    int count;

    public override void OnEnter()
    {
        owner.SetGravityScale(0f);
        owner.rb.linearVelocity = new Vector2(0, -owner.cycloneSlashFallSpeed);
        owner.anim.SetTrigger("cycloneAttack");
        count = 2;
    }
    public override void OnExit()
    {
        if (owner.isAttacking)
            owner.AttackOver();
        owner.SetGravityScale(owner.gravityscale);
    }
    public override void OnUpdate()
    {
        if (owner.moveInput.x > 0.5f)
            owner.rb.linearVelocity = new Vector2(owner.cycloneSlashMoveSpeed, owner.rb.linearVelocity.y);
        else if (owner.moveInput.x < -0.5f)
            owner.rb.linearVelocity = new Vector2(-owner.cycloneSlashMoveSpeed, owner.rb.linearVelocity.y);
        else
            owner.rb.linearVelocity = new Vector2(0, owner.rb.linearVelocity.y);
        if (!owner.isGround)
        {
            count--;
            owner.cycloneSlashCounter++;
            owner.attackPress = false;
        }
        if (owner.cycloneSlashCounter <= 1)
        {
            owner.anim.SetTrigger("cycloneAttackOver");
            owner.stateMechine.ChangeState(StateType.idel);
        }

    }
}