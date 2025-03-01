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
        state.RequireForUpdate<NetworkTime>();
    }

    public void OnUpdate(ref SystemState state)
    {
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

        foreach (var (transform, localViewRotation, input, entity) in SystemAPI
            .Query<RefRW<LocalTransform>, RefRW<CharacterLocalViewRotation>, RefRO<CharacterInput>>()
            .WithAll<GhostOwnerIsLocal>()
            .WithEntityAccess())
        {
            int networkId = SystemAPI.GetComponentRO<GhostOwner>(entity).ValueRO.NetworkId;

            float mouseX = SystemAPI.Time.DeltaTime * input.ValueRO.look.x;
            float mouseY = SystemAPI.Time.DeltaTime * input.ValueRO.look.y;

            transform.ValueRW.Rotation = math.mul(transform.ValueRO.Rotation, quaternion.RotateY(math.radians(mouseX)));
            transform.ValueRW.Rotation.value.x = 0;
            transform.ValueRW.Rotation.value.z = 0;

            float newRotationYDeg = math.degrees(localViewRotation.ValueRW.Value.value.x) - mouseY;
            newRotationYDeg = math.clamp(newRotationYDeg, -40, 40);
            localViewRotation.ValueRW.Value.value.x = math.radians(newRotationYDeg);
            localViewRotation.ValueRW.Value.value.y = 0;
            localViewRotation.ValueRW.Value.value.z = 0;

            Entity rcpEntity = state.EntityManager.CreateEntity(typeof(UpdateViewRotationRcpCommand), typeof(SendRpcCommandRequest));
            state.EntityManager.SetComponentData(rcpEntity, new UpdateViewRotationRcpCommand
            {
                NetworkId = networkId,
                RotationX = transform.ValueRO.Rotation,
                RotationY = localViewRotation.ValueRO.Value
            });
        }
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
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

            foreach (var (transform, localViewRotation, characterAndViewRotation, ghostOwner) in SystemAPI
                .Query<RefRW<LocalTransform>, RefRW<CharacterLocalViewRotation>, RefRW<CharacterAndViewRotationComponent>, RefRO<GhostOwner>>()
                .WithAll<CharacterComponent>())
            {
                if (ghostOwner.ValueRO.NetworkId != viewRotationCommand.ValueRO.NetworkId)
                {
                    continue;
                }

                transform.ValueRW.Rotation = viewRotationCommand.ValueRO.RotationX;

                localViewRotation.ValueRW.Value = viewRotationCommand.ValueRO.RotationY;

                characterAndViewRotation.ValueRW.CharacterRotation = transform.ValueRO.Rotation;
                characterAndViewRotation.ValueRW.ViewRotation = localViewRotation.ValueRO.Value;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct UpdateOtherCharacterAndViewRotationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, localViewRotation, characterAndViewRotation, ghostOwner) in SystemAPI
                .Query<RefRW<LocalTransform>, RefRW<CharacterLocalViewRotation>, RefRW<CharacterAndViewRotationComponent>, RefRO<GhostOwner>>()
                .WithAll<CharacterComponent>()
                .WithNone<GhostOwnerIsLocal>())
        {
            transform.ValueRW.Rotation = characterAndViewRotation.ValueRO.CharacterRotation;
            localViewRotation.ValueRW.Value = characterAndViewRotation.ValueRO.ViewRotation;
        }
    }
}
