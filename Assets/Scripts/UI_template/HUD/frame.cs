using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class frame : MonoBehaviour
{
    private const int MainCapacity = 99;
    private const int VesselCapacity = 33;
    private const float RefillDelay = 0.75f;
    private const float VesselTransferDuration = 0.25f;
    Animator animator;

    [SerializeField]
    private List<linnerSoul> linnerSoul;
    [SerializeField]
    private Soul soul;

    private int mainSoul;
    private int reserveSoul;
    private float refillAt = float.PositiveInfinity;
    int LinnerCount;
    private void OnEnable()
    {
        animator = GetComponent<Animator>();
    }
    public void Init(int CurrentSoul, int linnerCount)
    {
        soul.Init(this);
        mainSoul = Mathf.Min(CurrentSoul, MainCapacity);
        reserveSoul = CurrentSoul - mainSoul;
        refillAt = float.PositiveInfinity;
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

        UpdateSoulDisplay();
    }

    public void IncreaseSoul(int soulValue)
    {
        int gain = soulValue - (mainSoul + reserveSoul);
        if (gain <= 0)
            return;

        int addedToMain = Mathf.Min(gain, MainCapacity - mainSoul);
        mainSoul += addedToMain;
        reserveSoul += gain - addedToMain;

        if (mainSoul == MainCapacity)
            refillAt = float.PositiveInfinity;

        UpdateSoulDisplay();
    }

    public void DecreaseSoul(int soulValue)
    {
        int spent = mainSoul + reserveSoul - soulValue;
        if (spent <= 0)
            return;

        soul.PlayDrainWave();
        int spentFromMain = Mathf.Min(spent, mainSoul);
        mainSoul -= spentFromMain;
        reserveSoul -= spent - spentFromMain;

        refillAt = reserveSoul > 0 ? Time.time + RefillDelay : float.PositiveInfinity;
        UpdateSoulDisplay();
    }

    private void Update()
    {
        if (Time.time < refillAt)
            return;

        int topVesselSoul = (reserveSoul - 1) % VesselCapacity + 1;
        int transferred = Mathf.Min(topVesselSoul, MainCapacity - mainSoul);
        mainSoul += transferred;
        reserveSoul -= transferred;
        refillAt = reserveSoul > 0 && mainSoul < MainCapacity
            ? Time.time + VesselTransferDuration
            : float.PositiveInfinity;
        UpdateSoulDisplay(VesselTransferDuration / transferred);
    }

    private void UpdateSoulDisplay(float? refillStepTime = null)
    {
        soul.UpdateSoul(mainSoul, refillStepTime);

        // The prefab lists the vessels from the lowest position to the highest.
        for (int i = 0; i < LinnerCount; i++)
        {
            int amount = Mathf.Clamp(reserveSoul - i * VesselCapacity, 0, VesselCapacity);
            int quarters = amount == VesselCapacity ? 4 : Mathf.Min(amount / 8, 3);
            linnerSoul[i].SetValue(quarters);
        }
    }

    public void SoulBurst()
    {
        EventManager.instance.VoidEventInvoke("SoulBurst");
    }
}
