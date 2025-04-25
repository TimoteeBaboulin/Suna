
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

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
                bank.Add(pair.Key, pair.Value);
            }

            register.bank.Clear();
#endif
            ecb.RemoveComponent<SoundRegister>(entity);
        }
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerClearSoundBufferSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (soundQueue, entity) in SystemAPI
            .Query<DynamicBuffer<SoundQueue>>()
            .WithEntityAccess())
        {
            soundQueue.Clear();
        }
    }
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct SoundPlayQueueSystemClient : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        EntityQuery query = state.GetEntityQuery(typeof(SoundQueue));
        query.SetChangedVersionFilter(typeof(SoundQueue));
        state.RequireForUpdate(query);
    }

    public void OnUpdate(ref SystemState state)
    {
        SoundManager soundManager = SoundManager.Instance;

        foreach (var (soundQueue, entity) in SystemAPI
            .Query<DynamicBuffer<SoundQueue>>()
            .WithEntityAccess())
        {
            foreach (var soundInfos in soundQueue)
            {
#if !UNITY_SERVER
                soundManager.Play(soundInfos.keyGroup.ToString(), soundInfos.keyAction.ToString(), soundInfos.pos);
#endif
            }
            soundQueue.Clear();
        }
    }
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct SoundPlayRPCSystemClient : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StuffGameObjectRef>();
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
            soundManager.Play(soundRpc.ValueRO.keyGroup.ToString(), soundRpc.ValueRO.keyAction.ToString(), soundRpc.ValueRO.pos);
#endif
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}







