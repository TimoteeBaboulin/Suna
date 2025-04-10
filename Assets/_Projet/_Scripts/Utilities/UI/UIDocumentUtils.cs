using UnityEngine;
using UnityEngine.UIElements;

public class UIDocumentUtils
{
    static public void SetMargin<T>(ref T element, float value = 0) where T : VisualElement
    {
        SetMargin(ref element, Vector4.one * value);
    }
    static public void SetPadding<T>(ref T element, float value = 0) where T : VisualElement
    {
        SetPadding(ref element, Vector4.one * value);
    }
    static public void SetMargin<T>(ref T element, Vector4 values) where T : VisualElement
    {
        element.style.marginTop = values.x;
        element.style.marginLeft = values.y;
        element.style.marginRight = values.z;
        element.style.marginBottom = values.w;
    }
    static public void SetPadding<T>(ref T element, Vector4 values) where T : VisualElement
    {
        element.style.paddingTop = values.x;
        element.style.paddingLeft = values.y;
        element.style.paddingRight = values.z;
        element.style.paddingBottom = values.w;
    }
    static public void SetSize<T>(ref T element, float width, float height) where T : VisualElement
    {
        element.style.width = width;
        element.style.height = height;
    }
    static public void SetSize<T>(ref T element, Length width, Length height) where T : VisualElement
    {
        element.style.width = width;
        element.style.height = height;
    }
    static public void SetSize<T>(ref T element, Vector2 size) where T : VisualElement
    {
        SetSize(ref element, size.x, size.y);
    }
    static public void SetActive<T>(ref T element, bool value) where T : VisualElement
    {
        element.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
    }
    static public void ToggleActive<T>(ref T element) where T : VisualElement
    {
        element.style.display = element.style.display.value == DisplayStyle.Flex ? DisplayStyle.None : DisplayStyle.Flex;
    }
    static public bool IsActive<T>(ref T element) where T : VisualElement => element.style.display.value == DisplayStyle.Flex;

    static public Length PercentLength(float value) => new(value, LengthUnit.Percent);
    static public Length PixelLength(float value) => new(value, LengthUnit.Pixel);
    static public Length AutoLength() => Length.Auto();

    static public void SetPosition<T>(ref T element, TextAnchor anchor, float xShift, float yShift) where T : VisualElement
    {
        element.style.top = AutoLength();
        element.style.left = AutoLength();
        element.style.right = AutoLength();
        element.style.bottom = AutoLength();
        element.style.translate = new(new Translate(0f, 0f));

        switch (anchor)
        {
            case TextAnchor.UpperLeft:
                element.style.top = PixelLength(yShift);
                element.style.left = PixelLength(xShift);
                break;
            case TextAnchor.UpperRight:
                element.style.top = PixelLength(yShift);
                element.style.right = PixelLength(xShift);
                break;
            case TextAnchor.LowerLeft:
                element.style.bottom = PixelLength(yShift);
                element.style.left = PixelLength(xShift);
                break;
            case TextAnchor.LowerRight:
                element.style.bottom = PixelLength(yShift);
                element.style.right = PixelLength(xShift);
                break;

            case TextAnchor.MiddleLeft:
                element.style.top = PercentLength(50);
                element.style.bottom = PercentLength(50);
                element.style.left = PixelLength(xShift);
                element.style.translate = new(new Translate(0f, PercentLength(-50)));
                break;
            case TextAnchor.MiddleRight:
                element.style.top = PercentLength(50);
                element.style.bottom = PercentLength(50);
                element.style.right = PixelLength(xShift);
                element.style.translate = new(new Translate(0f, PercentLength(-50)));
                break;
            case TextAnchor.UpperCenter:
                element.style.left = PercentLength(50);
                element.style.right = PercentLength(50);
                element.style.top = PixelLength(yShift);
                element.style.translate = new(new Translate(PercentLength(-50), 0f));
                break;
            case TextAnchor.LowerCenter:
                element.style.left = PercentLength(50);
                element.style.right = PercentLength(50);
                element.style.bottom = PixelLength(yShift);
                element.style.translate = new(new Translate(PercentLength(-50), 0f));
                break;

            case TextAnchor.MiddleCenter:
                element.style.left = PercentLength(50);
                element.style.right = PercentLength(50);
                element.style.top = PercentLength(50);
                element.style.bottom = PercentLength(50);
                element.style.translate = new(new Translate(PercentLength(-50), PercentLength(-50)));
                break;
        }
    }
    static public void SetPosition<T>(ref T element, TextAnchor anchor, Vector2 shiftPosition) where T : VisualElement
    {
        SetPosition(ref element, anchor, shiftPosition.x, shiftPosition.y);
    }
    static public void SetSimplePosition<T>(ref T element, float xShift, float yShift) where T : VisualElement
    {
        element.style.top = yShift;
        element.style.left = xShift;
        element.style.right = AutoLength();
        element.style.bottom = AutoLength();
        element.style.translate = new(new Translate(0f, 0f));
    }
    static public void SetSimplePosition<T>(ref T element, Vector2 shiftPosition) where T : VisualElement
    {
        SetSimplePosition(ref element, shiftPosition.x, shiftPosition.y);
    }

    static public void SetBorderWidth<T>(ref T element, float value) where T : VisualElement
    {
        SetBorderWidth(ref element, Vector4.one * value);
    }
    static public void SetBorderWidth<T>(ref T element, Vector4 values) where T : VisualElement
    {
        element.style.borderTopWidth = values.x;
        element.style.borderLeftWidth = values.y;
        element.style.borderRightWidth = values.z;
        element.style.borderBottomWidth = values.w;
    }

    static public void SetBorderColor<T>(ref T element, Color value) where T : VisualElement
    {
        element.style.borderTopColor = value;
        element.style.borderLeftColor = value;
        element.style.borderRightColor = value;
        element.style.borderBottomColor = value;
    }

    static public Label Label(string text, float fontSize, Color color)
    {
        Label label = new(text);
        label.style.fontSize = fontSize;
        label.style.color = color;
        return label;
    }
    static public void ToggleBold(ref Label label)
    {
        label.style.unityFontStyleAndWeight = label.resolvedStyle.unityFontStyleAndWeight switch
        {
            FontStyle.Normal => FontStyle.Bold,
            FontStyle.Bold => FontStyle.Normal,
            FontStyle.Italic => FontStyle.BoldAndItalic,
            FontStyle.BoldAndItalic => FontStyle.Italic,
            _ => FontStyle.Normal
        };
    }
    static public void ToggleItalic(ref Label label)
    {
        label.style.unityFontStyleAndWeight = label.resolvedStyle.unityFontStyleAndWeight switch
        {
            FontStyle.Normal => FontStyle.Italic,
            FontStyle.Bold => FontStyle.BoldAndItalic,
            FontStyle.Italic => FontStyle.Normal,
            FontStyle.BoldAndItalic => FontStyle.Bold,
            _ => FontStyle.Normal
        };
    }
}
