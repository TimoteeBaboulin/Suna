using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct DropStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CharacterStuffList>();
        state.RequireForUpdate<CharacterInput>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var unequipStuffQueue = SystemAPI.GetSingletonBuffer<UnequipStuffQueue>();

        foreach (var (stuffListRW, inputRO, chara) in SystemAPI
        .Query<RefRW<CharacterStuffList>, RefRO<CharacterInput>>()
        .WithEntityAccess())
        {
            ref readonly CharacterInput input = ref inputRO.ValueRO;
            ref CharacterStuffList stuffList = ref stuffListRW.ValueRW;
            if (input.drop.IsSet && stuffList.StuffInHandSlot != StuffSlot.Melee)
            {
                unequipStuffQueue.Add(new UnequipStuffQueue
                {
                    Stuff = stuffList.StuffInHand,
                    Owner = chara,
                });
            }
        }
    }
}
