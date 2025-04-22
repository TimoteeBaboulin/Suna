using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

public class FirstPersonCharacterModelPrefab : IComponentData
{
    public GameObject CorpoModelPrefab;
    public GameObject NatifModelPrefab;
    public float3 DeltaPosition;
}

public class FirstPersonCharacterModelReference : ICleanupComponentData
{
    public GameObject ModelGameObject;
    public float3 DeltaPosition;
    public float3 ShootDelta; //TODO : tmp
}

public struct FPVVisualRecoil : IComponentData
{
    [GhostField] public float timeSinceLastShoot;
}
