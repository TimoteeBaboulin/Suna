using GameNetwork;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class LoadingScreen : MonoBehaviour
{
    private Label _loadingLabel;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Bind the progress bar (if needed)
        root.Q<ProgressBar>().SetBinding("value", new DataBinding
        {
            dataSource = LoadingData.Instance,
            dataSourcePath = new PropertyPath(LoadingData.LoadingProgressPropertyName),
            bindingMode = BindingMode.ToTarget,
        });

        // Bind the label's text property to LoadingStatusText.
        _loadingLabel = root.Q<Label>("LoadingStatus");
        _loadingLabel.SetBinding("text", new DataBinding
        {
            dataSource = LoadingData.Instance,
            dataSourcePath = new PropertyPath(LoadingData.LoadingStatusTextPropertyName),
            bindingMode = BindingMode.ToTarget,
        });

        // Log the initial value.
        Debug.Log("Initial LoadingStatusText: " + LoadingData.Instance.LoadingStatusText);

        // Subscribe to property changes for debug logging.
        LoadingData.Instance.propertyChanged += OnLoadingDataChanged;
    }

    void OnDisable()
    {
        LoadingData.Instance.propertyChanged -= OnLoadingDataChanged;
    }

    private void OnLoadingDataChanged(object sender, BindablePropertyChangedEventArgs e)
    {
        // Log changes when the LoadingStatusText property is updated.
        if (e.propertyName == LoadingData.LoadingStatusTextPropertyName)
        {
            Debug.Log("LoadingStatusText changed to: " + LoadingData.Instance.LoadingStatusText);
        }
    }
}
