using System.Collections;
using UnityEngine;

public class NAchargedEffect : MonoBehaviour
{
    Poolbb poolbb;

    private void Awake()
    {
        poolbb=GetComponent<Poolbb>();
    }

    private void OnEnable()
    {
        EventManager.instance.AddVoidAction("NAChargedOver", gameObject.name, NAChargedOver);
    }

    private void OnDisable()
    {
        EventManager.instance.RemoveVoidAction("NAChargedOver");
    }

    private void NAChargedOver()
    {
        poolbb.End();
    }

}