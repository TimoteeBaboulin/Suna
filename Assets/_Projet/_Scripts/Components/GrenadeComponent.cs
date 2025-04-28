using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

[GhostComponent]
public struct GrenadeDynamicData : IComponentData
{
    public float cookingTime;
    public bool isCooking;

    public float thrownForTime;
}

public struct GrenadeCommonData
{
    public GrenadeType grenadeType;

    public float cookingTime;
    public float impactRadius;
    public float inflictedDamage;

    public GrenadeTriggerType triggerType;
    public float timerTriggerDelay;
    public float maxImpactAngle;
    public float stillTriggerDelay;
    public uint bounceTriggerCount;
    public float proximityTriggerDistance;
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

[GhostEnabledBit]
[GhostComponent]
public struct ReleasedGrenade : IComponentData, IEnableableComponent 
{
    [GhostField] public Entity thrower;
}