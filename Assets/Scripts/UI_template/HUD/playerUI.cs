using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class playerUI : MonoBehaviour
{
    [HideInInspector] public CanvasGroup canvasGroup;
    [SerializeField] frame frame;
    [SerializeField] healthes healthes;
    [SerializeField] Animator geo;

    TextMeshProUGUI geoNum;
    PlayerData playerData;

    private void OnEnable()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        geoNum = geo.GetComponentInChildren<TextMeshProUGUI>();
        geoNum.alpha = 0;

        EventManager.instance.AddIntAction("healthDecrease", name, HealthDecrease);
        EventManager.instance.AddIntAction("foucusGet", name, HealthIncrease);
        EventManager.instance.AddVoidAction("healthRecover", name, HealthRecover);

        EventManager.instance.AddIntAction("payGeo", name, PayGeo);
        EventManager.instance.AddIntAction("getGeo", name, GetGeo);

        EventManager.instance.AddIntAction("soulDecrease", name, SoulDecrease);
        EventManager.instance.AddIntAction("soulIncrease", name, SoulIncrease);
    }

    private void OnDisable()
    {
        EventManager.instance.RemoveIntAction("healthDecrease", name);
        EventManager.instance.RemoveIntAction("foucusGet", name);
        EventManager.instance.RemoveVoidAction("healthRecover", name);

        EventManager.instance.RemoveIntAction("payGeo", name);
        EventManager.instance.RemoveIntAction("getGeo", name);

        EventManager.instance.RemoveIntAction("soulDecrease", name);
        EventManager.instance.RemoveIntAction("soulIncrease", name);
    }

    public void Show()
    {
        StartCoroutine(show());
    }

    IEnumerator show()
    {
        playerData = GameDataManager.instant.data.playerData;
        playerData.CurrentHealth = playerData.MaxHealth;
        geoNum.text = playerData.geo.ToString();

        healthes._Start(playerData.MaxHealth);
        frame.Init(playerData);
        yield return new WaitForSeconds(0.2f);

        frame.Appear();
        yield return new WaitForSeconds(0.1f);
        healthes.Appear();
        geo.SetTrigger("enter");
        while (geoNum.alpha < 1)
        {
            yield return null;
            geoNum.alpha += Time.deltaTime;
        }
    }

    #region Health
    public void HealthDecrease(int damage)
    {
        playerData.CurrentHealth -= damage;
        for (int i = 0; i < damage; i++)
            healthes.Break();
    }

    public void HealthIncrease(int damage)
    {
        playerData.CurrentHealth += damage;
        for (int i = 0; i < damage; i++)
            healthes.Refill();
    }

    public void HealthRecover()
    {
        playerData.CurrentHealth = playerData.MaxHealth;
        healthes.Recover();
    }
    #endregion

    #region Soul
    public void SoulDecrease(int damage)
    {
        if (playerData.CurrentSoul - damage < 0)
            playerData.CurrentSoul = 0;
        else
            playerData.CurrentSoul -= damage;
        frame.DecreaseSoul(playerData.CurrentSoul);
    }

    public void SoulIncrease(int damage)
    {
        Debug.Log("SoulIncrease" + damage);
        if (playerData.CurrentSoul + damage > 99 + playerData.LinnerCount * 33)
            playerData.CurrentSoul = 99 + playerData.LinnerCount * 33;
        else
            playerData.CurrentSoul += damage;
        frame.IncreaseSoul(playerData.CurrentSoul);
    }
    #endregion

    #region Geo
    public void GetGeo(int n)
    {
        StartCoroutine(getgeo(n));
    }

    IEnumerator getgeo(int n)
    {
        geo.SetTrigger("get");
        for (int i = 1; i <= n; i++)
        {
            playerData.geo++;
            geoNum.text = playerData.geo.ToString();
            yield return new WaitForSeconds(0.05f);
        }
    }

    public void PayGeo(int n)
    {
        StartCoroutine(paygeo(n));
    }

    IEnumerator paygeo(int n)
    {
        for (int i = 1; i <= n; i++)
        {
            playerData.geo--;
            geoNum.text = playerData.geo.ToString();
            yield return new WaitForSeconds(0.05f);
        }
    }
    #endregion
}
