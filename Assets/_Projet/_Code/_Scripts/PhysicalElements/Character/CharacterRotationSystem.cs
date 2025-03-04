using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct CharacterRotationSystem : ISystem
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
        }
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
partial struct ClientCharacterAndViewRotationRpcSendSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (characterTransform, characterLocalView) in SystemAPI
            .Query<RefRO<LocalTransform>, RefRO<CharacterLocalViewRotation>>()
            .WithAll<GhostOwnerIsLocal>())
        {
            ClientCharacterAndViewRotationRpcCommand command = new ClientCharacterAndViewRotationRpcCommand
            {
                CharacterRotation = characterTransform.ValueRO.Rotation,
                ViewRotation = characterLocalView.ValueRO.Value,
            };

            RpcUtils.SendClientToServerRpc(ref command);
        }
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
partial struct ServerCharacterAndViewRotationRpcReceiveSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (request, characterAndViewRotationRpc, entity) in SystemAPI
            .Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ClientCharacterAndViewRotationRpcCommand>>()
            .WithEntityAccess())
        {
            RefRO<NetworkId> requestNetworkId = SystemAPI.GetComponentRO<NetworkId>(request.ValueRO.SourceConnection);
           
            foreach (var (characterTransform, characterLocalViewRotation, characterAndViewRotation, ghostOwner) in SystemAPI
                .Query<RefRW<LocalTransform>, RefRW<CharacterLocalViewRotation>, RefRW<CharacterAndViewRotationComponent>, RefRO<GhostOwner>>())
            {
                if (ghostOwner.ValueRO.NetworkId != requestNetworkId.ValueRO.Value)
                {
                    continue;
                }

                characterTransform.ValueRW.Rotation = characterAndViewRotationRpc.ValueRO.CharacterRotation;

                characterLocalViewRotation.ValueRW.Value = characterAndViewRotationRpc.ValueRO.ViewRotation;

                characterAndViewRotation.ValueRW.CharacterRotation = characterTransform.ValueRO.Rotation;
                characterAndViewRotation.ValueRW.ViewRotation = characterLocalViewRotation.ValueRO.Value;
            }

            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
partial struct UpdateOtherCharacterAndViewRotationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
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
