using Unity.Entities;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct CharacterInstantiateStuffSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        EntityQuery query = SystemAPI.QueryBuilder()
        .WithAll<IsInstanciateDefaultStuff>()
        .Build();

        state.RequireForUpdate(query);
        state.RequireForUpdate<GameResourcesDatabase>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var stuffInstanciateQueus = SystemAPI.GetSingletonBuffer<GameResourcesInstantiateStuffQueue>();

        foreach (var (stuffListRef, defaultStuffNames, chara) in SystemAPI
            .Query<RefRW<CharacterStuffList>, DynamicBuffer<CharacterDefaultStuffName>>()
            .WithAll<IsInstanciateDefaultStuff>()
            .WithEntityAccess())
        {
            foreach (var name in defaultStuffNames)
            {
                stuffInstanciateQueus.Add(new GameResourcesInstantiateStuffQueue 
                { 
                    StuffName = name.Value, 
                    Owner = chara 
                });
            }

            state.EntityManager.SetComponentEnabled<IsInstanciateDefaultStuff>(chara, false);
        }
    }
}
