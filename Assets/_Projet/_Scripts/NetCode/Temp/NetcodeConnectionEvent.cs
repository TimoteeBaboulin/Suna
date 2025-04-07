using GameNetwork.Utils;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[UpdateAfter(typeof(NetworkReceiveSystemGroup))]
[BurstCompile]
public partial struct NetcodeConnectionEvent : ISystem
{

    //[BurstCompile]
    //public void OnUpdate(ref SystemState state)
    //{
    //    var connectionEventsForTick = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;

    //    foreach (var evt in connectionEventsForTick)
    //    {
    //        Debug.Log($"[{state.WorldUnmanaged.Name}] {evt.State} for entity {evt.ConnectionEntity}!");

    //        if (state.World.IsServer())
    //        {
    //            if (evt.State == ConnectionState.State.Handshake)
    //            {
    //                Debug.Log("Handshake state");

    //                if (!ClientConnection.AllowConnection)
    //                {
    //                    Debug.Log("Blocking connection during handshake");
    //                    state.EntityManager.DestroyEntity(evt.ConnectionEntity);
    //                }
    //            }
    //        }

    //        if (!state.World.IsClient())
    //        {
    //            if (evt.State == ConnectionState.State.Disconnected)
    //            {
    //                Debug.Log("Client: Failed to connect to server. Server might be offline.");
    //            }
    //        }
    //    }
    //}
}
