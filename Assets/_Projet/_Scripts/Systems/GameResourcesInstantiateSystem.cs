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

        EntityQuery query = state.GetEntityQuery(typeof(GameResourcesInstantiateStuffQueu));
        query.SetChangedVersionFilter(typeof(GameResourcesInstantiateStuffQueu));
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
            DynamicBuffer<GameResourcesInstantiateStuffQueu>>())
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

                            //If the queue specified an owner :
                            if (stuffInfos.Owner != Entity.Null)
                            {
                                var equipStuffQueu = SystemAPI.GetSingletonBuffer<EquipStuffQueu>();

                                equipStuffQueu.Add(new EquipStuffQueu
                                {
                                    Stuff = stuff,
                                    Owner = stuffInfos.Owner
                                });

                                //TODO : Tu t'es arretter ici

                                //ecb.SetComponent(stuff, new StuffOwner { Value = stuffInfos.Owner });

                                //int networkId = state.EntityManager.GetComponentData<GhostOwner>(stuffInfos.Owner).NetworkId;
                                //ecb.SetComponent(stuff, new GhostOwner { NetworkId = networkId }); //Set owner of player to connection
                                //ecb.AppendToBuffer(stuffInfos.Owner, new LinkedEntityGroup { Value = stuff }); //Link it to connection
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

            SpecificLoadSet(ref state, stuff, ref stuffData, ref database.StuffDatabaseRef.Value);
            
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



