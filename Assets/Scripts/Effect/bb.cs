using System.Collections;
using UnityEngine;

public class bb : MonoBehaviour
{
    public virtual void destroy()
    {
        GameObject.Destroy(gameObject);
    }
}