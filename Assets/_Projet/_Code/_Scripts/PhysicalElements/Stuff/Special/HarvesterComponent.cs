using System.ComponentModel;
using Unity.Entities;
using Unity.NetCode;

public partial struct HarvesterPlanting : IComponentData, IEnableableComponent
{

}

public partial struct HarvesterPlanted : IComponentData, IEnableableComponent
{

}

public partial struct RpcHarvesterPlanted : IRpcCommand
{
    public Entity harvester;
    public NetworkTick plantedTick;
    public Entity harvesterOwner;
}

public partial struct TemporaryOverrideGameObjectActive : IComponentData
{

}

public partial struct RpcHarvesterDefuseStart : IRpcCommand
{
    public Entity harvester;
    public NetworkTick defuseStartTick;
}

public partial struct RpcHarvesterOwnerChange : IRpcCommand
{
    public Entity harvester;
    public Entity newOwner;
    public Entity character;
}

public partial struct HarvesterComponent : IComponentData
{
    public Entity Owner;
    public NetworkTick DroppedTick;
    public NetworkTick PlantStartedTick;
    public NetworkTick PlantedTick;
}
