using System.Collections;
using UnityEngine;

public class PlayerDashState : BaseState<PlayerController>
{
    public PlayerDashState(PlayerController owner)
    {
        this.owner = owner;
    }
    float vector;

    public override void OnEnter()
    {
        owner.dashTimer = owner.dashTime;
        owner.SetGravityScale(0f);
        owner.dashCDTimer = owner.dashCD;
        owner.canControl = false;
        if (owner.isAttacking)
        {
            owner.AttackOver();
        }
        if (owner.moveInput.x != 0)
        {
            vector = owner.moveInput.x > 0 ? 1 : -1;
            owner.transform.localScale = new Vector3(-vector, 1, 1);
            owner.facingRight = vector > 0 ? true : false;
        }
        else if(owner.facingRight)
        {
            vector = 1;
        }
        else
        {
            vector = -1;
        }
    }

    public override void OnExit()
    {
        owner.isDashing = false;
        owner.canControl = true;
        owner.dashAttack = false;
        owner.SetGravityScale(owner.gravityscale);
    }

    public override void OnUpdate()
    {
        if (owner.dashTimer > 0f)
            owner.dashTimer -= Time.deltaTime;

        owner.rb.linearVelocity = new Vector2(vector * owner.dashSpeed, 0f);
        if (owner.dashTimer < 0f)
        {
            owner.stateMechine.ChangeState(StateType.idel);

        }
    }
}