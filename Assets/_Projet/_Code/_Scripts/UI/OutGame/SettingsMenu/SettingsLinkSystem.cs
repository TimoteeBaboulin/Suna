using Unity.Entities;

partial class SettingsLinkSystem : SystemBase
{
    public bool ClientSettingsFound = false;

    protected override void OnUpdate()
    {
    }

    public bool TryGetClientSettings(out ClientSettingsComponent clientSettings)
    {
        ClientSettingsFound = SystemAPI.TryGetSingleton(out clientSettings);
        return ClientSettingsFound;
    }

    public void UpdateClientSettings(ClientSettingsComponent clientSettings)
    {
        Entity entity = SystemAPI.GetSingletonEntity<ClientSettingsComponent>();
        SystemAPI.SetComponent(entity, clientSettings);
        EntityManager.AddComponent<ClientSettingsSaveTag>(entity);
    }
}
