using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;


partial struct InstanciateEntityStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StuffEntityPrefabsBuffer>();
        state.RequireForUpdate<GameResourcesDatabase>();

        EntityQuery query = state.GetEntityQuery(typeof(InstantiateStuffQueue));
        query.SetChangedVersionFilter(typeof(InstantiateStuffQueue));
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
            DynamicBuffer<InstantiateStuffQueue>>())
        {
            ref var stuffCommonDataArray = ref databaseRO.ValueRO.StuffDatabaseRef.Value.StuffCommonData;

            // Explore Stuff Infos queue
            foreach (var instanteInfos in instantiateStuffQueue)
            {
                //UnityEngine.Debug.Log("Instanciate stuff " + instanteInfos.StuffName + " for " + instanteInfos.Owner);
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
                                Owner = instanteInfos.Owner,
                                Position = instanteInfos.Position
                            });

                            ecb.SetComponent(stuff, new StuffDynamicData
                            {
                                dropedEntityPrefab = stuffPrefabs[i].dropedEntityPrefab,
                                grenadeThrownPrefab = stuffPrefabs[i].thrownGrenadeEntityPrefab,
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
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (dataAccessRW, dynDataRW, processRO, ghostOwnerRW, stuff) in SystemAPI
            .Query<RefRW<StuffDatabaseAccess>, RefRW<StuffDynamicData>, RefRO<StuffProcessPending>, RefRW<GhostOwner>>()
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

            ecb.AddComponent(stuff, new SoundEmitter { keyGroup = stuffData.Name.ToString() });
            //Add stuff on player inventory
            if (processRO.ValueRO.Owner != Entity.Null)
            {
                var equipStuffQueu = SystemAPI.GetSingletonBuffer<EquipStuffQueue>();
                StuffUtils.EquipNextFrame(equipStuffQueu, processRO.ValueRO.Owner, stuff, true);
            }
            else
            {
                StuffUtils.InstantiateDrop(ref ecb, ref dynDataRW.ValueRW, stuff, processRO.ValueRO.Position, float3.zero, 0f);
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



