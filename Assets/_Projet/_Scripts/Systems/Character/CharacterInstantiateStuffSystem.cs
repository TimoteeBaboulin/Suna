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
        state.RequireForUpdate<InstantiateStuffQueue>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var stuffInstanciateQueus = SystemAPI.GetSingletonBuffer<InstantiateStuffQueue>();

        foreach (var (defaultStuffNames, chara) in SystemAPI
            .Query<DynamicBuffer<CharacterDefaultStuffName>>()
            .WithAll<IsInstanciateDefaultStuff>()
            .WithEntityAccess())
        {
            //foreach (var name in defaultStuffNames)
            for (int i = defaultStuffNames.Length - 1; i >= 0; i--)
            {
                var name = defaultStuffNames[i];
                StuffUtils.InstantiateNextFrame(stuffInstanciateQueus, name.Value, chara);
            }

            state.EntityManager.SetComponentEnabled<IsInstanciateDefaultStuff>(chara, false);
        }
    }
}
