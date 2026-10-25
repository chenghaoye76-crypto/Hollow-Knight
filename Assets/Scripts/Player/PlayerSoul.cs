using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerSoul : MonoBehaviour
{
    [SerializeField, Min(33)] private int maximumSoul = 99;
    [SerializeField, Min(0), Tooltip("练习场景默认满灵魂，可改为 0。")] private int startingSoul = 99;
    public int Current { get; private set; }
    public int Maximum => maximumSoul;
    public event Action Changed;
    private void Awake() => Current = Mathf.Clamp(startingSoul, 0, maximumSoul);
    public void Add(int amount)
    {
        if (amount > 0) Set(Current + amount);
    }
    public bool TrySpend(int amount)
    {
        if (amount < 0 || Current < amount) return false;
        Set(Current - amount);
        return true;
    }
    public void Set(int amount)
    {
        int next = Mathf.Clamp(amount, 0, maximumSoul);
        if (next == Current) return;
        Current = next;
        Changed?.Invoke();
    }
}
