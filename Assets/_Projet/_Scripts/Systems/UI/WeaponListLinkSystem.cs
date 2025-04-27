using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
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
            foreach (var stuffList in SystemAPI
            .Query<RefRO<CharacterStuffList>>()
            .WithAll<GhostOwnerIsLocal>())
            {
                List<string> names = new();
                List<int> ids = new();
                foreach (var stuffEntity in stuffList.ValueRO.List)
                {
                    if (stuffEntity != Entity.Null)
                    {
                        bool isValid = SystemAPI.HasComponent<StuffDatabaseAccess>(stuffEntity);
                        if (!isValid) continue;
                        StuffDatabaseAccess dbAccess = SystemAPI.GetComponent<StuffDatabaseAccess>(stuffEntity);
                        names.Add(dbAccess.GetData(ref database).Name.ToString());
                        ids.Add((int)dbAccess.GetData(ref database).slot);
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

                stuffInHandIndex = (int)stuffList.ValueRO.StuffInHandSlot;
                OnStuffIdChange?.Invoke(this, new StuffIdEventArgs()
                {
                    StuffId = stuffInHandIndex
                });
            }
        }
    }
}
