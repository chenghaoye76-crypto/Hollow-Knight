using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class MaskCanvas : MonoBehaviour
{
    public static MaskCanvas Instance;
    [HideInInspector] public CanvasGroup maskPanel;
    [HideInInspector] public LoadingIcon loadingIcon;
    bool isShow;
    float timer;
    CanvasGroup inputGate;

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
            return;
        }
        // The fade group lives on MaskPanel, not on the Canvas root.
        maskPanel = GetComponentInChildren<CanvasGroup>(true);
        loadingIcon = GetComponentInChildren<LoadingIcon>(true);
        inputGate = GetComponent<CanvasGroup>();
        if (inputGate == null)
            inputGate = gameObject.AddComponent<CanvasGroup>();
        inputGate.interactable = false;
        inputGate.blocksRaycasts = false;
        isShow = false;
    }
    private void Update()
    {
        if (isShow)
        {
            if (timer > 0)
                timer -= Time.unscaledDeltaTime;
            else if (timer < 0 && timer > -1)
            {
                if (loadingIcon != null) loadingIcon.Show();
                timer = -1.5f;
            }
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // FirstInPanel owns the intro animation. Later scenes release the overlay.
        if (Instance == this && scene.name != "FirstIn") UnShow();
    }

    #region Show
    public void Show(UnityAction finish)
    {
        StopAllCoroutines();
        inputGate.blocksRaycasts = true;
        if (maskPanel == null)
        {
            inputGate.blocksRaycasts = false;
            finish?.Invoke();
            return;
        }
        maskPanel.blocksRaycasts = true;
        StartCoroutine(show(finish));
    }
    IEnumerator show(UnityAction finish)
    {
        //MenuAudioManager.Instant.MasterUnshow();
        while (maskPanel.alpha < 1f)
        {
            yield return null;
            maskPanel.alpha = Mathf.MoveTowards(maskPanel.alpha, 1f, Time.unscaledDeltaTime * 1.5f);
        }
        isShow = true;
        timer = 1f;
        finish?.Invoke();
    }
    #endregion

    #region UnShow
    public void UnShow()
    {
        StopAllCoroutines();
        isShow = false;
        inputGate.blocksRaycasts = false;
        if (maskPanel == null) return;
        maskPanel.blocksRaycasts = false;
        StartCoroutine(unShow());
    }
    IEnumerator unShow()
    {
        //MenuAudioManager.Instant.MasterShow();
        if (loadingIcon != null) loadingIcon.UnShow();
        while (maskPanel.alpha > 0f)
        {
            yield return null;
            maskPanel.alpha = Mathf.MoveTowards(maskPanel.alpha, 0f, Time.unscaledDeltaTime);
        }
        maskPanel.blocksRaycasts = false;
    }
    #endregion
}
