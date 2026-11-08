using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class frame : MonoBehaviour
{
    //攻击 获取的灵魂 5点，施放法术消耗20点，最大60点，小灵魂槽20点
    Animator animator;

    [SerializeField]
    private List<linnerSoul> linnerSoul;
    [SerializeField]
    private Soul soul;

    int CurrentSoul;
    int LinnerCount;
    private static frame refillingFrame;
    private Coroutine soulRefill;
    private bool waitingToRefill;

    internal static bool IsSoulRefilling => refillingFrame != null;

    private void OnDisable()
    {
        if (soulRefill != null)
            StopCoroutine(soulRefill);

        soulRefill = null;
        waitingToRefill = false;
        if (refillingFrame == this)
            refillingFrame = null;
    }
    private void OnEnable()
    {
        animator = GetComponent<Animator>();
    }
    public void Init(int CurrentSoul, int linnerCount)
    {
        soul.Init(this);
        this.CurrentSoul = CurrentSoul;
        LinnerCount = linnerCount;
    }

    public void Appear()
    {
        animator.SetTrigger("appear");
        StartCoroutine(appear());
    }

    IEnumerator appear()
    {
        yield return new WaitForSeconds(0.3f);
        for (int i = 0; i < LinnerCount; i++)
        {

            linnerSoul[i].Appear();
            yield return new WaitForSeconds(0.4f);
        }

        IncreaseSoul(CurrentSoul);
    }
    public void IncreaseSoul(int SoulValue)
    {
        CurrentSoul = SoulValue;
        if (waitingToRefill)
            return;

        if (CurrentSoul <= 99)
        {
            soul.UpdateSoul(SoulValue);

            for (int i = 0; i < LinnerCount; i++)
            {
                linnerSoul[i].SetValue(0);
            }
        }
        else
        {
            soul.UpdateSoul(99);

            int LinnerSoulValue = CurrentSoul - 99;
            int LinnerSoulCount = Mathf.FloorToInt(LinnerSoulValue / 8);

            for (int i = 0; i < LinnerCount; i++)
            {
                if (LinnerSoulCount == 0)
                    linnerSoul[i].SetValue(0);
                if (LinnerSoulCount <= 4)
                {
                    linnerSoul[i].SetValue(LinnerSoulCount);
                    LinnerSoulCount = 0;
                }
                else
                {
                    linnerSoul[i].SetValue(4);
                    LinnerSoulCount -= 4;
                }
            }
        }
    }

    public void DecreaseSoul(int SoulValue)
    {
        int PreSoul = CurrentSoul;
        CurrentSoul = SoulValue;

        int dec = PreSoul - CurrentSoul;
        if (dec > 0)
            soul.PlayDrainWave();


        if (dec > 0 && PreSoul > 99)
        {
            soul.UpdateSoul(Mathf.Max(0, 99 - dec));
            refillingFrame = this;
            waitingToRefill = true;
            soulRefill = StartCoroutine(RefillSoul());
        }
        else
        {
            IncreaseSoul(CurrentSoul);
        }
    }

    private IEnumerator RefillSoul()
    {
        yield return new WaitForSeconds(0.5f);
        waitingToRefill = false;
        IncreaseSoul(CurrentSoul);

        while (soul.DisplayedSoul != Mathf.Min(CurrentSoul, 99))
            yield return null;

        if (refillingFrame == this)
            refillingFrame = null;
        soulRefill = null;
    }

    public void SoulBurst()
    {
        EventManager.instance.VoidEventInvoke("SoulBurst");
    }
}
