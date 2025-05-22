
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct SoundBankSystemClient : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SoundRegister>();
    }

    public void OnUpdate(ref SystemState state)
    {
#if !UNITY_SERVER

        var bank = SoundManager.Instance.bank;
#endif
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (register, entity) in SystemAPI
            .Query<SoundRegister>()
            .WithEntityAccess())
        {
#if !UNITY_SERVER

            foreach (var pair in register.bank)
            {
                if (!bank.ContainsKey(pair.Key))
                {
                    //UnityEngine.Debug.Log("<color=green>SoundBankSystemClient Add </color> " + pair.Key);
                    bank.Add(pair.Key, pair.Value);
                }
            }

            register.bank.Clear();
#endif
            ecb.RemoveComponent<SoundRegister>(entity);
        }
    }
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct SoundPlayRPCSystemClient : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        //state.RequireForUpdate<StuffGameObjectRef>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        SoundManager soundManager = SoundManager.Instance;

        foreach (var (request, soundRpc, rpcEntity) in SystemAPI
            .Query<RefRO<ReceiveRpcCommandRequest>, RefRO<SoundRpc>>()
            .WithEntityAccess())
        {
                //UnityEngine.Debug.Log("<color=blue>Play Rpc 0 </color>" + soundRpc.ValueRO.keyGroup.ToString()+soundRpc.ValueRO.keyAction.ToString());
            if (request.ValueRO.IsConsumed) continue;
                //UnityEngine.Debug.Log("<color=blue>Play Rpc 1 </color>" + soundRpc.ValueRO.keyGroup.ToString()+soundRpc.ValueRO.keyAction.ToString());

            if (soundRpc.ValueRO.side != TeamSideType.Neutre)
            {
                //UnityEngine.Debug.Log("<color=blue>Play Rpc 2 </color>" + soundRpc.ValueRO.keyGroup.ToString()+soundRpc.ValueRO.keyAction.ToString());

                EntityQuery query = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ClientComponent>(), ComponentType.ReadOnly<GhostOwnerIsLocal>());
                NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);

                if (entities.Length == 0) continue;
                //UnityEngine.Debug.Log("<color=blue>Play Rpc 3 </color>" + soundRpc.ValueRO.keyGroup.ToString()+soundRpc.ValueRO.keyAction.ToString());

                int OwnerLocalnetworkId = state.EntityManager.GetComponentData<GhostOwner>(entities[0]).NetworkId;
                TeamSideType side = PlayerHelpers.GetPlayerInTeam(OwnerLocalnetworkId);

                if (side == soundRpc.ValueRO.side)
                {
                //UnityEngine.Debug.Log("<color=blue>Play Rpc 4 </color>" + soundRpc.ValueRO.keyGroup.ToString()+soundRpc.ValueRO.keyAction.ToString());
#if !UNITY_SERVER
                    soundManager.Play(soundRpc.ValueRO.keyGroup.ToString(), soundRpc.ValueRO.keyAction.ToString(), soundRpc.ValueRO.pos);
#endif
                }
            }
            else
            {
                //UnityEngine.Debug.Log("<color=blue>Play Rpc 5 </color>" + soundRpc.ValueRO.keyGroup.ToString()+soundRpc.ValueRO.keyAction.ToString());
#if !UNITY_SERVER
                soundManager.Play(soundRpc.ValueRO.keyGroup.ToString(), soundRpc.ValueRO.keyAction.ToString(), soundRpc.ValueRO.pos);
#endif
            }

            ecb.DestroyEntity(rpcEntity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ClientSimulation)]
partial struct SoundMainMenuVolumeSystemClient : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ClientSettingsComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (state.World.IsServer()) return;

        SoundManager soundManager = SoundManager.Instance;
        float volume = SystemAPI.GetSingleton<ClientSettingsComponent>().Volume;

#if !UNITY_SERVER
        soundManager.SetVolume(volume);
#endif
    }
}







