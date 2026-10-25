using System.Collections;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    public Animator[] healthItem;
    public Animator geo;
    private int nextHurtIndex;
    private RectTransform[] hudRects;
    private Vector3[] hudScales;
    private Vector2[] hudPositions;
    private PlayerHealth health;
    private bool[] revealed;
    private bool hidden;
    private void Start()
    {
        // This script lives on the player, so resize only HUD elements, never its transform.
        hudRects = new RectTransform[healthItem.Length + 1];
        hudScales = new Vector3[hudRects.Length];
        hudPositions = new Vector2[hudRects.Length];
        for (int i = 0; i < hudRects.Length; i++)
        {
            Animator item = i < healthItem.Length ? healthItem[i] : geo;
            if (item == null) continue;
            hudRects[i] = item.GetComponent<RectTransform>();
            if (hudRects[i] == null) continue;
            hudScales[i] = hudRects[i].localScale;
            hudPositions[i] = hudRects[i].anchoredPosition;
        }
        GameOptions.Changed += ApplyHUDSize;
        ApplyHUDSize();
        health = GetComponent<PlayerHealth>();
        nextHurtIndex = (health != null ? Mathf.Min(health.CurrentHealth, healthItem.Length) : healthItem.Length) - 1;
        if (health != null) health.Changed += HealthChanged;
        revealed = new bool[healthItem.Length];
        foreach (var item in healthItem)
            if (item != null) item.Play("Hidden", 0, 0f);
        StartCoroutine(ShowHealthItems());
    }

    private void ApplyHUDSize()
    {
        float scale = GameOptions.SmallHUD ? 1f : 1.25f;
        for (int i = 0; i < hudRects.Length; i++)
        {
            if (hudRects[i] == null) continue;
            hudRects[i].localScale = hudScales[i] * scale;
            hudRects[i].anchoredPosition = hudPositions[i] * scale;
        }
    }

    private void OnDestroy()
    {
        GameOptions.Changed -= ApplyHUDSize;
        if (health != null) health.Changed -= HealthChanged;
    }
    private void HealthChanged(int before, int after)
    {
        for (int i = Mathf.Min(before, after); i < Mathf.Min(Mathf.Max(before, after), healthItem.Length); i++)
            if (!hidden && revealed[i]) Trigger(healthItem[i], after > before ? "Enter" : "Hurt");
        nextHurtIndex = Mathf.Min(after, healthItem.Length)-1;
    }
    public void Hurt()
    {
        if (nextHurtIndex < 0)
        {
            return;
        }

        if (!hidden && revealed[nextHurtIndex]) Trigger(healthItem[nextHurtIndex], "Hurt");
        nextHurtIndex--;
    }
    public IEnumerator ShowHealthItems()
    {
        hidden = false;
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < healthItem.Length; i++)
        {
            if (hidden) yield break;
            revealed[i] = true;
            if (health == null || i < health.CurrentHealth) Trigger(healthItem[i], "Enter");
            else if (healthItem[i] != null) healthItem[i].Play("Empty", 0, 0f);
            yield return new WaitForSeconds(0.5f);
        }
        yield return new WaitForSeconds(1f);
        PlayGeoEnter();
    }
    public void HideHealthItems()
    {
        StopAllCoroutines();
        hidden = true;
        if (geo != null && geo.HasState(0, Animator.StringToHash("Exit"))) geo.Play("Exit");
        for (int i = 0; i < healthItem.Length; i++)
        {
            if (revealed[i]) Trigger(healthItem[i], "Hide");
            revealed[i] = false;
        }
    }
    public void RestoreHealth()
    {
        StopAllCoroutines();
        hidden = false;
        nextHurtIndex = (health != null ? Mathf.Min(health.CurrentHealth, healthItem.Length) : healthItem.Length) - 1;
        for (int i = 0; i < healthItem.Length; i++)
        {
            revealed[i] = true;
            if (health == null || i < health.CurrentHealth) Trigger(healthItem[i], "Enter");
            else if (healthItem[i] != null) healthItem[i].Play("Empty", 0, 0f);
        }
        PlayGeoEnter();
    }

    private static void Trigger(Animator item, string trigger)
    {
        if (item == null) return;
        item.ResetTrigger("Enter");
        item.ResetTrigger("Hurt");
        item.ResetTrigger("Hide");
        item.SetTrigger(trigger);
    }

    private void PlayGeoEnter()
    {
        if (geo != null && geo.HasState(0, Animator.StringToHash("enter"))) geo.Play("enter");
    }

}
