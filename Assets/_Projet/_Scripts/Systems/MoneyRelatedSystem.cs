using Unity.Entities;
using Unity.Mathematics;

public partial struct MoneyRelatedSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CharacterMoney>();
    }
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (moneyRef, chara) in SystemAPI
            .Query<RefRW<CharacterMoney>>()
            .WithEntityAccess())
        {
            ref CharacterMoney moneyComp = ref moneyRef.ValueRW;

            moneyComp.money = math.clamp(moneyComp.money, 0, moneyComp.maxMoney);
        }
    }
}