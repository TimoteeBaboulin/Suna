using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

// The character's rotation affects the Y-axis of its LocalTransform.
// The view is a separate quaternion contained within a dedicated component.
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct CharacterAndViewRotationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder queryBuilder = new EntityQueryBuilder(Allocator.Temp);
        queryBuilder.WithAll<LocalTransform, CharacterLocalViewRotation, CharacterInput, GhostOwnerIsLocal>();
        state.RequireForUpdate(state.GetEntityQuery(queryBuilder));
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (characterTransform, CharacterLocalViewRotation, CharacterInput) in SystemAPI
            .Query<RefRW<LocalTransform>, RefRW<CharacterLocalViewRotation>, RefRO<CharacterInput>>()
            .WithAll<GhostOwnerIsLocal>())
        {
            float mouseX = SystemAPI.Time.DeltaTime * CharacterInput.ValueRO.look.x;
            float mouseY = SystemAPI.Time.DeltaTime * CharacterInput.ValueRO.look.y;

            characterTransform.ValueRW.Rotation = math.mul(characterTransform.ValueRO.Rotation, quaternion.RotateY(math.radians(mouseX)));
            characterTransform.ValueRW.Rotation.value.x = 0;
            characterTransform.ValueRW.Rotation.value.z = 0;

            // We perform the calculations in degrees to prevent the clamp from returning the opposite value if the mouse movement value is too high.
            float newRotationYDegree = math.degrees(CharacterLocalViewRotation.ValueRW.ViewRotation.value.x) - mouseY;
            newRotationYDegree = math.clamp(newRotationYDegree, -40, 40);
            CharacterLocalViewRotation.ValueRW.ViewRotation.value.x = math.radians(newRotationYDegree);
            CharacterLocalViewRotation.ValueRW.ViewRotation.value.y = 0;
            CharacterLocalViewRotation.ValueRW.ViewRotation.value.z = 0;
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
        EntityQueryBuilder queryBuilder = new EntityQueryBuilder(Allocator.Temp);
        queryBuilder.WithAll<LocalTransform, CharacterLocalViewRotation, GhostOwnerIsLocal>();
        state.RequireForUpdate(state.GetEntityQuery(queryBuilder));
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
                ViewRotation = characterLocalView.ValueRO.ViewRotation,
            };

            // Send the RPC command to the server.
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
        EntityQueryBuilder queryBuilderRcpCommand = new EntityQueryBuilder(Allocator.Temp);
        queryBuilderRcpCommand.WithAll<ReceiveRpcCommandRequest, ClientCharacterAndViewRotationRpcCommand>();
        state.RequireForUpdate(state.GetEntityQuery(queryBuilderRcpCommand));
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
                characterLocalViewRotation.ValueRW.ViewRotation = characterAndViewRotationRpc.ValueRO.ViewRotation;

                // Which allows synchronizing the character's rotation and its view rotation with all other clients.
                characterAndViewRotation.ValueRW.CharacterRotation = characterTransform.ValueRO.Rotation;
                characterAndViewRotation.ValueRW.ViewRotation = characterLocalViewRotation.ValueRO.ViewRotation;

                break;
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
        EntityQueryBuilder queryBuilderRcpCommand = new EntityQueryBuilder(Allocator.Temp);
        queryBuilderRcpCommand.WithAll<LocalTransform, CharacterLocalViewRotation, CharacterAndViewRotationComponent>()
            .WithNone<GhostOwnerIsLocal>();
        state.RequireForUpdate(state.GetEntityQuery(queryBuilderRcpCommand));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, localViewRotation, characterAndViewRotation) in SystemAPI
                .Query<RefRW<LocalTransform>, RefRW<CharacterLocalViewRotation>, RefRW<CharacterAndViewRotationComponent>>()
                .WithNone<GhostOwnerIsLocal>())
        {
            transform.ValueRW.Rotation = characterAndViewRotation.ValueRO.CharacterRotation;
            localViewRotation.ValueRW.ViewRotation = characterAndViewRotation.ValueRO.ViewRotation;
        }
    }
}
