using System.Collections;
using UnityEngine;


public enum StateType
{
    //Player
    idel, move, jump, douleJump, upPush, hurt, spikeHurt, death, dash, superDash, climb, fireBall, scream, quake, foucus, bigSlash, cycloneSlash, dashSlash, sit, blank,
    //crawler
    crawlerWalk, crawlerDead,
    //huskWarrior
    huskWarriorIdle, huskWarriorWalk, huskWarriorRun, huskWarriorDeath, huskWarriorShield,
}


public abstract class BaseState<T>
{
    public T owner;
    public abstract void OnEnter();
    public abstract void OnUpdate();
    public abstract void OnExit();
}