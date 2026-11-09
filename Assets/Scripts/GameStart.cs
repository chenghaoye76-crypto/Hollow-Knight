using System.Collections;
using UnityEngine;

public class GameStart : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("GameStart");
        StartCoroutine(start());
    }
    IEnumerator start()
    {
        //刷新UI面板
        Debug.Log("GameStart");
        UIManager.Instant.ClearPanel();
        UIManager.Instant.PushPanel(UIPanelType.GamePanel);
        Debug.Log("GameStart");
        EventManager.instance.VoidEventInvoke("GameStart");
        yield return new WaitForSeconds(0.1f);

    }
}