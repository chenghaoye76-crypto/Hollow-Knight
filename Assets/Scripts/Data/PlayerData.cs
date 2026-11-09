using System.Collections.Generic;
using UnityEngine;

public class Data
{
    //玩家数据
    public PlayerData playerData;
    //背包数据
    public PackageData packageData;
    //存档点
    public SavePoint savePoint;
    //游戏时间
    public float gameTime;

    public Dictionary<string, bool> boolData = new Dictionary<string, bool>();
    public Dictionary<string, int> intData = new Dictionary<string, int>();

    public Data()
    {
        playerData = new PlayerData();
        packageData = new PackageData();
        savePoint = new SavePoint();
        gameTime = 0;
    }
}

public class SavePoint
{
    public bool isGet;
    public bool isChairpoint;
    public GameSceneType gameScene;
    public SerializeVector3 position;

    public SavePoint()
    {
        isGet = false;
        gameScene = GameSceneType.dirtmouth;
    }

    public SavePoint(bool isChairpoint, Vector3 pos, GameSceneType gameSceneType)
    {
        isGet = true;
        this.isChairpoint = isChairpoint;
        gameScene = gameSceneType;
        position = new SerializeVector3(pos);
    }
}

public class PlayerData
{
    [Newtonsoft.Json.JsonIgnore]
    public int MainSoul { get; internal set; }
    public PlayerData()
    {
        CurrentHealth = 9;
        MaxHealth = 9;
        LinnerCount = 3;
        CurrentSoul = 0;
        geo = 114514;
    }

    public int MaxHealth;
    public int CurrentHealth;
    public int LinnerCount;
    public int CurrentSoul;
    public int geo;
}

public class PackageData
{
    public List<PackageGoodType> packageGoodTypes;
    public List<PackageTalismanType> packageTalismanTypes;
    public List<PackageTalismanType> equippedItemTypes;

    public int grooveNum;
    public int boneNailGrade;
    public int MaskShardGrade;

    public PackageData()
    {
        packageGoodTypes = new List<PackageGoodType>();
        equippedItemTypes = new List<PackageTalismanType>();
        packageTalismanTypes = new List<PackageTalismanType>();
        grooveNum = 5;
        boneNailGrade = 1;
        MaskShardGrade = 0;
    }

    public void AddGood(PackageGoodType item)
    {
        foreach (PackageGoodType itemType in packageGoodTypes)
        {
            if (itemType == item)
                return;
        }
        packageGoodTypes.Add(item);
    }

    public void AddTalisman(PackageTalismanType item)
    {
        foreach (PackageTalismanType itemType in packageTalismanTypes)
        {
            if (itemType == item)
                return;
        }
        packageTalismanTypes.Add(item);
    }

    public PlayerSkill GetSkill()
    {
        PlayerSkill skill = new PlayerSkill();
        foreach (PackageGoodType type in packageGoodTypes)
        {
            switch (type)
            {
                case PackageGoodType.Mantis_Claw:
                    skill.Climb = true;
                    break;
                case PackageGoodType.Crystal_Heart:
                    skill.SuperDash = true;
                    break;
                case PackageGoodType.Dash_Slash:
                    skill.DashSlash = true;
                    break;
                case PackageGoodType.Great_Slash:
                    skill.BigSlash = true;
                    break;
                case PackageGoodType.Cyclone_Slash:
                    skill.CycloneSlash = true;
                    break;
                case PackageGoodType.Vengeful_Spirit:
                    skill.FireBall = true;
                    break;
                case PackageGoodType.Shade_Soul:
                    skill.ShadeFireBall = true;
                    break;
                case PackageGoodType.Desolate_Dive:
                    skill.Quake = true;
                    break;
                case PackageGoodType.Descending_Dark:
                    skill.ShadeQuake = true;
                    break;
                case PackageGoodType.Howling_Wraiths:
                    skill.Scream = true;
                    break;
                case PackageGoodType.Abyss_Shriek:
                    skill.ShadeScream = true;
                    break;
                case PackageGoodType.Monarch_Wings:
                    skill.DoubleJump = true;
                    break;
                case PackageGoodType.Mothwing_Cloak:
                    skill.NormalDash = true;
                    break;
                case PackageGoodType.Shade_Cloak:
                    skill.ShadeDash = true;
                    break;
            }
        }
        return skill;
    }
}

public class SerializeVector3
{
    public float x, y, z;

    public SerializeVector3(Vector3 pos)
    {
        this.x = pos.x;
        this.y = pos.y;
        this.z = pos.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

public class PlayerSkill
{
    public bool BigSlash;
    public bool CycloneSlash;
    public bool DashSlash;
    public bool Scream;
    public bool FireBall;
    public bool Quake;
    public bool ShadeScream;
    public bool ShadeFireBall;
    public bool ShadeQuake;
    public bool NormalDash;
    public bool ShadeDash;
    public bool Climb;
    public bool DoubleJump;
    public bool SuperDash;
}
