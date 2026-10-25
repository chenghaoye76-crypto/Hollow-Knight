using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildPersistant : MonoBehaviour
{
    private Vector3 position;

    private void Start()
    {
        position = transform.position;
    }

    private void Update()
    {
        transform.position = position;
    }
}
