using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class foucusEffect : bb
{
    Poolbb poolbb;
    private void Awake()
    {
        poolbb = GetComponent<Poolbb>();
    }

    private void OnEnable()
    {
        EventManager.instance.AddVoidAction("foucusOver", name, FoucusOver);
    }

    private void OnDisable()
    {
        EventManager.instance.RemoveVoidEvent("foucusOver");
    }

    public void FoucusOver()
    {
        poolbb.End();
    }

}
