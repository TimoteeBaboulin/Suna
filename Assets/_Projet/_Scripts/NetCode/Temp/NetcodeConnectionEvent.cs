using GameNetwork.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Rendering;

[UpdateAfter(typeof(NetworkReceiveSystemGroup))]
[BurstCompile]
public partial struct NetcodeConnectionEvent : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkId>();
    }


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var connectionEventsForTick = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;

        //foreach (var evt in connectionEventsForTick)
        //{
        //    if (state.World.IsServer())
        //    {
        //        if (evt.State == ConnectionState.State.Handshake)
        //        {
        //            Debug.Log("Handshake state");

        //            //if (!ClientConnection.AllowConnection)
        //            //{
        //            //    Debug.Log("Blocking connection during handshake");
        //            //    state.EntityManager.DestroyEntity(evt.ConnectionEntity);
        //            //}
        //        }
        //        else if (evt.State == ConnectionState.State.Disconnected)
        //        {
        //            foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().
        //                WithAll<InitializedClient>().
        //                WithEntityAccess())
        //            {
        //                NetworkId networkId = SystemAPI.GetComponent<NetworkId>(entity);
        //                FixedString128Bytes worldName = ClientServerBootstrap.ServerWorld.Name;
        //                ServerConsole.Log(ServerConsole.LogType.Info, $"Client DISCONNECTED with NetworkId {networkId.Value}, in the world {worldName}");
        //            }
        //        }
        //        else if (evt.State == ConnectionState.State.Connected)
        //        {
        //            foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().
        //            WithAll<InitializedClient>().
        //            WithEntityAccess())
        //            {
        //                NetworkId networkId = SystemAPI.GetComponent<NetworkId>(entity);
        //                FixedString128Bytes worldName = ClientServerBootstrap.ServerWorld.Name;
        //                ServerConsole.Log(ServerConsole.LogType.Info, $"Client DISCONNECTED with NetworkId {networkId.Value}, in the world {worldName}");
        //            }
        //        }
        //    }

        //if (state.World.IsClient())
        //{
        //    if (evt.State == ConnectionState.State.Unknown)
        //    {
        //        Debug.Log("Client: Failed to connect to server. Server might be offline.");
        //    }
        //}
    }
}
