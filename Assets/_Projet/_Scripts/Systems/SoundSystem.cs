#if !UNITY_SERVER
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

        if (SoundManager.TryGetBank(out var bank)) return;

        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (register, entity) in SystemAPI
            .Query<SoundRegister>()
            .WithEntityAccess())
        {

            foreach (var pair in register.bank)
            {
                bank.Add(pair.Key, pair.Value);
            }

            register.bank.Clear();
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
                soundManager.Play(soundInfos.keyGroup.ToString(), soundInfos.keyAction.ToString(), soundInfos.pos);
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

            soundManager.Play(soundRpc.ValueRO.keyGroup.ToString(), soundRpc.ValueRO.keyAction.ToString(), soundRpc.ValueRO.pos);

            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
#endif







