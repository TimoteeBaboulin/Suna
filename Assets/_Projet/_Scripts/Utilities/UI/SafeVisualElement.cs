using UnityEngine.UIElements;

public abstract class SafeVisualElement : VisualElement
{
    private bool initialized = false;

    public SafeVisualElement()
    {
        RegisterCallback<AttachToPanelEvent>(OnAttachSafe);
    }

    private void OnAttachSafe(AttachToPanelEvent evt)
    {
        if (initialized) return;

        initialized = true;
        OnInit();
    }

    protected abstract void OnInit();
}
