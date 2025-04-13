using GameNetwork.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Services.Multiplayer;
using Unity.Transforms;
using UnityEngine;

public struct ServerMessageRpcCommand : IRpcCommand
{
    public FixedString64Bytes message;
}
public struct ClientComponent : IComponentData
{
    public int networkID;
    public FixedString64Bytes playerID;
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class ServerSystem : SystemBase
{
    private ComponentLookup<NetworkId> _clients;

    protected override void OnCreate()
    {
        _clients = GetComponentLookup<NetworkId>(true);

        RequireForUpdate<NetworkId>();
    }
    protected override void OnUpdate()
    {
        _clients.Update(this);

        EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.Temp);

        //Message from all clients to server
        foreach (var (request, command, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ClientMessageRpcCommand>>().WithEntityAccess())
        {
            ServerConsole.Log(ServerConsole.LogType.Info, $"{command.ValueRO.message} from client index {request.ValueRO.SourceConnection.Index}, version {request.ValueRO.SourceConnection.Version}");
            Debug.Log($"{command.ValueRO.message} from client index {request.ValueRO.SourceConnection.Index}");
            commandBuffer.DestroyEntity(entity);
        }

        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<ClientComponent>().WithEntityAccess())
        {
            InstantiateClient(entity, commandBuffer);
        }

        //if (Keyboard.current.oKey.wasPressedThisFrame)
        //{
        //    ServerMessageRpcCommand command = new ServerMessageRpcCommand() { message = "Hello world" };
        //    RpcUtils.SendServerToClientRpc(ref command);
        //}

        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }

    #region Public Methods

