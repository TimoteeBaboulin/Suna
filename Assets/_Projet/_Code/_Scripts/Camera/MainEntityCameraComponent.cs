using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct MainEntityCameraTag : IComponentData { }

public struct UpdateViewRotationRcpCommand : IRpcCommand
{
    public int NetworkId;
    public quaternion RotationY;
    public quaternion RotationX;
}
