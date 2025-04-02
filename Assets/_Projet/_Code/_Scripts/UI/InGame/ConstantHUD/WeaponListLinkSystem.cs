using System;
using System.Collections.Generic;
using Unity.Entities;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial class WeaponListLinkSystem : SystemBase
{
    public class StuffListChangeEventArgs : EventArgs
    {
        public List<string> StuffListNames;
        public List<int> StuffListIds;
    }

    public event EventHandler<StuffListChangeEventArgs> OnStuffListChange;

    int stuffInHandIndex = -1;

    protected override void OnUpdate()
    {
        var database = SystemAPI.GetSingleton<GameResourcesDatabase>();

        foreach (var (stuffList, stuffInHand) in SystemAPI
            .Query<RefRO<CharacterStuffList>, RefRO<CharacterStuffInHandLocation>>())
        {
            List<string> names = new();
            List<int> ids = new();
            foreach (var stuffEntity in stuffList.ValueRO.Value)
            {
                if (stuffEntity != Entity.Null)
                {
                    StuffDatabaseAccess dbAccess = SystemAPI.GetComponent<StuffDatabaseAccess>(stuffEntity);
                    names.Add(dbAccess.GetData(ref database).Name.ToString());
                    ids.Add((int)dbAccess.GetData(ref database).location);
                }
            }
            if (names.Count > 0)
            {
                OnStuffListChange?.Invoke(this, new StuffListChangeEventArgs()
                {
                    StuffListNames = names,
                    StuffListIds = ids
                });
            }
        }
    }
}
