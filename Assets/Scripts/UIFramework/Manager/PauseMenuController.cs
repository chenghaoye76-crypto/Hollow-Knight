using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public sealed class PauseMenuController : MonoBehaviour
{
    private PanelManager manager;
    private GameObject canvasObject;
    private GameObject ownedEventSystem;
    private bool paused;
    private float previousScale;
    private bool previousPause;
    private GameObject previousSelection;

    private void Update()
    {
        if (paused) return; // 各页面负责返回，避免同一帧重复处理 Esc。
        var keyboard = Keyboard.current;
        var pad = Gamepad.current;
        if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
            (pad != null && pad.startButton.wasPressedThisFrame)) Open();
    }

    public void Open()
    {
        var health = GetComponent<PlayerHealth>();
        if (paused || Time.timeScale <= 0 || (health != null && health.CurrentHealth <= 0)) return;
        if (canvasObject == null)
        {
            canvasObject = new GameObject("Pause Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var backdrop = new GameObject("Pause Backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)backdrop.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            backdrop.GetComponent<Image>().color = new Color(0, 0, 0, 0.82f);
            manager = new PanelManager(new UIManager(canvasObject.transform));
        }
        canvasObject.SetActive(true);
        if (EventSystem.current == null)
        {
            ownedEventSystem = new GameObject("Pause EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            ownedEventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        previousScale = Time.timeScale;
        previousPause = TimeManager.Instance != null && TimeManager.Instance.pause;
        paused = true;
        if (TimeManager.Instance != null) TimeManager.Instance.pause = true;
        Time.timeScale = 0;
        manager.Push(new EscPanel(manager, Resume));
        if (manager.Current == null) RestoreTime();
    }

    public void Resume()
    {
        if (!paused) return;
        manager.CloseRoot(() =>
        {
            RestoreTime();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousSelection);
        });
    }

    private void RestoreTime()
    {
        if (!paused) return;
        paused = false;
        if (canvasObject != null) canvasObject.SetActive(false);
        Time.timeScale = previousScale;
        if (TimeManager.Instance != null) TimeManager.Instance.pause = previousPause;
    }

    private void OnDisable()
    {
        RestoreTime();
        if (canvasObject != null)
        {
            canvasObject.SetActive(false);
            Destroy(canvasObject);
        }
        if (ownedEventSystem != null) Destroy(ownedEventSystem);
        canvasObject = ownedEventSystem = null;
        manager = null;
    }
}
