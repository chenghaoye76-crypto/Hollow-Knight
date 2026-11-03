using System.Collections;
using UnityEngine;

public class PlayerScreamState : BaseState<PlayerController>
{
    public PlayerScreamState(PlayerController owner)
    {
        this.owner = owner;
    }
    public override void OnEnter()
    {
        owner.SetGravityScale(0f);
        owner.canControl = false;
        owner.rb.linearVelocity = Vector2.zero;

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
        if(!owner.isUsingSkill)
        {
            owner.stateMechine.ChangeState(StateType.idel);
        }
    }
}