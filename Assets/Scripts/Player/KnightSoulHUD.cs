using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class KnightSoulHUD : MonoBehaviour
{
    private PlayerSoul soul;
    private TMP_Text counter;
    private void Start()
    {
        soul = GetComponent<PlayerSoul>();
        if (soul == null) return;
        RectTransform orb = null;
        foreach (var rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (rect.gameObject.scene == gameObject.scene && rect.name == "SoulOrb") { orb = rect; break; }
        if (orb == null) return;
        var child = new GameObject("Soul amount", typeof(RectTransform));
        child.transform.SetParent(orb, false);
        var position = child.GetComponent<RectTransform>();
        position.anchorMin = position.anchorMax = new Vector2(.5f, 0);
        position.anchoredPosition = new Vector2(0, -12); position.sizeDelta = new Vector2(160, 32);
        counter = child.AddComponent<TextMeshProUGUI>();
        counter.font = TMP_Settings.defaultFontAsset;
        counter.fontSize = 24; counter.color = new Color(.84f,.93f,1);
        counter.alignment = TextAlignmentOptions.Center; counter.raycastTarget = false;
        soul.Changed += Refresh; Refresh();
    }
    private void Refresh() { if (counter != null) counter.text = soul.Current + " / " + soul.Maximum; }
    private void OnDestroy()
    {
        if (soul != null) soul.Changed -= Refresh;
        if (counter != null) Destroy(counter.gameObject);
    }
}
