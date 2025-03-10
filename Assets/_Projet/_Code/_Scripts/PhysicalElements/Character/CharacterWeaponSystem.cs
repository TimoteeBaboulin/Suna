using Unity.Entities;
using Unity.NetCode;

public struct WaitForInstanciateDefaultWeapon : IComponentData { }

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct CharacterWeaponSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WaitForInstanciateDefaultWeapon>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (prefab, charaEntity) in SystemAPI
            .Query<RefRO<CharacterDefaultWeaponPrefab>>()
            .WithEntityAccess())
        {
            if (prefab.ValueRO.Value != Entity.Null)
            {
                ecb.RemoveComponent<WaitForInstanciateDefaultWeapon>(charaEntity);

                Entity weaponEntity = ecb.Instantiate(prefab.ValueRO.Value);

                ecb.SetComponent(weaponEntity, new WeaponOwner { Value = charaEntity });
                ecb.SetComponent(charaEntity, new CharacterDefaultWeapon { Value = weaponEntity });

                int networkId = state.EntityManager.GetComponentData<GhostOwner>(charaEntity).NetworkId;
                ecb.SetComponent(weaponEntity, new GhostOwner() //Set owner of player to connection
                {
                    NetworkId = networkId
                });
                ecb.AppendToBuffer(charaEntity, new LinkedEntityGroup() //Link it to connection
                {
                    Value = weaponEntity
                });
            }
        }
    }
}
