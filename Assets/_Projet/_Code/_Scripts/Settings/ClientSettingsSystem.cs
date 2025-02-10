using System.IO;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[WorldSystemFilter(WorldSystemFilterFlags.Default, WorldSystemFilterFlags.ClientSimulation)]
partial struct ClientSettingsSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder query = new EntityQueryBuilder(Allocator.Temp);
        query.WithAll<ClientSettingsComponent, ClientSettingsSaveTag>();
        state.RequireForUpdate(state.GetEntityQuery(query));

        if (SystemAPI.HasSingleton<ClientSettingsComponent>())
        {
            return;
        }

        EntityManager entityManager = state.EntityManager;
        string filePath = Path.Combine(Application.persistentDataPath, "Settings.json");

        Entity entity = entityManager.CreateEntity();
        entityManager.SetName(entity, "ClientSettings");

        if (File.Exists(filePath))
        {
            string fileText = File.ReadAllText(filePath);
            ClientSettingsComponent settingsComponent = JsonUtility.FromJson<ClientSettingsComponent>(fileText);
            entityManager.AddComponentData(entity, settingsComponent);
        }
        else
        {
            Addressables.LoadAssetAsync<ClientSettingsData>("DefaultClientSettingsData").Completed += (handle) =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    ClientSettingsData settingsData = handle.Result;
                    ClientSettingsComponent settingsComponent = new ClientSettingsComponent()
                    {
                        Sensivity = settingsData.Sensivity,
                    };

                    entityManager.AddComponentData(entity, settingsComponent);
                    entityManager.AddComponentData(entity, new ClientSettingsSaveTag());
                }
            };
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        Entity entity = SystemAPI.GetSingletonEntity<ClientSettingsComponent>();
        ClientSettingsComponent settingsComponent = SystemAPI.GetComponent<ClientSettingsComponent>(entity);

        string filePath = Path.Combine(Application.persistentDataPath, "Settings.json");
        string fileText = JsonUtility.ToJson(settingsComponent);
        File.WriteAllText(filePath, fileText);

        state.EntityManager.RemoveComponent<ClientSettingsSaveTag>(entity);
    }
}
