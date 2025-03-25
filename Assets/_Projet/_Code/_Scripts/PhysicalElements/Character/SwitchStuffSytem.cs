using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
public partial struct SwitchStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StuffOwner>();

        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<PhysicsWorldHistorySingleton>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        //Eviter répétition sur le serveur du a la différence de framerate avec le client
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
        if (!networkTime.IsFirstPredictionTick) return;

        float dt = networkTime.ServerTickFraction * SystemAPI.Time.DeltaTime;
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        foreach (var (stuffListRef, activeStuffRef, inputRef, chara) in SystemAPI
        .Query<RefRO<CharacterStuffList>, RefRW<CharacterStuffInHandType>, RefRO<CharacterInput>>()
        .WithEntityAccess())
        {
            ref readonly CharacterInput input = ref inputRef.ValueRO;
            ref readonly CharacterStuffList stuffList = ref stuffListRef.ValueRO;
            ref CharacterStuffInHandType stuffInHandType = ref activeStuffRef.ValueRW;

            if (state.World.IsServer())
            {
                Entity previousStuff = stuffList.Value[(int)stuffInHandType.Value];
                Entity nextStuff;

                int whileLimit = 0;
                int dir = input.selectNext.IsSet ? 1 : input.selectPrevious.IsSet ? -1 : 0;

                if (dir != 0)
                {
                    state.EntityManager.SetComponentEnabled<IsStuffInHand>(previousStuff, false);

                    do
                    {
                        stuffInHandType.Value += dir;

                        stuffInHandType.Value = (StuffType)((stuffList.Value.Length + (int)stuffInHandType.Value) % stuffList.Value.Length);

                        nextStuff = stuffList.Value[(int)stuffInHandType.Value];

                        whileLimit++;

                    } while (nextStuff == Entity.Null && whileLimit < stuffList.Value.Length);

                    state.EntityManager.SetComponentEnabled<IsStuffInHand>(nextStuff, true);
                }
            }
        }

        foreach (var (stuffListRef, activeStuffRef, inputRef, chara) in SystemAPI
        .Query<RefRO<CharacterStuffList>, RefRW<CharacterStuffInHandType>, RefRO<CharacterInput>>()
        .WithEntityAccess())
        {
            ref readonly CharacterInput input = ref inputRef.ValueRO;
            ref readonly CharacterStuffList stuffList = ref stuffListRef.ValueRO;
            ref CharacterStuffInHandType stuffInHandType = ref activeStuffRef.ValueRW;

            if (state.World.IsServer() && input.selectStuffId != -1)
            {
                Entity previousStuff = stuffList.Value[(int)stuffInHandType.Value];
                Entity nextStuff = stuffList.Value[input.selectStuffId];

                if (nextStuff != Entity.Null)
                {
                    state.EntityManager.SetComponentEnabled<IsStuffInHand>(previousStuff, false);
                    state.EntityManager.SetComponentEnabled<IsStuffInHand>(nextStuff, true);

                    stuffInHandType.Value = (StuffType)input.selectStuffId;
                }
            }
        }
    }
}
