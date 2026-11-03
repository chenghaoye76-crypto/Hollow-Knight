using System.Collections;
using UnityEngine;

public class PlayerFireBallState : BaseState<PlayerController>
{
    public PlayerFireBallState(PlayerController owner)
    {
        this.owner = owner;
    }
    float t;
    public override void OnEnter()
    {
        owner.SetGravityScale(0f);
        owner.canControl = false;
        owner.rb.linearVelocity = Vector2.zero;
        owner.fireBallPushStarted = false;
        t = owner.fireBallPushTime;

        //CameraImpluseManager.instance.Skill();
    }

    public override void OnExit()
    {
        if (owner.isUsingSkill)
            owner.SkillOver();
        owner.SetGravityScale(owner.gravityscale);
    }

    public override void OnUpdate()
    {
        if (t > 0 && owner.fireBallPushStarted) 
        {
            t -= Time.deltaTime;
            if(owner.facingRight)
            {
                owner.rb.linearVelocity = new Vector2(-owner.fireBallPushForce, 0);
            }
            else
            {
                owner.rb.linearVelocity = new Vector2(owner.fireBallPushForce, 0);
            }
        }
        else
        {
            owner.rb.linearVelocity = Vector2.zero;
        }
        if (!owner.isUsingSkill)
        {
            owner.stateMechine.ChangeState(StateType.idel);
        }
    }
}