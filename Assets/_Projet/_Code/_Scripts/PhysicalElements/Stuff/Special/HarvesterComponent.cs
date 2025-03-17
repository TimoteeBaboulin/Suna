using Unity.Entities;
using Unity.NetCode;

partial struct HarvesterComponent : IComponentData
{
    public NetworkTime time;
    public int ownerNetworkId;
}