using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlashGhostEffect : MonoBehaviour
{
    [Header("发射角度参数")]
    public float ShotAngle;
    public float AngleOffset;
    [Header("发射位置")]
    public float Radius;
    [Header("发射数量")]
    public int MaxNum;
    public int MinNum;
    [Header("发射速度")]
    public float ShotSpeedMax;
    public float ShotSpeedMin;
    [Header("发射大小")]
    public float MaxSize;
    public float MinSize;
    [Header("预制体")]
    public GameObject slashGhost;

    public void Play()
    {
        int num = Random.Range(MinNum, MaxNum + 1);
        for (int i = 0; i < num; i++)
        {
            float rotationZ = AngleOffset + Random.Range(-ShotAngle, ShotAngle);
            float radians = rotationZ * Mathf.Deg2Rad;
            Vector3 postion = transform.position + new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0) * Radius;

            SlashGhost slashGhost = CreatGhost();
            slashGhost.transform.position = postion;
            slashGhost.transform.localScale = Vector3.one * Random.Range(MinSize, MaxSize);
            slashGhost.Motion(Random.Range(ShotSpeedMin, ShotSpeedMax), rotationZ);
        }
    }

    private SlashGhost CreatGhost()
    {
        return EffectManager.instance.CreateEffect(slashGhost).GetComponent<SlashGhost>();
    }
}
