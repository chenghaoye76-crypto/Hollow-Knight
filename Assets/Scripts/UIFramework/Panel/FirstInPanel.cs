using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
/// <summary>
/// 进入场景的淡入淡出
/// </summary>
public class FirstInPanel : MonoBehaviour
{
    CanvasGroup canvasGroup;
    [SerializeField] private LoadingIcon loadingIcon;
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (loadingIcon == null)
            loadingIcon = GetComponentInChildren<LoadingIcon>(true);
    }
    private void Start()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = true;
        StartCoroutine(enter());
    }

    IEnumerator enter()
    {
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
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        if (loadingIcon != null)
            loadingIcon.Show();
        yield return new WaitForSeconds(2.5f);
        if (loadingIcon != null)
            loadingIcon.UnShow();
        SceneManager.LoadScene("MainMenu");
    }
}
