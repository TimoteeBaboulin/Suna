using GameNetwork;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class LoadingScreen : MonoBehaviour
{
    private Label _loadingLabel;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        root.Q<ProgressBar>().SetBinding("value", new DataBinding
        {
            dataSource = SessionData.Instance,
            dataSourcePath = new PropertyPath(SessionData.LoadingProgressPropertyName),
            bindingMode = BindingMode.ToTarget,
        });

        _loadingLabel = root.Q<Label>("LoadingStatus");
        _loadingLabel.SetBinding("text", new DataBinding
        {
            dataSource = SessionData.Instance,
            dataSourcePath = new PropertyPath(SessionData.LoadingStatusTextPropertyName),
            bindingMode = BindingMode.ToTarget,
        });

        Debug.Log("Initial LoadingStatusText: " + SessionData.Instance.LoadingStatusText);

        SessionData.Instance.propertyChanged += OnLoadingDataChanged;
    }

    void OnDisable()
    {
        SessionData.Instance.propertyChanged -= OnLoadingDataChanged;
    }

    private void OnLoadingDataChanged(object sender, BindablePropertyChangedEventArgs e)
    {
        if (e.propertyName == SessionData.LoadingStatusTextPropertyName)
        {
            Debug.Log("LoadingStatusText changed to: " + SessionData.Instance.LoadingStatusText);
        }
    }
}
