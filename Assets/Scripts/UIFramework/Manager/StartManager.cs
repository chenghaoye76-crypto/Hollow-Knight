using UnityEngine;

public class StartManager : MonoBehaviour
{
    [SerializeField] private Transform panelRoot;
    [SerializeField] private Transform backgroundRoot;

    private PanelManager panelManager;

    private void Start()
    {
        // A designer's scene preview must not overlap the managed runtime panel.
        Transform optionsPreview = panelRoot != null ? panelRoot.Find("OptionsPanel") : null;
        if (optionsPreview != null) optionsPreview.gameObject.SetActive(false);
        LoadBackground();

        var uiManger = new UIManager(panelRoot);
        panelManager = new PanelManager(uiManger);

        panelManager.Push(new MainMenuPanel(panelManager));
    }

    private void LoadBackground()
    {
        if (backgroundRoot == null)
        {
            Debug.Log("请给 Background Root 指定 BGCanvas");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/Panel/VoidHeartBG");

        if(prefab == null)
        {
            Debug.Log("找不到VoidHeatBG预制体");
            return;
        }

        GameObject background = Instantiate(prefab, backgroundRoot, false);
        // 背景不接收点击。
        foreach (var graphic in background.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) 
        {
            graphic.raycastTarget = false;
        }
        var fade = background.GetComponent<PanelFade>();
        if (fade == null)
            fade = background.AddComponent<PanelFade>();

        fade.Show(0.8f);

        // 背景只显示，不参与交互。
        background.GetComponent<CanvasGroup>().blocksRaycasts = false;
    }
}
