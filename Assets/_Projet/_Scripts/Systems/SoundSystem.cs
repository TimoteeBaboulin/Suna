
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

        foreach (var (request, soundRpc, entity) in SystemAPI
            .Query<RefRO<ReceiveRpcCommandRequest>, RefRO<SoundRpc>>()
            .WithEntityAccess())
        {
            if (request.ValueRO.IsConsumed) continue;
#if !UNITY_SERVER
            //UnityEngine.Debug.Log("<color=green>SoundPlayRPCSystemClient Play </color> " + soundRpc.ValueRO.keyGroup.ToString() + soundRpc.ValueRO.keyAction.ToString());

            soundManager.Play(soundRpc.ValueRO.keyGroup.ToString(), soundRpc.ValueRO.keyAction.ToString(), soundRpc.ValueRO.pos);
#endif
            ecb.DestroyEntity(entity);
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







