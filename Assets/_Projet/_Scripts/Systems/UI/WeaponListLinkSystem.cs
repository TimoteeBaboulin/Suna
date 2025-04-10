using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;


partial class WeaponListLinkSystem : SystemBase
{
    // Events and Args
    public class StuffListChangeEventArgs : EventArgs { public List<string> StuffListNames; public List<int> StuffListIds; }
    public class StuffIdEventArgs : EventArgs { public int StuffId; }

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
                // For now these events are fired every frame, but we could optimize this by checking if the list has changed or (better and simpler) by add ing a flag to the entity
                // List and ids of all weapons in character
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
                if (names.Count > 0) // Check if there are any weapons
                {
                    OnStuffListChange?.Invoke(this, new StuffListChangeEventArgs()
                    {
                        StuffListNames = names,
                        StuffListIds = ids
                    });
                }

                // Held weapon id
                stuffInHandIndex = (int)stuffInHand.ValueRO.Value;
                OnStuffIdChange?.Invoke(this, new StuffIdEventArgs()
                {
                    StuffId = stuffInHandIndex
                });
            }
        }
    }
}
