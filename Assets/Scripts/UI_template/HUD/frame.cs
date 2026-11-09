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
    private int mainSoul;
    private Coroutine pendingRefill;
    private Coroutine gainAnimation;
    private int queuedMainGain;
    private bool reserveFillDeferred;
    private PlayerData playerData;
    int CurrentSoul;
    int LinnerCount;
    private void OnEnable()
    {
        animator = GetComponent<Animator>();
    }
    public void Init(PlayerData data)
    {
        StopGainAnimation();
        queuedMainGain = 0;
        reserveFillDeferred = false;
        playerData = data;
        CurrentSoul = data.CurrentSoul;
        LinnerCount = data.LinnerCount;
        data.MainSoul = Mathf.Min(CurrentSoul, 99);
        soul.Init(this);
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
    public void IncreaseSoul(int soulValue)
    {
        int gained = Mathf.Max(0, soulValue - CurrentSoul);
        if (gained == 0 && pendingRefill != null)
            return;

        CurrentSoul = soulValue;
        int availableSpace = 99 - playerData.MainSoul - queuedMainGain;
        queuedMainGain += Mathf.Min(gained, availableSpace);
        UpdateReserveDisplay();

        if (gainAnimation == null && (queuedMainGain > 0 || pendingRefill == null))
        {
            soul.UpdateSoul(playerData.MainSoul);
            if (queuedMainGain > 0)
                gainAnimation = StartCoroutine(AnimateSoulGain());
        }
    }

    private IEnumerator AnimateSoulGain()
    {
        while (queuedMainGain > 0)
        {
            while (soul.DisplayedSoul != playerData.MainSoul)
                yield return null;

            int target = playerData.MainSoul + queuedMainGain;
            soul.UpdateSoul(target);

            while (playerData.MainSoul < target)
            {
                yield return null;
                int displayed = Mathf.Min(target, soul.DisplayedSoul);
                int gained = Mathf.Max(0, displayed - playerData.MainSoul);
                playerData.MainSoul += gained;
                queuedMainGain -= gained;
            }
        }

        gainAnimation = null;
    }

    private void StopGainAnimation()
    {
        if (gainAnimation == null)
            return;

        StopCoroutine(gainAnimation);
        gainAnimation = null;
    }

    public void DecreaseSoul(int soulValue)
    {
        int spent = CurrentSoul - soulValue;
        if (spent <= 0)
            return;

        CurrentSoul = soulValue;
        playerData.MainSoul = Mathf.Max(0, playerData.MainSoul - spent);

        StopGainAnimation();
        soul.PlayDrainWave();
        UpdateSoulDisplay();
        if (queuedMainGain > 0)
            gainAnimation = StartCoroutine(AnimateSoulGain());

        if (pendingRefill != null)
        {
            StopCoroutine(pendingRefill);
            pendingRefill = null;
        }

        if (CurrentSoul - queuedMainGain > playerData.MainSoul)
            pendingRefill = StartCoroutine(RefillSoul());
    }

    private IEnumerator RefillSoul()
    {
        yield return new WaitForSeconds(1f);

        while (playerData.MainSoul < 99 && CurrentSoul - queuedMainGain > playerData.MainSoul)
        {
            if (gainAnimation != null || soul.DisplayedSoul != playerData.MainSoul)
            {
                yield return null;
                continue;
            }

            int previousSoul = playerData.MainSoul;
            int reserveSoul = CurrentSoul - previousSoul;
            int topVesselSoul = (reserveSoul - 1) % 33 + 1;
            int transferred = Mathf.Min(Soul.RefillPointsPerStep, topVesselSoul, 99 - previousSoul);
            int nextSoul = previousSoul + transferred;
            soul.UpdateSoul(nextSoul, isRefill: true);

            while (gainAnimation == null && playerData.MainSoul == previousSoul && soul.DisplayedSoul != nextSoul)
                yield return null;

            if (gainAnimation == null && playerData.MainSoul == previousSoul)
            {
                playerData.MainSoul = nextSoul;
                UpdateReserveDisplay((reserveSoul - 1) / 33);
            }
        }

        pendingRefill = null;
    }
    private void UpdateSoulDisplay()
    {
        soul.UpdateSoul(playerData.MainSoul);
        UpdateReserveDisplay();
    }

    private void LateUpdate()
    {
        if (reserveFillDeferred && soul.DisplayedSoul == 99 && playerData.MainSoul == 99)
            UpdateReserveDisplay();
    }

    private void UpdateReserveDisplay(int drainingVessel = -1)
    {
        bool canFillVessel = soul.DisplayedSoul == 99 && playerData.MainSoul == 99;
        reserveFillDeferred = false;
        int reserveSoul = Mathf.Max(0, CurrentSoul - playerData.MainSoul - queuedMainGain);
        for (int i = 0; i < LinnerCount; i++)
        {
            int amount = Mathf.Clamp(reserveSoul - i * 33, 0, 33);
            int quarters = amount == 33 ? 4 : Mathf.Min(amount / 8, 3);
            linnerSoul vessel = linnerSoul[i];
            if (i == drainingVessel)
            {
                vessel.SetRefillAmount(amount, quarters);
                canFillVessel = false;
                continue;
            }

            int displayedTarget = Mathf.Min(vessel.PosValue, vessel.CurrentValue);
            if (!canFillVessel && quarters > displayedTarget)
            {
                reserveFillDeferred = true;
                quarters = displayedTarget;
            }

            vessel.SetValue(quarters);
            canFillVessel = canFillVessel && vessel.IsVisuallyFull;
        }
    }
    public void SoulBurst()
    {
        EventManager.instance.VoidEventInvoke("SoulBurst");
    }
}
