using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

[GhostComponent] //TODO : Attention à toujour mettre [GhostField] devant les variables,
                 //[GhostComponent] ne le fait pas par defaut
public struct HarvesterComponent : IComponentData
{
    [GhostField] public float defuseRange;
    [GhostField] public float pickupDistance;

    [GhostField] public NetworkTick DroppedTick;

    [GhostField] public bool IsActive;
}

[GhostEnabledBit]
[GhostComponent]
public partial struct HarvesterPlanting : IComponentData, IEnableableComponent
{
    [GhostField] public NetworkTick PlantStartedTick;
}

[GhostEnabledBit]
[GhostComponent]
public partial struct HarvesterDefusing : IComponentData, IEnableableComponent
{
    [GhostField] public NetworkTick DefuseStartedTick;
    [GhostField] public Entity Defuser;
}

[GhostEnabledBit]
[GhostComponent]
public partial struct HarvesterPlanted : IComponentData, IEnableableComponent
{
    [GhostField] public NetworkTick PlantedTick;
}

#region RPCCommands
public partial struct RpcHarvesterOwnerChange : IRpcCommand
{
    public Entity harvester;
    public Entity character;
}

public partial struct RpcHarvesterDropped : IRpcCommand
{
    public Entity harvester;
    public float3 position;
}
public partial struct RpcHarvesterPlanted : IRpcCommand
{
    public Entity harvester;
    public NetworkTick plantedTick;
    public Entity harvesterOwner;

    public float3 plantPosition;
}
public partial struct RpcHarvesterDefuseStop : IRpcCommand
{
    public Entity harvester;
    public Entity character;
    public NetworkTick defuseStopTick;
}

public partial struct RpcHarvesterDefuseStart : IRpcCommand
{
    public Entity harvester;
    public Entity character;
    public NetworkTick defuseStartTick;
}

public struct RpcHarvesterPlantStart : IRpcCommand
{
    public NetworkTick tick;
    public Entity harvester;
    public Entity character;
}

public struct RpcHarvesterPlantStop : IRpcCommand
{
    public NetworkTick tick;
    public Entity harvester;
}

public struct RpcRequestHarvesterOwners : IRpcCommand
{

}
#endregion //RPCCommands


//public partial struct HarvesterPlanted : IComponentData, IEnableableComponent
//{

//}

//public partial struct TemporaryOverrideGameObjectActive : IComponentData
//{

//}

public partial struct HarvesterRespawn : IComponentData
{

}
