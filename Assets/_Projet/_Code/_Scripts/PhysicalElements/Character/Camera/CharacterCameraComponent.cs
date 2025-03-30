using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[GhostEnabledBit]
public struct CharacterCameraIsEnable : IComponentData, IEnableableComponent { }

public struct CharacterCameraComponent : IComponentData
{
    public Entity CameraFollowEntity;
    public float3 DeltaPosition;
}
