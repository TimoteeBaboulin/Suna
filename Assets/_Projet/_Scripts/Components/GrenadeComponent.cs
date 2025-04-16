using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

[GhostComponent]
public struct GrenadeDynamicData : IComponentData
{
    
}

public struct GrenadeCommonData
{
    
}

[GhostComponent]
public struct GrenadeDatabaseAccess : IComponentData
{
    [GhostField] public int Value;

    public readonly ref GrenadeCommonData GetData(ref GameResourcesDatabase database)
    {
        return ref database.StuffDatabaseRef.Value.GrenadesCommonData[Value];
    }
}