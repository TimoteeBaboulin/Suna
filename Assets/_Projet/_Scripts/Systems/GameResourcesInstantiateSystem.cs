using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;


partial struct InstanciateEntityStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameResourcesStuffEntityPrefabs>();
        state.RequireForUpdate<GameResourcesDatabase>();

        EntityQuery query = state.GetEntityQuery(typeof(GameResourcesInstanciateStuffQueu));
        query.SetChangedVersionFilter(typeof(GameResourcesInstanciateStuffQueu));
        state.RequireForUpdate(query);
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);


        foreach (var (prefabsRO, database, stuffQueu) in SystemAPI.Query<
            RefRO<GameResourcesStuffEntityPrefabs>,
            RefRO<GameResourcesDatabase>,
            DynamicBuffer<GameResourcesInstanciateStuffQueu>>())
        {
            ref var stuffCommonDataArray = ref database.ValueRO.StuffDatabaseRef.Value.StuffCommonData;
            ref readonly var prefabs = ref prefabsRO.ValueRO;
            FixedString128Bytes Name;

            foreach (var item in stuffQueu)
            {
                for (int i = 0; i < stuffCommonDataArray.Length; i++)
                {
                    Name = item.StuffName;

                    if (stuffCommonDataArray[i].Name.ToString() == Name.Value)
                    {
                        ref StuffCommonData stuffData = ref stuffCommonDataArray[i];

                        Entity prefab = Entity.Null;
                        switch (stuffData.type)
                        {
                            case StuffType.RangedWeapon:
                                prefab = prefabs.rangedWeaponEntityPrefab;
                                break;
                            case StuffType.MeleeWeapon:
                                prefab = prefabs.meleeWeaponEntityPrefab;
                                break;
                            case StuffType.Harvester:
                                prefab = prefabs.harvesterEntityPrefab;
                                break;
                            case StuffType.Grenade:
                                prefab = prefabs.grenadesEntityPrefab;
                                break;
                            default:
                                break;
                        }

                        if (prefab != Entity.Null)
                        {
                            Entity stuff = ecb.Instantiate(prefab);

                            ecb.SetComponent(stuff, new StuffDatabaseAccess
                            {
                                ID = i,
                                IsConnectedToDatabase = true,
                                NameInDatabase = stuffData.Name.ToString()
                            });

                            if (item.Owner != Entity.Null)
                            {
                                ecb.SetComponent(stuff, new StuffOwner { Value = item.Owner });

                                int networkId = state.EntityManager.GetComponentData<GhostOwner>(item.Owner).NetworkId;
                                ecb.SetComponent(stuff, new GhostOwner { NetworkId = networkId }); //Set owner of player to connection
                                ecb.AppendToBuffer(item.Owner, new LinkedEntityGroup { Value = stuff }); //Link it to connection
                            }
                        }
                        break;
                    }
                    else continue;
                }
            }
            stuffQueu.Clear();
        }
    }
}


partial struct ProcessPendingStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameResourcesDatabase>();
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<StuffProcessPending>().Build(ref state);
        state.RequireForUpdate(query);
    }

    public void OnUpdate(ref SystemState state)
    {
        var database = SystemAPI.GetSingleton<GameResourcesDatabase>();
        ref var stuffCommonDataArray = ref database.StuffDatabaseRef.Value.StuffCommonData;
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (ownerRO, dataAccessRW, stuff) in SystemAPI
            .Query<RefRO<StuffOwner>, RefRW<StuffDatabaseAccess>>()
            .WithAll<StuffProcessPending>()
            .WithEntityAccess())
        {
            //Connects the entity to the database if it has been instantiated with the scene
            if (!dataAccessRW.ValueRO.IsConnectedToDatabase)
            {
                for (int i = 0; i < stuffCommonDataArray.Length; i++)
                {

                    if (stuffCommonDataArray[i].Name.ToString() == dataAccessRW.ValueRO.NameInDatabase)
                    {
                        dataAccessRW.ValueRW.ID = i;
                        dataAccessRW.ValueRW.IsConnectedToDatabase = true;
                    }
                }
            }

            //Get Stuff Data on database
            ref StuffCommonData stuffData = ref dataAccessRW.ValueRO.GetData(ref database);

            //Rename Entity in hierarchy
            state.EntityManager.SetName(stuff, stuffData.Name.ToString());

            //Add stuff on player inventory
            if (ownerRO.ValueRO.Value != Entity.Null)
            {
                var charaStuffList = SystemAPI.GetComponentRW<CharacterStuffList>(ownerRO.ValueRO.Value);
                charaStuffList.ValueRW.Value[(int)stuffData.location] = stuff;
            }

            SpecificLoadSet(ref state, stuff, ref stuffData, ref database.StuffDatabaseRef.Value, ecb);
            
            SystemAPI.SetComponentEnabled<StuffProcessPending>(stuff, false);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    void SpecificLoadSet(ref SystemState state, Entity stuff, ref StuffCommonData stuffData, ref StuffDatabase database, EntityCommandBuffer ecb)
    {
        switch (stuffData.type)
        {
            case StuffType.RangedWeapon:
                SystemAPI.SetComponent(stuff, new RangedWeaponDatabaseAccess { Value = stuffData.dataID });

                ref RangedWeaponCommonData data = ref database.RangedWeaponsCommonData[stuffData.dataID];
                SystemAPI.SetComponent(stuff, new RangedWeaponDynamicData
                {
                    currentAmmo = data.magazineCapacity + 1, // 1 = bullet in chamber
                    remainingAmmo = data.magazineCapacity * (data.nbMagazine - 1),
                });
                if (data.scope.ScopeFOV != 0)
                {
                    ecb.AddComponent(stuff, data.scope);
                }
                break;
            case StuffType.MeleeWeapon:
                SystemAPI.SetComponent(stuff, new MeleeWeaponDatabaseAccess { Value = stuffData.dataID });
                break;
            case StuffType.Grenade:
                SystemAPI.SetComponent(stuff, new GrenadeDatabaseAccess { Value = stuffData.dataID });
                break;
            default:

                break;
        }
    }
}



