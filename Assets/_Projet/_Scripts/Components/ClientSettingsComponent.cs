using Unity.Collections;
using Unity.Entities;

public struct ClientSettingsComponent : IComponentData
{
    public float Sensivity;
    public float Volume;
    public FixedString32Bytes Pseudo;
}

public struct ClientSettingsSaveTag : IComponentData { }
