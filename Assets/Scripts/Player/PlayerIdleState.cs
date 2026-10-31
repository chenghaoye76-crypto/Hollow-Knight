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
    }

    public override void OnExit()
    {

    }

    public override void OnUpdate()
    {

    }
}