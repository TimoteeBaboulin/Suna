using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

public struct StuffIsInstanciedTag : IComponentData { }

public struct OwnerRefComponent : IComponentData
{
    public Entity Value;
}

[BurstCompile]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct CharacterInstanciateStuffsSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
        builder.WithAll<CharacterStuffsPrefabComponent>().WithAbsent<StuffIsInstanciedTag>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (stuffsPrefab, stuffs, charaEntity) in SystemAPI.Query<RefRO<CharacterStuffsPrefabComponent>, RefRW<CharacterStuffsComponent>>().WithEntityAccess())
        {
            if (stuffsPrefab.ValueRO.mainWeapon != Entity.Null)
            {
                stuffs.ValueRW.mainWeapon = ecb.Instantiate(stuffsPrefab.ValueRO.mainWeapon);
                ecb.AddComponent(stuffs.ValueRW.mainWeapon, new OwnerRefComponent { Value = charaEntity });

                //ecb.AddComponent(stuffs.ValueRW.mainWeapon, new Parent { Value = charaEntity });

                //ecb.AddBuffer<Child>(charaEntity);
                //ecb.AppendToBuffer(charaEntity, new Child { Value = stuffs.ValueRW.mainWeapon });
            }

            ecb.AddComponent(charaEntity, new StuffIsInstanciedTag());
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[BurstCompile]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct EquipStuffSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<OwnerRefComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (ownerEntity, stuffTransform, stuffEntity) in SystemAPI
            .Query<RefRO<OwnerRefComponent>, RefRW<LocalTransform>>()
            .WithEntityAccess())
        {
            if (state.EntityManager.HasBuffer<Child>(ownerEntity.ValueRO.Value))
            {
                DynamicBuffer<Child> childBuffer = state.EntityManager.GetBuffer<Child>(ownerEntity.ValueRO.Value);
                foreach (Child child in childBuffer)
                {
                    if (state.EntityManager.HasComponent<MainEntityCameraTag>(child.Value))
                    {
                        LocalToWorld cameraTransform = state.EntityManager.GetComponentData<LocalToWorld>(child.Value);

                        stuffTransform.ValueRW.Position = cameraTransform.Position 
                            + cameraTransform.Forward * 0.6f 
                            + stuffTransform.ValueRW.Right() * 0.4f 
                            - stuffTransform.ValueRW.Up() * 0.3f;

                        stuffTransform.ValueRW.Rotation = cameraTransform.Rotation;
                    }
                }
            }
        }
    }
}

//[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
//public partial struct CharacterInstanciateStuffsSystem : ISystem
//{
//    public void OnCreate(ref SystemState state)
//    {
//        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
//        builder.WithAll<CharacterStuffsPrefabComponent>().WithAbsent<StuffIsInstanciedTag>();
//        state.RequireForUpdate(state.GetEntityQuery(builder));
//    }

//    public void OnUpdate(ref SystemState state)
//    {
//        //EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
//        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
//        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

//        foreach (var (stuffsPrefab, stuffs, currentStuff, charaEntity) in SystemAPI.Query<
//            RefRO<CharacterStuffsPrefabComponent>, RefRW<CharacterStuffsComponent>,
//            RefRW<CharacterCurrentStuffComponent>>().WithEntityAccess())
//        {
//            if (stuffsPrefab.ValueRO.mainWeapon != Entity.Null)
//            {
//                stuffs.ValueRW.mainWeapon = ecb.Instantiate(stuffsPrefab.ValueRO.mainWeapon);
//                //stuffs.ValueRW.mainWeapon = state.EntityManager.Instantiate(stuffsPrefab.ValueRO.mainWeapon);

//                ecb.AddComponent(stuffs.ValueRW.mainWeapon, new OwnerRefComponent { owner = charaEntity });
//                currentStuff.ValueRW.Value = stuffs.ValueRW.mainWeapon;
//            }

//            ecb.AddComponent(charaEntity, new StuffIsInstanciedTag());
//        }

//        //ecb.Playback(state.EntityManager);
//        //ecb.Dispose();
//    }
//}


//[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
//public partial struct CurrentWeaponFPSViewSystem : ISystem
//{

//    public void OnCreate(ref SystemState state)
//    {
//        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp);
//        builder.WithAll<CharacterCurrentStuffComponent>().WithAll<StuffIsInstanciedTag>();
//        state.RequireForUpdate(state.GetEntityQuery(builder));
//    }

//    public void OnUpdate(ref SystemState state)
//    {
//        foreach (var (currentWeapon, childBuffer) in SystemAPI.Query<RefRW<CharacterCurrentStuffComponent>, DynamicBuffer<Child>>())
//        {
//            foreach (Child child in childBuffer)
//            {
//                if (state.EntityManager.HasComponent<MainEntityCameraTag>(child.Value))
//                {
//                    LocalTransform cameraTransform = state.EntityManager.GetComponentData<LocalTransform>(child.Value);

//                    LocalTransform weaponTransform = state.EntityManager.GetComponentData<LocalTransform>(currentWeapon.ValueRW.Value);
//                    weaponTransform.Position = cameraTransform.Position /*+ cameraTransform.Forward() * 0.6f + weaponTransform.Right() * 0.4f - weaponTransform.Up() * 0.3f*/;
//                    weaponTransform.Rotation = cameraTransform.Rotation;

//                    state.EntityManager.SetComponentData(currentWeapon.ValueRW.Value, weaponTransform);
//                    Debug.Log(weaponTransform.Position);
//                }
//            }
//        }
//    }
//}
