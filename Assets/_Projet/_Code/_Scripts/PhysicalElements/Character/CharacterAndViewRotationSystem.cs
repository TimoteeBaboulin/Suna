using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

// This system is updated only on client and runs every frame.
// Update of the character's rotation and its view.
// The character's rotation affects the Y-axis of its LocalTransform.
// The rotation of the view modifies the value of a component called "CharacterLocalViewRotation,"
// and the rotation occurs around the X-axis.
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct CharacterAndViewRotationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Creation of a QueryBuilder that prevents the system from executing if no entity possesses the required components in the foreach loop of the update.
        EntityQueryBuilder queryBuilder = new EntityQueryBuilder(Allocator.Temp);
        queryBuilder.WithAll<LocalTransform, CharacterLocalViewRotation, CharacterInput, GhostOwnerIsLocal>();
        state.RequireForUpdate(state.GetEntityQuery(queryBuilder));
    }

    public void OnUpdate(ref SystemState state)
    {
        // Search for the character owned by the client to update its rotation and its view rotation.
        foreach (var (characterTransform, CharacterLocalViewRotation, CharacterInput) in SystemAPI
            .Query<RefRW<LocalTransform>, RefRW<CharacterLocalViewRotation>, RefRO<CharacterInput>>()
            .WithAll<GhostOwnerIsLocal>())
        {
            // We retrieve the mouse movement value in X and Y and multiply it by the delta time of the current simulation.
            float mouseX = SystemAPI.Time.DeltaTime * CharacterInput.ValueRO.look.x;
            float mouseY = SystemAPI.Time.DeltaTime * CharacterInput.ValueRO.look.y;

            // Character rotation update: For safety, once the rotation calculation is completed, the values of the X and Z axes are set to 0.
            characterTransform.ValueRW.Rotation = math.mul(characterTransform.ValueRO.Rotation, quaternion.RotateY(math.radians(mouseX)));
            characterTransform.ValueRW.Rotation.value.x = 0;
            characterTransform.ValueRW.Rotation.value.z = 0;

            // Update of the character's view rotation:
            // First, we perform the calculations in degrees to prevent the clamp from returning the opposite value if the mouse movement value is too high.
            // Then, we apply the rotation in radians and set the values of the Y and Z axes to 0.
            float newRotationYDegree = math.degrees(CharacterLocalViewRotation.ValueRW.Value.value.x) - mouseY;
            newRotationYDegree = math.clamp(newRotationYDegree, -40, 40);
            CharacterLocalViewRotation.ValueRW.Value.value.x = math.radians(newRotationYDegree);
            CharacterLocalViewRotation.ValueRW.Value.value.y = 0;
            CharacterLocalViewRotation.ValueRW.Value.value.z = 0;
        }
    }
}

// This system is updated only on the client and runs at a fixed number of times per second.
// Create an RPC command that will be sent to the server, which includes the character's rotation and the rotation of its view.
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
partial struct ClientCharacterAndViewRotationRpcSendSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Creation of a QueryBuilder that prevents the system from executing if no entity possesses the required components in the foreach loop of the update.
        EntityQueryBuilder queryBuilder = new EntityQueryBuilder(Allocator.Temp);
        queryBuilder.WithAll<LocalTransform, CharacterLocalViewRotation, GhostOwnerIsLocal>();
        state.RequireForUpdate(state.GetEntityQuery(queryBuilder));
    }

    public void OnUpdate(ref SystemState state)
    {
        // Search for the character owned by the player in order to create an RPC command and send it to the server.
        foreach (var (characterTransform, characterLocalView) in SystemAPI
            .Query<RefRO<LocalTransform>, RefRO<CharacterLocalViewRotation>>()
            .WithAll<GhostOwnerIsLocal>())
        {
            // Creation of the RPC command and assignment of the character's rotation value and its view value.
            ClientCharacterAndViewRotationRpcCommand command = new ClientCharacterAndViewRotationRpcCommand
            {
                CharacterRotation = characterTransform.ValueRO.Rotation,
                ViewRotation = characterLocalView.ValueRO.Value,
            };

            // Send the RPC command to the server.
            RpcUtils.SendClientToServerRpc(ref command);
        }
    }
}

// This system is updated only on the server and runs at a fixed number of times per second.
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
partial struct ServerCharacterAndViewRotationRpcReceiveSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder queryBuilderRcpCommand = new EntityQueryBuilder(Allocator.Temp);
        queryBuilderRcpCommand.WithAll<LocalTransform, CharacterLocalViewRotation, GhostOwnerIsLocal>();
        state.RequireForUpdate(state.GetEntityQuery(queryBuilderRcpCommand));

        EntityQueryBuilder queryBuilderCharacter = new EntityQueryBuilder(Allocator.Temp);
        queryBuilderCharacter.WithAll<LocalTransform, CharacterLocalViewRotation, GhostOwnerIsLocal>();
        state.RequireForUpdate(state.GetEntityQuery(queryBuilderCharacter));
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
