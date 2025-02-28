using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct CharacterViewSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MainEntityCameraTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

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
            characterTransform.ValueRW.Rotation.value.x = 0;
            characterTransform.ValueRW.Rotation.value.z = 0;

            float newRotationYDeg = math.degrees(transform.ValueRO.Rotation.value.x) - mouseY;
            newRotationYDeg = math.clamp(newRotationYDeg, -40, 40);
            transform.ValueRW.Rotation.value.x = math.radians(newRotationYDeg);
            transform.ValueRW.Rotation.value.y = 0;
            transform.ValueRW.Rotation.value.z = 0;

            Entity rcpEntity = state.EntityManager.CreateEntity(typeof(UpdateViewRotationRcpCommand), typeof(SendRpcCommandRequest));
            state.EntityManager.SetComponentData(rcpEntity, new UpdateViewRotationRcpCommand
            {
                NetworkId = networkId,
                RotationX = characterTransform.ValueRO.Rotation,
                RotationY = transform.ValueRO.Rotation
            });
        }
    }
}

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

            foreach (var (transform, characterViewEntity, characterAndViewRotation, ghostOwner) in SystemAPI
                .Query<RefRW<LocalTransform>, RefRO<CharacterViewEntityComponent>, RefRW<CharacterAndViewRotationComponent>, RefRO<GhostOwner>>()
                .WithAll<CharacterComponent>())
            {
                if (ghostOwner.ValueRO.NetworkId != viewRotationCommand.ValueRO.NetworkId)
                {
                    continue;
                }

                transform.ValueRW.Rotation = viewRotationCommand.ValueRO.RotationX;

                RefRW<LocalTransform> viewTransform = SystemAPI.GetComponentRW<LocalTransform>(characterViewEntity.ValueRO.Value);
                viewTransform.ValueRW.Rotation = viewRotationCommand.ValueRO.RotationY;

                characterAndViewRotation.ValueRW.CharacterRotation = transform.ValueRO.Rotation;
                characterAndViewRotation.ValueRW.ViewRotation = viewTransform.ValueRO.Rotation;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct UpdateOtherCharacterAndViewRotationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, characterViewEntity, characterAndViewRotation, ghostOwner) in SystemAPI
                .Query<RefRW<LocalTransform>, RefRO<CharacterViewEntityComponent>, RefRW<CharacterAndViewRotationComponent>, RefRO<GhostOwner>>()
                .WithAll<CharacterComponent>()
                .WithNone<GhostOwnerIsLocal>())
        {
            transform.ValueRW.Rotation = characterAndViewRotation.ValueRO.CharacterRotation;

            RefRW<LocalTransform> viewTransform = SystemAPI.GetComponentRW<LocalTransform>(characterViewEntity.ValueRO.Value);
            viewTransform.ValueRW.Rotation = characterAndViewRotation.ValueRO.ViewRotation;
        }
    }
}
