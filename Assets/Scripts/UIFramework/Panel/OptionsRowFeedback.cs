using System;
using UnityEngine;

// The description follows the highlighted row, just as the decorative pointers do.
public sealed class OptionsRowFeedback : MonoBehaviour
{
    public Action<bool> Highlight;
    private ButtonHover hover;
    private void OnEnable()
    {
        hover = GetComponent<ButtonHover>();
        if (hover != null) hover.HighlightChanged += OnHighlightChanged;
    }
    private void OnHighlightChanged(bool active) => Highlight?.Invoke(active);
    private void OnDisable()
    {
        if (hover != null) hover.HighlightChanged -= OnHighlightChanged;
        Highlight?.Invoke(false);
    }
}
