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

        EntityQuery query = state.GetEntityQuery(typeof(GameResourcesInstantiateStuffQueue));
        query.SetChangedVersionFilter(typeof(GameResourcesInstantiateStuffQueue));
        state.RequireForUpdate(query);
    }

    public void OnUpdate(ref SystemState state)
    {
        //Get ECB
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        //
        foreach (var (prefabsRO, database, instantiateStuffQueu) in SystemAPI.Query<
            RefRO<GameResourcesStuffEntityPrefabs>,
            RefRO<GameResourcesDatabase>,
            DynamicBuffer<GameResourcesInstantiateStuffQueue>>())
        {
            ref var stuffCommonDataArray = ref database.ValueRO.StuffDatabaseRef.Value.StuffCommonData;
            ref readonly var prefabs = ref prefabsRO.ValueRO;
            FixedString128Bytes Name;

            //Explore Stuff Infos queue
            foreach (var stuffInfos in instantiateStuffQueu)
            {
                //Retrieve stuff in database
                for (int i = 0; i < stuffCommonDataArray.Length; i++)
                {
                    Name = stuffInfos.StuffName;

                    if (stuffCommonDataArray[i].Name.ToString() == Name.Value)
                    {
                        ref StuffCommonData stuffData = ref stuffCommonDataArray[i];

                        //Load entity prefab depending on stuff type
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
                            default:
                                break;
                        }

                        if (prefab != Entity.Null)
                        {
                            //Instantiate the Entity prefab
                            Entity stuff = ecb.Instantiate(prefab);

                            //Define database access for this new stuff
                            ecb.SetComponent(stuff, new StuffDatabaseAccess
                            {
                                ID = i,
                                IsConnectedToDatabase = true,
                                NameInDatabase = stuffData.Name.ToString()
                            });

                            //If the queue specified an owner, we equip them
                            if (stuffInfos.Owner != Entity.Null)
                            {
                                ecb.SetComponentEnabled<StuffProcessPending>(stuff, true);
                                ecb.SetComponent(stuff, new StuffProcessPending { Owner = stuffInfos.Owner });
                            }
                        }
                        break;
                    }
                    else continue;
                }
            }
            instantiateStuffQueu.Clear();
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

        foreach (var (ownerRO, dataAccessRW, processRO, stuff) in SystemAPI
            .Query<RefRO<StuffOwner>, RefRW<StuffDatabaseAccess>, RefRO<StuffProcessPending>>()
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

            SpecificLoadSet(ref state, stuff, ref stuffData, ref database.StuffDatabaseRef.Value);

            //Add stuff on player inventory
            if (processRO.ValueRO.Owner != Entity.Null)
            {
                var equipStuffQueu = SystemAPI.GetSingletonBuffer<EquipStuffQueue>();
                equipStuffQueu.Add(new EquipStuffQueue
                {
                    Stuff = stuff,
                    Owner = processRO.ValueRO.Owner,
                });
            }

            SystemAPI.SetComponentEnabled<StuffProcessPending>(stuff, false);
        }
    }

    void SpecificLoadSet(ref SystemState state, Entity stuff, ref StuffCommonData stuffData, ref StuffDatabase database)
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
                break;
            case StuffType.MeleeWeapon:
                SystemAPI.SetComponent(stuff, new MeleeWeaponDatabaseAccess { Value = stuffData.dataID });
                break;
            default:

                break;
        }
    }
}



