using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public partial struct HarvesterPlanting : IComponentData, IEnableableComponent
{

}

public partial struct HarvesterPlanted : IComponentData, IEnableableComponent
{

}

public partial struct TemporaryOverrideGameObjectActive : IComponentData
{

}

#region RPCCommands
public partial struct RpcHarvesterOwnerChange : IRpcCommand
{
    public Entity harvester;
    public Entity newOwner;
    public Entity character;
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
}

public struct RpcHarvesterPlantStop : IRpcCommand
{
    public NetworkTick tick;
    public Entity harvester;
}
#endregion //RPCCommands

public partial struct HarvesterComponent : IComponentData
{
    public Entity Owner;
    public NetworkTick DroppedTick;
    public NetworkTick PlantStartedTick;
    public NetworkTick PlantedTick;
}
