using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class healthes : MonoBehaviour
{
    [SerializeField] GameObject health;
    List<health> healthList = new List<health>();
    int maxHealth;
    int currentHealth;

    public void _Start(int maxhealth)
    {
        maxHealth = maxhealth;
        currentHealth = maxhealth;
        for (int i = 0; i < maxhealth; i++)
        {
            GameObject obj = GameObject.Instantiate(health);
            obj.transform.SetParent(transform, false);
            healthList.Add(obj.GetComponent<health>());
        }
    }

    public void Appear()
    {
        StartCoroutine(appear());
    }

    IEnumerator appear()
    {
        for (int i = 0; i < maxHealth; i++)
        {
            healthList[i].Appear();
            yield return new WaitForSeconds(0.3f);
        }
        EventManager.instance.VoidEventInvoke("canPause");
    }

    public void Refill()
    {
        if (currentHealth < maxHealth)
        {
            healthList[currentHealth].Refill();
            currentHealth++;
        }
    }

    public void Break()
    {
        if (currentHealth >= 1)
        {
            currentHealth--;
            healthList[currentHealth].Break();
        }
    }

    public void Recover()
    {
        while (currentHealth < maxHealth)
            Refill();
    }
}
