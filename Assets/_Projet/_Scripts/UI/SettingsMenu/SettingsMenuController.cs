using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UIElements;




public struct NameCommand : IRpcCommand
{
    public FixedString32Bytes name;
    public Entity clientEntity;
}
public class SettingsMenuController : MonoBehaviour
{
    // Settings UI Elements
    public VisualElement root;
    private Button _saveButton;
    private Slider _sensitivitySlider;
    private FloatField _sensitivityField;
    private Slider _volumeSlider;
    private FloatField _volumeField;
    private TextField _pseudoField;

    private void Start()
    {
        // Initialize Elements and Callbacks
        _saveButton = root.Q<Button>("SaveButton");
        _saveButton.clicked += SaveAndCloseSettings;

        _sensitivitySlider = root.Q<Slider>("SensitivitySlider");
        _sensitivitySlider.RegisterValueChangedCallback(OnSensitivitySlider_ValueChanged);

        _sensitivityField = root.Q<FloatField>("SensitivityField");
        _sensitivityField.RegisterValueChangedCallback(OnSensitivityField_ValueChanged);

        _volumeSlider = root.Q<Slider>("VolumeSlider");
        _volumeSlider.RegisterValueChangedCallback(OnVolumeSlider_ValueChanged);

        _volumeField = root.Q<FloatField>("VolumeField");
        _volumeField.RegisterValueChangedCallback(OnVolumeField_ValueChanged);

        _pseudoField = root.Q<TextField>("PseudoField");

        // Load settings
        SettingsLinkSystem mainMenuLinkSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SettingsLinkSystem>();
        if (mainMenuLinkSystem.TryGetClientSettings(out ClientSettingsComponent clientSettings))
        {
            _sensitivitySlider.value = clientSettings.Sensivity;
            _sensitivityField.value = clientSettings.Sensivity;
            _volumeSlider.value = clientSettings.Volume;
            _volumeField.value = clientSettings.Volume;
            _pseudoField.value = clientSettings.Pseudo.ToString();
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

    private void OnVolumeSlider_ValueChanged(ChangeEvent<float> evt)
    {
        _volumeField.value = evt.newValue;
    }

    private void OnVolumeField_ValueChanged(ChangeEvent<float> evt)
    {
        float value = Mathf.Clamp(evt.newValue, _volumeSlider.lowValue, _volumeSlider.highValue);
        _volumeSlider.value = value;
        _volumeField.value = value;
    }

    private void SaveSettings()
    {
        float sensitivityValue = _sensitivitySlider.value;
        _sensitivitySlider.UnregisterValueChangedCallback(OnSensitivitySlider_ValueChanged);
        _sensitivityField.UnregisterValueChangedCallback(OnSensitivityField_ValueChanged);

        SettingsLinkSystem settingsLinkSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SettingsLinkSystem>();
        if (settingsLinkSystem.TryGetClientSettings(out ClientSettingsComponent clientSettings))
        {
            clientSettings.Sensivity = _sensitivitySlider.value;
            clientSettings.Volume = _volumeSlider.value;
            clientSettings.Pseudo = _pseudoField.value;

            NameCommand command = new NameCommand { name = clientSettings.Pseudo };
            RpcUtils.SendClientToServerRpc(ref command);
            settingsLinkSystem.UpdateClientSettings(clientSettings);
        }
    }

    public void SaveAndCloseSettings()
    {
        SaveSettings();
        root.RemoveFromHierarchy();
    }
}
