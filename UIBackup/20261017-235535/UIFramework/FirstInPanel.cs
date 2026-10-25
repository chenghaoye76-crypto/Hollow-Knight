using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
/// <summary>
/// 进入场景的淡入淡出
/// </summary>
public class FirstInPanel : MonoBehaviour
{
    CanvasGroup canvasGroup;
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }
    private void Start()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = true;
        StartCoroutine(enter());
    }

    IEnumerator enter()
    {
        Debug.Log("enter");
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(2.5f);
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(2.5f);
    }
}
