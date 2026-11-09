using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Poolbb : MonoBehaviour
{
    public ObjectPool pool;

    private void OnParticleSystemStopped()
    {
        if (pool != null)
            End();
    }

    public virtual void End()
    {
        pool.ReturnGameObject(gameObject);
    }

}
