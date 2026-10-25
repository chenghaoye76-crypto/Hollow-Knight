using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.PlayerLoop;

public class MaskCanvas : MonoBehaviour
{
    public static MaskCanvas Instance;
    [HideInInspector] public CanvasGroup maskPanel;
    [HideInInspector] public LoadingIcon loadingIcon;
    bool isShow;
    float timer;

    private void Awake()
    {
        if (Instance == null)
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        isShow = true;
    }
    private void Update()
    {
        if (isShow)
        {
            if (timer > 0)
                timer -= TimeManager.Instance.deltaTime;
            else if (timer < 0 && timer > -1)
            {
                loadingIcon.Show();
                timer = -1.5f;
            }
        }
    }

    private void OnEnable()
    {
        maskPanel=GetComponent<CanvasGroup>();
        loadingIcon = GetComponentInChildren<LoadingIcon>();
    }

    #region Show
    public void Show(UnityAction finish)
    {
        StopAllCoroutines();
        maskPanel.blocksRaycasts = true;
        StartCoroutine(show(finish));
    }
    IEnumerator show(UnityAction finish)
    {
        //MenuAudioManager.Instant.MasterUnshow();
        while (maskPanel.alpha < 1f)
        {
            yield return null;
            //maskPanel.alpha += TimeManager.Instant.deltaTime * 1.5f;
        }
        isShow = true;
        timer = 1f;
        finish();
    }
    #endregion

    #region UnShow
    public void UnShow()
    {
        StopAllCoroutines();
        StartCoroutine(unShow());
        isShow = false;
    }
    IEnumerator unShow()
    {
        //MenuAudioManager.Instant.MasterShow();
        yield return new FixedUpdate();
        loadingIcon.UnShow();
        while (maskPanel.alpha > 1f)
        {
            yield return null;
            //maskPanel.alpha -= TimeManager.Instant.deltaTime * 1f;
        }
        maskPanel.blocksRaycasts = false;
    }
    #endregion
}