using System.Collections;
using UnityEngine;

public class PlayerFoucusState : BaseState<PlayerController>
{
    public PlayerFoucusState(PlayerController owner)
    {
        this.owner = owner;
    }
    public override void OnEnter()
    {
        owner.canControl = false;
        owner.rb.linearVelocity = Vector2.zero;

        //CinemachineManager.Instance.Focus();
    }

    public override void OnExit()
    {
        owner.isFocus = false;
        owner.canFocus = false;
        owner.canMove = true;
        EventManager.instance.VoidEventInvoke("focusOver");
        //owner.audioManager.StopSound();

        //CinemachineManager.Instance.FocusOver(); 
    }

    public override void OnUpdate()
    {
        if (!owner.skillPress /*|| owner.character.Attribute.CurrentSoul < 30*/)
            owner.isFocus = false;
        if (owner.canFocus && owner.foucusTimer > 0)
        {
            owner.foucusTimer -= Time.deltaTime;
        }
        else if (owner.canFocus && owner.foucusTimer < 0)
        {
            owner.anim.SetTrigger("focusGet");
            owner.canFocus = false;
            //owner.audioManager.FocusGet();
            owner.effectManager.BrustEffect();
            //EventManager.instance.IntEventInvoke("focusGet", 1);
            //EventManager.instance.IntEventInvoke("soulDecrease", 30);
            EventManager.instance.VoidEventInvoke("focusOver");
        }

        if (!owner.isFocus)
        {
            if (owner.moveInput.x != 0)
                owner.stateMechine.ChangeState(StateType.move);
            else
                owner.stateMechine.ChangeState(StateType.idel);
        }
    }
}