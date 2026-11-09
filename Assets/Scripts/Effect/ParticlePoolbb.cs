using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlePoolbb : Poolbb
{
    public float ReturnTime;

    private float timer;

    private void OnEnable()
    {
        timer = ReturnTime;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer < 0)
            base.End();
    }

}
