using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;


partial struct InstanciateEntityStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StuffEntityPrefabsBuffer>();
        state.RequireForUpdate<GameResourcesDatabase>();

        EntityQuery query = state.GetEntityQuery(typeof(GameResourcesInstantiateStuffQueue));
        query.SetChangedVersionFilter(typeof(GameResourcesInstantiateStuffQueue));
        state.RequireForUpdate(query);
    }

    public void OnUpdate(ref SystemState state)
    {
        // Get ECB
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // Query
        foreach (var (databaseRO, stuffPrefabs, instantiateStuffQueue) in SystemAPI.Query<
            RefRO<GameResourcesDatabase>,
            DynamicBuffer<StuffEntityPrefabsBuffer>,
            DynamicBuffer<GameResourcesInstantiateStuffQueue>>())
        {
            ref var stuffCommonDataArray = ref databaseRO.ValueRO.StuffDatabaseRef.Value.StuffCommonData;

            // Explore Stuff Infos queue
            foreach (var instanteInfos in instantiateStuffQueue)
            {
                // Retrieve stuff in database
                for (int i = 0; i < stuffCommonDataArray.Length; i++)
                {
                    if (stuffCommonDataArray[i].Name.ToString() == instanteInfos.StuffName)
                    {
                        if (stuffPrefabs[i].inHandEntityPrefab != Entity.Null)
                        {
                            Entity stuff = ecb.Instantiate(stuffPrefabs[i].inHandEntityPrefab);

                            if (instanteInfos.Owner != Entity.Null)
                            {
                                int networkId = state.EntityManager.GetComponentData<GhostOwner>(instanteInfos.Owner).NetworkId;
                                ecb.SetComponent(stuff, new GhostOwner { NetworkId = networkId });
                                ecb.AppendToBuffer(instanteInfos.Owner, new LinkedEntityGroup { Value = stuff });
                            }
                            else
                            {
                                ecb.SetComponent(stuff, new GhostOwner { NetworkId = -1 });
                            }

                            //Define database access for this new stuff

                            ecb.SetComponent(stuff, new StuffDatabaseAccess
                            {
                                ID = i,
                                IsConnectedToDatabase = true,
                                NameInDatabase = instanteInfos.StuffName
                            });

                            ecb.SetComponentEnabled<StuffProcessPending>(stuff, true);
                            ecb.SetComponent(stuff, new StuffProcessPending 
                            { 
                                Owner = instanteInfos.Owner ,
                            });

                            ecb.SetComponent(stuff, new StuffDynamicData
                            {
                                dropedEntityPrefab = stuffPrefabs[i].dropedEntityPrefab
                            });
                        }
                        break;
                    }
                    else continue;
                }
            }
            instantiateStuffQueue.Clear();
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

        foreach (var (dataAccessRW, processRO, ghostOwnerRW, stuff) in SystemAPI
            .Query<RefRW<StuffDatabaseAccess>, RefRO<StuffProcessPending>, RefRW<GhostOwner>>()
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

            // When spawning a server-owned ghost
            //ghostOwnerRW.ValueRW.NetworkId = -1;

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



