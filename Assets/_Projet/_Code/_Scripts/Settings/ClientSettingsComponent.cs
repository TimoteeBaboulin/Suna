using Unity.Entities;

public struct ClientSettingsComponent : IComponentData
{
    public float Sensivity;
}

public struct ClientSettingsSaveTag : IComponentData { }
