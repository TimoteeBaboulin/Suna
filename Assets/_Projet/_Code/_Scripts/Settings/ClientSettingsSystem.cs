using Unity.Burst;
using Unity.Entities;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[WorldSystemFilter(WorldSystemFilterFlags.Default, WorldSystemFilterFlags.ClientSimulation)]
partial struct ClientSettingsSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        if (SystemAPI.HasSingleton<ClientSettingsComponent>())
        {
            return;
        }

        EntityManager entityManager = state.EntityManager;

        Addressables.LoadAssetAsync<ClientSettingsData>("DefaultClientSettingsData").Completed += (handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                ClientSettingsData settingsData = handle.Result;

                Entity entity = entityManager.CreateEntity();
                entityManager.AddComponentData(entity, new ClientSettingsComponent
                {
                    Sensivity = settingsData.Sensivity
                });

                entityManager.SetName(entity, "ClientSettings");
            }
        };
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        
    }
}