    public void InstantiateClient(Entity ownerEntity, EntityCommandBuffer ecb)
    {
        if (SystemAPI.TryGetSingleton(out ClientPrefabData prefabManager))
        {
            if (prefabManager.Client == null)
            {
                return;
            }

            NetworkId networkId = SystemAPI.GetComponent<NetworkId>(ownerEntity);
            FixedString128Bytes worldName = ClientServerBootstrap.ServerWorld.Name;
            Entity client = ecb.Instantiate(prefabManager.Client);
            ecb.SetComponent(client, new GhostOwner() //Set owner of player to connection
            {
                NetworkId = networkId.Value
            });
            ecb.AppendToBuffer(ownerEntity, new LinkedEntityGroup() //Link it to connection
            {
                Value = client
            });

            FixedString64Bytes currentPlayerId = ClientTransportHelper.instance.Session.CurrentPlayer.Id;

            Debug.Log($"Assigning Player ID: {currentPlayerId.ToString()} to client with NetworkId {networkId.Value}");
            ecb.AddComponent(ownerEntity, new ClientComponent { 
                networkID = networkId.Value, 
                playerID = currentPlayerId}
            );

            ServerConsole.Log(ServerConsole.LogType.Info, $"New Client connected with NetworkId {networkId.Value}, in the world {worldName}");
        }
    }
    #endregion
}

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class SessionStatusSystem : SystemBase
{
    // Set your desired interval (in seconds)
    private float logInterval = 5f;
    private float timer;
    private bool didSubscribe = false;

    protected override void OnCreate()
    {
        var clients = GetComponentLookup<NetworkId>(true);

        RequireForUpdate<NetworkId>();
        Debug.Log("[SessionStatusSystem] Waiting for session to be created...");
    }

    protected override void OnUpdate()
    {
        // First, check if we haven't subscribed and a session is available.
        if (!didSubscribe && ClientTransportHelper.instance != null)
        {
            var session = ClientTransportHelper.instance.Session;
            session.PlayerLeaving += OnPlayerLeaving;
            session.PlayerHasLeft += OnPlayerHasLeft;
            session.RemovedFromSession += OnRemovedFromSession;
            session.SessionPropertiesChanged += OnSessionPropertiesChanged;
            session.StateChanged += OnStateChanged;

            didSubscribe = true;
            Debug.Log($"[SessionStatusSystem] Subscribed to session events for session: {session.Name}");
        }
        float deltaTime = SystemAPI.Time.DeltaTime;
        timer += deltaTime;

        if (timer >= logInterval)
        {
            timer = 0f; 

            if (ClientTransportHelper.instance != null)
            {
                var session = ClientTransportHelper.instance.Session;
                Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Session ID: {session.Id}");
                Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Session Name: {session.Name}");
                Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Current Nb of player: {session.PlayerCount}");
                Debug.Log($"[SessionStatusSystem :@ {System.DateTime.Now}] Session State: {session.State} ");;


                var players = session.Players;

                //Debug.Log($"Count Corpo : {session.Properties["CountTeamCorpo"].Value}");
                //Debug.Log($"Count Natif : {session.Properties["CountTeamNatif"].Value}");

                foreach (var player in players)
                {
                    Debug.Log($"[SessionStatusSystem :@ {player.Id}] propertyCount: {player.Properties.Count} ");
                    foreach (var property in player.Properties)
                    {
                        Debug.Log($"[SessionStatusSystem :@ {player.Id}] team: {property.Value.Value} "); ;
                    }
                }
            }
            else
            {
                Debug.Log($"[SessionStatusSystem] No active session detected @ {System.DateTime.Now}");
            }
        }
    }

    private void Session_StateChanged(Unity.Services.Multiplayer.SessionState obj)
    {
        throw new System.NotImplementedException();
    }

    protected override void OnDestroy()
    {
        if (didSubscribe && ClientTransportHelper.instance != null)
        {
            var session = ClientTransportHelper.instance.Session;
            session.PlayerLeaving -= OnPlayerLeaving;
            session.PlayerHasLeft -= OnPlayerHasLeft;
            session.RemovedFromSession -= OnRemovedFromSession;
            session.SessionPropertiesChanged -= OnSessionPropertiesChanged;

            Debug.Log("[SessionStatusSystem] Unsubscribed from session events.");
        }
    }

    private void OnStateChanged(SessionState state)
    {
        Debug.Log($"[SessionStatusSystem] state {state}");
    }

    private void OnPlayerLeaving(string playerId)
    {
        Debug.Log($"[SessionStatusSystem] Player with NetworkId {playerId} is leaving the session.");

        //if (int.TryParse(playerId, out int targetId))
        //{
        //    foreach (var (clientData, entity) in SystemAPI.Query<RefRO<InitializedClient>>().WithEntityAccess())
        //    {
        //        if (clientData.ValueRO.id == targetId)
        //        {
        //            EntityManager.DestroyEntity(entity);
        //            Debug.Log($"[SessionStatusSystem] Destroyed client entity with NetworkId {playerId}");
        //        }
        //    }
        //}
        //else
        //{
        //    Debug.LogError($"[SessionStatusSystem] Unable to parse playerId {playerId} to an integer.");
        //}
    }

    // Event handler for when a player has left.
    private void OnPlayerHasLeft(string playerId)
    {
        Debug.Log($"[SessionStatusSystem] Player with NetworkId {playerId} has left the session.");
        //if (int.TryParse(playerId, out int targetId))
        //{
        //    foreach (var (clientData, entity) in SystemAPI.Query<RefRO<InitializedClient>>().WithEntityAccess())
        //    {
        //        if (clientData.ValueRO.id == targetId)
        //        {
        //            EntityManager.DestroyEntity(entity);
        //            Debug.Log($"[SessionStatusSystem] Destroyed client entity with NetworkId {playerId}");
        //        }
        //    }
        //}
        //else
        //{
        //    Debug.LogError($"[SessionStatusSystem] Unable to parse playerId {playerId} to an integer.");
        //}
    }

    // Event handler for when the current client is removed.
    private void OnRemovedFromSession()
    {
        Debug.Log("[SessionStatusSystem] Current client has been removed from the session.");
    }

    // Event handler for when session properties change.
    private void OnSessionPropertiesChanged()
    {
        Debug.Log("[SessionStatusSystem] Session properties have been updated.");
        // Update internal ECS state or perform other actions based on the new properties.
    }
}