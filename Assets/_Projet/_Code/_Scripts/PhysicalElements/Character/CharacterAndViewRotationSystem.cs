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
            float newRotationYDegree = math.degrees(CharacterLocalViewRotation.ValueRW.ViewRotation.value.x) - mouseY;
            newRotationYDegree = math.clamp(newRotationYDegree, -40, 40);
            CharacterLocalViewRotation.ValueRW.ViewRotation.value.x = math.radians(newRotationYDegree);
            CharacterLocalViewRotation.ValueRW.ViewRotation.value.y = 0;
            CharacterLocalViewRotation.ValueRW.ViewRotation.value.z = 0;
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
                ViewRotation = characterLocalView.ValueRO.ViewRotation,
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
        // Creation of a query builder that prevents the system from executing
        // if there is not at least one RPC message that matches what is needed in the update function.
        EntityQueryBuilder queryBuilderRcpCommand = new EntityQueryBuilder(Allocator.Temp);
        queryBuilderRcpCommand.WithAll<ReceiveRpcCommandRequest, ClientCharacterAndViewRotationRpcCommand>();
        state.RequireForUpdate(state.GetEntityQuery(queryBuilderRcpCommand));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Creation of a command buffer allocated temporarily.
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        // Search for a received RPC message that contains the component sending the character's rotation and its view.
        foreach (var (request, characterAndViewRotationRpc, entity) in SystemAPI
            .Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ClientCharacterAndViewRotationRpcCommand>>()
            .WithEntityAccess())
        {
            // Retrieve the client ID that sent this RPC.
            RefRO<NetworkId> requestNetworkId = SystemAPI.GetComponentRO<NetworkId>(request.ValueRO.SourceConnection);

            // Search for the character that corresponds to the client ID that sent the RPC and assign the values of the character's rotation,
            // its local view rotation, and the values from the characterAndViewRotation component that synchronize both rotations with all other players.
            foreach (var (characterTransform, characterLocalViewRotation, characterAndViewRotation, ghostOwner) in SystemAPI
                .Query<RefRW<LocalTransform>, RefRW<CharacterLocalViewRotation>, RefRW<CharacterAndViewRotationComponent>, RefRO<GhostOwner>>())
            {
                // Check if the player's ID matches the one that sent the RPC. If the IDs do not match, the foreach loop continues.
                if (ghostOwner.ValueRO.NetworkId != requestNetworkId.ValueRO.Value)
                {
                    continue;
                }

                // Assign the values of the character's rotation and its view rotation.
                characterTransform.ValueRW.Rotation = characterAndViewRotationRpc.ValueRO.CharacterRotation;
                characterLocalViewRotation.ValueRW.ViewRotation = characterAndViewRotationRpc.ValueRO.ViewRotation;

                // Assign the values from the characterAndViewRotation component,
                // which allows synchronizing the character's rotation and its view rotation with all other clients.
                characterAndViewRotation.ValueRW.CharacterRotation = characterTransform.ValueRO.Rotation;
                characterAndViewRotation.ValueRW.ViewRotation = characterLocalViewRotation.ValueRO.ViewRotation;

                // Exit the foreach loop because we have found and updated the values for the concerned client.
                break;
            }

            // Add to the command buffer the deletion of the entity that contains the RPC message.
            ecb.DestroyEntity(entity);
        }

        // Execute the commands stored in the command buffer and release it.
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

// This system is updated only on the client and runs at a fixed number of times per second.
// Allows updating the rotation of the character and its view, which belong to other clients.
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
partial struct UpdateOtherCharacterAndViewRotationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Creation of a query builder that prevents the execution of the update
        // if there is not at least one entity that has the components needed in the foreach loop.
        EntityQueryBuilder queryBuilderRcpCommand = new EntityQueryBuilder(Allocator.Temp);
        queryBuilderRcpCommand.WithAll<LocalTransform, CharacterLocalViewRotation, CharacterAndViewRotationComponent>()
            .WithNone<GhostOwnerIsLocal>();
        state.RequireForUpdate(state.GetEntityQuery(queryBuilderRcpCommand));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Search for characters and their views that do not belong to the current client in order to update their rotation.
        foreach (var (transform, localViewRotation, characterAndViewRotation) in SystemAPI
                .Query<RefRW<LocalTransform>, RefRW<CharacterLocalViewRotation>, RefRW<CharacterAndViewRotationComponent>>()
                .WithNone<GhostOwnerIsLocal>())
        {
            // Assign the values for the character's rotation and its view from the values contained in the CharacterAndViewRotation component,
            // which is synchronized by the server.
            transform.ValueRW.Rotation = characterAndViewRotation.ValueRO.CharacterRotation;
            localViewRotation.ValueRW.ViewRotation = characterAndViewRotation.ValueRO.ViewRotation;
        }
    }
}
