using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class focusEffect : bb
{
    Poolbb poolbb;
    private void Awake()
    {
        poolbb = GetComponent<Poolbb>();
    }

    private void OnEnable()
    {
        EventManager.instance.AddVoidAction("focusOver", name, FoucusOver);
    }

    private void OnDisable()
    {
        EventManager.instance.RemoveVoidEvent("focusOver");
    }

    public void FoucusOver()
    {
        poolbb.End();
    }

}
