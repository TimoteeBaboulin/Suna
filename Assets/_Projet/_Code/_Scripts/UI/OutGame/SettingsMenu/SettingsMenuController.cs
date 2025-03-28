using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UIElements;

public class SettingsMenuController : MonoBehaviour
{
    // Settings UI Elements
    public VisualElement root;
    private Slider _sensitivitySlider;
    private FloatField _sensitivityField;

    private void Start()
    {
        _sensitivitySlider = root.Q<Slider>("SensitivitySlider");
        _sensitivitySlider.RegisterValueChangedCallback(OnSensitivitySlider_ValueChanged);

        _sensitivityField = root.Q<FloatField>("SensitivityField");
        _sensitivityField.RegisterValueChangedCallback(OnSensitivityField_ValueChanged);

        // Load settings
        if (ClientServerBootstrap.ServerWorld == null)
        {
            SettingsLinkSystem mainMenuLinkSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SettingsLinkSystem>();
            if (mainMenuLinkSystem.TryGetClientSettings(out ClientSettingsComponent clientSettings))
            {
                _sensitivitySlider.value = clientSettings.Sensivity;
                _sensitivityField.value = clientSettings.Sensivity;
            }
        }
    }

    private void OnSensitivitySlider_ValueChanged(ChangeEvent<float> evt)
    {
        _sensitivityField.value = evt.newValue;
    }

    private void OnSensitivityField_ValueChanged(ChangeEvent<float> evt)
    {
        float value = Mathf.Clamp(evt.newValue, _sensitivitySlider.lowValue, _sensitivitySlider.highValue);
        _sensitivitySlider.value = value;
        _sensitivityField.value = value;
    }

    public void SaveSettings()
    {
        float sensitivityValue = _sensitivitySlider.value;
        _sensitivitySlider.UnregisterValueChangedCallback(OnSensitivitySlider_ValueChanged);
        _sensitivityField.UnregisterValueChangedCallback(OnSensitivityField_ValueChanged);

        if (ClientServerBootstrap.ServerWorld == null)
        {
            SettingsLinkSystem settingsLinkSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SettingsLinkSystem>();
            if (settingsLinkSystem.TryGetClientSettings(out ClientSettingsComponent clientSettings))
            {
                clientSettings.Sensivity = _sensitivitySlider.value;
                settingsLinkSystem.UpdateClientSettings(clientSettings);
            }
        }
    }
}
