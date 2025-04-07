using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class FloatingWindow : VisualElement
{
    public static BindingId titleProperty = nameof(title);

    [UxmlAttribute]
    public string title = "Floating Window";

    public FloatingWindow()
    {
        style.position = Position.Absolute;
        Label titleLabel = new Label(title);
        titleLabel.name = "floating-window-title";
        titleLabel.AddToClassList(titleLabel.name);
        Add(titleLabel);
    }
}
