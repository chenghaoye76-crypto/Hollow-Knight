using System.Collections;
using UnityEngine;

public class NAcharge : MonoBehaviour
{
    Poolbb poolbb;


    private void Awake()
    {
        poolbb = GetComponent<Poolbb>();
    }

    private void OnEnable()
    {
        EventManager.instance.AddVoidAction("NAChargeOver", name, NAChargeOver);
    }


    private void NAChargeOver()
    {
        poolbb.End();
    }


    private void OnDisable()
    {
        EventManager.instance.RemoveVoidAction("NAChargeOver");
    }





}