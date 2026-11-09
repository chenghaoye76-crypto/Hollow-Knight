using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;


public class Character : MonoBehaviour
{
    private PlayerData playerData;
    private PlayerSkill skill;

    public void Init()
    {
        playerData = GameDataManager.instant.data.playerData;
        skill = GameDataManager.instant.data.packageData.GetSkill();
    }

    public PlayerData Attribute { get { return playerData; } }
    public PlayerSkill Skill { get { return skill; } }
}