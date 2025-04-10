using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;


partial class WeaponListLinkSystem : SystemBase
{
    public class StuffListChangeEventArgs : EventArgs
    {
        public List<string> StuffListNames;
        public List<int> StuffListIds;
    }

    public class StuffIdEventArgs : EventArgs
    {
        public int StuffId;
    }

    public event EventHandler<StuffListChangeEventArgs> OnStuffListChange;
    public event EventHandler<StuffIdEventArgs> OnStuffIdChange;

    int stuffInHandIndex = -1;

    protected override void OnUpdate()
    {
        if (SystemAPI.TryGetSingleton(out GameResourcesDatabase database))
        {
            foreach (var (stuffList, stuffInHand) in SystemAPI
            .Query<RefRO<CharacterStuffList>, RefRO<CharacterStuffInHandLocation>>()
            .WithAll<GhostOwnerIsLocal>())
            {
                List<string> names = new();
                List<int> ids = new();
                foreach (var stuffEntity in stuffList.ValueRO.List)
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

                stuffInHandIndex = (int)stuffInHand.ValueRO.Value;
                OnStuffIdChange?.Invoke(this, new StuffIdEventArgs()
                {
                    StuffId = stuffInHandIndex
                });
            }
        }
    }
}
