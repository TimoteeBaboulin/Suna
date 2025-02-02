using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct CharacterViewSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MainEntityCameraTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

        if (!networkTime.IsFirstPredictionTick)
        {
            return;
        }

        foreach (var (transform, parent, entity) in SystemAPI
            .Query<RefRW<LocalTransform>, RefRO<Parent>>()
            .WithAll<MainEntityCameraTag>()
            .WithEntityAccess())
        {
            RefRW<LocalTransform> characterTransform = SystemAPI.GetComponentRW<LocalTransform>(parent.ValueRO.Value);
            RefRO<CharacterInput> input = SystemAPI.GetComponentRO<CharacterInput>(parent.ValueRO.Value);
            int networkId = SystemAPI.GetComponentRO<GhostOwner>(parent.ValueRO.Value).ValueRO.NetworkId;

            float mouseX = SystemAPI.Time.DeltaTime * input.ValueRO.look.x;
            float mouseY = SystemAPI.Time.DeltaTime * input.ValueRO.look.y;

            characterTransform.ValueRW.Rotation = math.mul(characterTransform.ValueRO.Rotation, quaternion.RotateY(math.radians(mouseX)));
            transform.ValueRW.Rotation = math.mul(transform.ValueRO.Rotation, quaternion.RotateX(math.radians(-mouseY)));

            Entity rcpEntity = state.EntityManager.CreateEntity(typeof(UpdateViewRotationRcpCommand), typeof(SendRpcCommandRequest));
            state.EntityManager.SetComponentData(rcpEntity, new UpdateViewRotationRcpCommand
            {
                NetworkId = networkId,
                RotationX = characterTransform.ValueRO.Rotation,
                RotationY = transform.ValueRO.Rotation
            });

            //transform.ValueRW.Rotation.value.x = math.clamp(transform.ValueRO.Rotation.value.x, math.radians(-89f), math.radians(89f));
        }
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct ReceiveRcpCharacterViewSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UpdateViewRotationRcpCommand>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (viewRotationCommand, entity) in SystemAPI
            .Query<RefRO<UpdateViewRotationRcpCommand>>()
            .WithAll<ReceiveRpcCommandRequest>()
            .WithEntityAccess())
        {
            ecb.DestroyEntity(entity);

            foreach (var (transform, characterViewEntity, ghostOwner) in SystemAPI
                .Query<RefRW<LocalTransform>, RefRO<CharacterViwEntityComponent>, RefRO<GhostOwner>>()
                .WithAll<CharacterComponent>())
            {
                if (ghostOwner.ValueRO.NetworkId != viewRotationCommand.ValueRO.NetworkId)
                {
                    continue;
                }

                transform.ValueRW.Rotation = viewRotationCommand.ValueRO.RotationX;

                RefRW<LocalTransform> viewTransform = SystemAPI.GetComponentRW<LocalTransform>(characterViewEntity.ValueRO.View);
                viewTransform.ValueRW.Rotation = viewRotationCommand.ValueRO.RotationY;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
